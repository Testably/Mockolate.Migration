using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RegexMatch = System.Text.RegularExpressions.Match;

namespace Mockolate.Migration.Tests.Docs;

/// <summary>
///     An <c>&lt;ApplyFix id="…"&gt;</c> box of a documentation page that starts at the one-based
///     <paramref name="Line" />, with the code of its <c>```csharp</c> blocks.
/// </summary>
/// <remarks>
///     The docs site shows the first block as the code before the migration and the second block, behind a "Migrate"
///     button, as the code after it.
/// </remarks>
public sealed partial record ApplyFixBox(string Page, int Line, string Id, IReadOnlyList<string> Blocks)
{
	/// <summary>
	///     Extracts the <c>ApplyFix</c> boxes from the <paramref name="lines" /> of the <paramref name="page" />, without
	///     the Docusaurus highlight comment lines of their blocks.
	/// </summary>
	public static List<ApplyFixBox> Extract(string page, string[] lines)
	{
		List<ApplyFixBox> boxes = [];
		ApplyFixBox? box = null;
		int i = 0;
		while (i < lines.Length)
		{
			string line = lines[i++];
			RegexMatch opening =Opening().Match(line);
			if (opening.Success)
			{
				box = new ApplyFixBox(page, i, opening.Groups["id"].Value, []);
			}
			else if (box is not null && Closing().IsMatch(line))
			{
				boxes.Add(box);
				box = null;
			}
			else if (Fence().Match(line) is { Success: true, } fence)
			{
				string? code = ReadBlock(lines, ref i, fence);
				if (box is not null && code is not null)
				{
					box = box with { Blocks = [..box.Blocks, code,], };
				}
			}
		}

		return boxes;
	}

	/// <inheritdoc />
	public override string ToString()
		=> $"{Page}:{Line}";

	private static string? ReadBlock(string[] lines, ref int i, RegexMatch fence)
	{
		string marker = fence.Groups["fence"].Value;
		int start = i;
		while (i < lines.Length && !IsClosing(lines[i], marker))
		{
			i++;
		}

		int end = i++;
		if (!fence.Groups["info"].Value.TrimStart().StartsWith("csharp", StringComparison.Ordinal))
		{
			return null;
		}

		return string.Join("\n", lines[start..end].Where(line => !Highlight().IsMatch(line)));
	}

	private static bool IsClosing(string line, string fence)
	{
		string trimmed = line.Trim();
		return trimmed.Length >= fence.Length && trimmed.All(c => c == fence[0]);
	}

	[GeneratedRegex("""^\s*<ApplyFix\b[^>]*\bid="(?<id>[^"]+)"[^>]*>\s*$""")]
	private static partial Regex Opening();

	[GeneratedRegex(@"^\s*</ApplyFix>\s*$")]
	private static partial Regex Closing();

	[GeneratedRegex(@"^\s*(?<fence>`{3,}|~{3,})(?<info>.*)$")]
	private static partial Regex Fence();

	[GeneratedRegex(@"^\s*//\s*highlight-(next-line|start|end)\s*$")]
	private static partial Regex Highlight();
}
