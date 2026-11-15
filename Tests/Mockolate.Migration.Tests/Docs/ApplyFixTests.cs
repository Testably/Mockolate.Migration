using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mockolate.Migration.Tests.Docs;

public sealed class ApplyFixTests
{
	private static readonly string PagesDirectory = GetPagesDirectory();

	public static IEnumerable<ApplyFixBox> Boxes()
		=> Directory.EnumerateFiles(PagesDirectory, "*.md*", SearchOption.AllDirectories)
			.Order(StringComparer.Ordinal)
			.SelectMany(path => ApplyFixBox.Extract(
				$"Docs/pages/{Path.GetRelativePath(PagesDirectory, path).Replace('\\', '/')}",
				File.ReadAllLines(path)));

	[Test]
	[MethodDataSource(nameof(Boxes))]
	public async Task ApplyFix_ShouldShowTheResultOfTheCodeFix(ApplyFixBox box)
	{
		Fail.Unless(box.Blocks.Count == 2,
			$"The ApplyFix box at {box} must contain two csharp blocks, but contains {box.Blocks.Count}.");

		CodeFixRunner.Result result = await CodeFixRunner.ApplyAsync(box.Id, box.Blocks[0], box.Blocks[1]);

		Fail.Unless(result.Errors.Count == 0,
			$"The code fix could not migrate the code of the ApplyFix box at {box}:\n{string.Join("\n", result.Errors)}");
		Fail.Unless(result.Actual == result.Expected,
			$"The code after the migration in the ApplyFix box at {box} differs from the result of the code fix.\n" +
			$"Expected (page):\n{result.Expected}\n\nActual (code fix):\n{result.Actual}");
	}

	[Test]
	public async Task Boxes_ShouldFindTheApplyFixBoxesOfAllPages()
	{
		int count = Boxes().Count();

		await That(count).IsGreaterThanOrEqualTo(4)
			.Because("the check must not pass because it no longer finds the ApplyFix boxes");
	}

	[Test]
	public async Task Extract_ShouldFindBothBlocksOfABox()
	{
		string[] lines =
		[
			"```csharp",
			"int outside = 0;",
			"```",
			"<ApplyFix id=\"MockolateM001\" migrateTo=\"Mockolate\">",
			"",
			"```csharp title=\"Before (Moq)\"",
			"int before = 1;",
			"```",
			"",
			"```csharp title=\"After (Mockolate)\"",
			"int after = 2;",
			"```",
			"",
			"</ApplyFix>",
		];

		List<ApplyFixBox> boxes = ApplyFixBox.Extract("page.md", lines);

		await That(boxes).HasSingle().Which.Satisfies(box => box.Page == "page.md" && box.Line == 4 && box.Id == "MockolateM001");
		await That(boxes[0].Blocks).IsEqualTo(["int before = 1;", "int after = 2;",])
			.Because("blocks outside of an ApplyFix box are not part of it");
	}

	[Test]
	public async Task Extract_ShouldRemoveTheHighlightCommentLines()
	{
		string[] lines =
		[
			"<ApplyFix id=\"MockolateM002\" migrateTo=\"Mockolate\">",
			"```csharp",
			"// highlight-next-line",
			"int a = 1;",
			"  // highlight-start",
			"int b = 2;",
			"// highlight-end",
			"// a comment",
			"```",
			"</ApplyFix>",
		];

		List<ApplyFixBox> boxes = ApplyFixBox.Extract("page.md", lines);

		await That(boxes).HasSingle().Which.Satisfies(box => box.Blocks.Count == 1);
		await That(boxes[0].Blocks[0]).IsEqualTo("int a = 1;\nint b = 2;\n// a comment");
	}

	private static string GetPagesDirectory()
	{
		DirectoryInfo? directory = new(AppContext.BaseDirectory);
		while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Docs", "pages")))
		{
			directory = directory.Parent;
		}

		return directory is null
			? throw new DirectoryNotFoundException($"No Docs/pages above {AppContext.BaseDirectory}")
			: Path.Combine(directory.FullName, "Docs", "pages");
	}
}
