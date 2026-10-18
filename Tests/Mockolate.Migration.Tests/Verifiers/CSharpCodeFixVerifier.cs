using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Mockolate.SourceGenerators;

namespace Mockolate.Migration.Tests.Verifiers;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TCodeFix : CodeFixProvider, new()
{
	public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
	{
		public Test()
		{
			ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(
			[
				new PackageIdentity("Moq", "4.20.72"),
				new PackageIdentity("NSubstitute", "5.3.0"),
				new PackageIdentity("Mockolate", typeof(MockRegistry).Assembly.GetName().Version!.ToString(3)),
			]);
			TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
			SolutionTransforms.Add((solution, projectId) =>
			{
				Project? project = solution.GetProject(projectId);

				if (project is null)
				{
					return solution;
				}

				CompilationOptions? compilationOptions = project.CompilationOptions;

				if (compilationOptions is null)
				{
					return solution;
				}

				CSharpParseOptions? parseOptions = project.ParseOptions as CSharpParseOptions;

				if (parseOptions is null)
				{
					return solution;
				}

				compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
					compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));

				solution = solution.WithProjectCompilationOptions(projectId, compilationOptions)
					.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion.Preview));

				return solution;
			});
		}

		protected override IEnumerable<Type> GetSourceGenerators()
			=> [typeof(MockGenerator),];
	}
}
