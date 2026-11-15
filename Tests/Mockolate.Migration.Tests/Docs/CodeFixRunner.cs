using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Mockolate.Migration.Analyzers;
using Mockolate.Migration.Tests.Verifiers;

namespace Mockolate.Migration.Tests.Docs;

/// <summary>
///     Applies the code fix of a diagnostic to the code before the migration until the diagnostic is no longer reported.
/// </summary>
/// <remarks>
///     The code before and after the migration is wrapped the same way into a document: the <c>using</c> directive of the
///     migrated library and the <c>using</c> directives of the code at the top, the statements in a method and the types
///     after it. A scaffold with the types of the examples is part of the compilation, and the references are those of
///     the code fix tests. The <c>using</c> directives that the code fix adds are not compared, as the pages do not show
///     them.
/// </remarks>
internal static class CodeFixRunner
{
	private const string ExampleClass = "ApplyFixExample";
	private const int MaxFixes = 10;

	private const string Scaffold = """
		public delegate void ChocolateDispensedDelegate(string type, int amount);

		public interface IChocolateDispenser
		{
			int this[string type] { get; set; }
			int TotalDispensed { get; set; }
			bool Dispense(string type, int amount);
			event ChocolateDispensedDelegate ChocolateDispensed;
		}

		public class VendingMachine(IChocolateDispenser dispenser)
		{
			public string Buy(string type, int amount)
				=> dispenser.Dispense(type, amount) ? "Enjoy!" : "Sold out";
		}
		""";

	private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

	private static readonly Dictionary<string, string> Libraries = new()
	{
		["MockolateM001"] = "Moq",
		["MockolateM002"] = "NSubstitute",
	};

	private static readonly ImmutableArray<DiagnosticAnalyzer> Analyzers =
		[new MoqAnalyzer(), new NSubstituteAnalyzer(),];

	private static readonly CodeFixProvider[] CodeFixes = [new MoqCodeFixProvider(), new NSubstituteCodeFixProvider(),];

	private static readonly Lazy<Task<ImmutableArray<MetadataReference>>> References = new(() =>
		new CSharpCodeFixVerifier<MoqAnalyzer, MoqCodeFixProvider>.Test().ReferenceAssemblies
			.ResolveAsync(LanguageNames.CSharp, CancellationToken.None));

	/// <summary>
	///     Applies the code fix for the diagnostic <paramref name="id" /> to the <paramref name="before" /> code and
	///     returns the result next to the <paramref name="after" /> code, both unwrapped and normalized.
	/// </summary>
	public static async Task<Result> ApplyAsync(string id, string before, string after)
	{
		CodeFixProvider? codeFix = CodeFixes.FirstOrDefault(fix => fix.FixableDiagnosticIds.Contains(id));
		if (codeFix is null || !Libraries.TryGetValue(id, out string? library))
		{
			return new Result("", "", [$"There is no code fix for {id}.",]);
		}

		string expected = Unwrap(Wrap(library, after));
		using AdhocWorkspace workspace = new();
		Document document = await CreateDocument(workspace, Wrap(library, before));
		List<string> errors = await GetCompilerErrors(document);
		if (errors.Count > 0)
		{
			return new Result(expected, "", errors);
		}

		for (int i = 0; i < MaxFixes; i++)
		{
			Diagnostic? diagnostic = await GetDiagnostic(document, id);
			if (diagnostic is null)
			{
				return new Result(expected, Unwrap((await document.GetTextAsync()).ToString()), []);
			}

			document = await Fix(codeFix, document, diagnostic);
		}

		return new Result(expected, Unwrap((await document.GetTextAsync()).ToString()),
			[$"{id} is still reported after {MaxFixes} code fixes.",]);
	}

	private static string Wrap(string library, string code)
	{
		CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(code, ParseOptions).GetCompilationUnitRoot();
		StringBuilder source = new();
		source.Append("using ").Append(library).Append(";\n");
		foreach (UsingDirectiveSyntax directive in root.Usings)
		{
			source.Append(directive.ToFullString());
		}

		source.Append("\npublic class ").Append(ExampleClass)
			.Append("\n{\npublic async System.Threading.Tasks.Task Run()\n{\n");
		foreach (GlobalStatementSyntax statement in root.Members.OfType<GlobalStatementSyntax>())
		{
			source.Append(statement.ToFullString());
		}

		source.Append("\n}\n}\n");
		foreach (MemberDeclarationSyntax member in root.Members.Where(member => member is not GlobalStatementSyntax))
		{
			source.Append('\n').Append(member.ToFullString());
		}

		return source.ToString();
	}

	private static string Unwrap(string document)
	{
		CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(document, ParseOptions).GetCompilationUnitRoot();
		List<string> parts = [];
		foreach (MemberDeclarationSyntax member in root.Members)
		{
			if (member is ClassDeclarationSyntax { Identifier.Text: ExampleClass, } example)
			{
				BlockSyntax body = example.Members.OfType<MethodDeclarationSyntax>().Single().Body!;
				parts.Add(document[body.OpenBraceToken.Span.End..body.CloseBraceToken.SpanStart]);
			}
			else
			{
				parts.Add(member.ToFullString());
			}
		}

		return Normalize(string.Join("\n", parts));
	}

	private static string Normalize(string code)
		=> string.Join("\n", code.Replace("\r\n", "\n").Split('\n').Select(line => line.TrimEnd())).Trim('\n');

	private static async Task<Document> CreateDocument(AdhocWorkspace workspace, string source)
	{
		ProjectId projectId = ProjectId.CreateNewId();
		DocumentId documentId = DocumentId.CreateNewId(projectId);
		Solution solution = workspace.CurrentSolution
			.AddProject(projectId, "Docs", "Docs", LanguageNames.CSharp)
			.WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
			.WithProjectParseOptions(projectId, ParseOptions)
			.AddMetadataReferences(projectId, await References.Value)
			.AddDocument(DocumentId.CreateNewId(projectId), "Scaffold.cs", Scaffold)
			.AddDocument(documentId, "Example.cs", source);
		return solution.GetDocument(documentId)!;
	}

	private static async Task<List<string>> GetCompilerErrors(Document document)
	{
		Compilation compilation = (await document.Project.GetCompilationAsync())!;
		return compilation.GetDiagnostics()
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.Select(diagnostic =>
				$"{diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)} in `{GetLine(diagnostic.Location)}`")
			.ToList();
	}

	private static string GetLine(Location location)
		=> location.SourceTree?.GetText().Lines[location.GetLineSpan().StartLinePosition.Line].ToString().Trim() ?? "";

	private static async Task<Diagnostic?> GetDiagnostic(Document document, string id)
	{
		Compilation compilation = (await document.Project.GetCompilationAsync())!;
		SyntaxTree tree = (await document.GetSyntaxTreeAsync())!;
		ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(Analyzers).GetAnalyzerDiagnosticsAsync();
		return diagnostics.FirstOrDefault(diagnostic => diagnostic.Id == id && diagnostic.Location.SourceTree == tree);
	}

	private static async Task<Document> Fix(CodeFixProvider codeFix, Document document, Diagnostic diagnostic)
	{
		List<CodeAction> actions = [];
		await codeFix.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic,
			(action, _) => actions.Add(action), CancellationToken.None));
		if (actions.Count == 0)
		{
			return document;
		}

		ImmutableArray<CodeActionOperation> operations = await actions[0].GetOperationsAsync(CancellationToken.None);
		return operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;
	}

	/// <summary>
	///     The <paramref name="Expected" /> code after the migration, the <paramref name="Actual" /> result of the code
	///     fix and the <paramref name="Errors" /> that kept the code fix from running to the end.
	/// </summary>
	public sealed record Result(string Expected, string Actual, IReadOnlyList<string> Errors);
}
