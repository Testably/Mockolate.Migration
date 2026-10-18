# Architecture and Source Locations

Product code lives in `Source/`, tests and playgrounds in `Tests/`, the build pipeline in `Pipeline/`, CI in `.github/workflows/` and docs in `Docs/`. All projects are listed in `Mockolate.Migration.slnx`.

| Project | Responsibility |
|---|---|
| `Source/Mockolate.Migration.Analyzers` | `MoqAnalyzer` and `NSubstituteAnalyzer`, the diagnostic descriptors in `Rules.cs`, the texts in `Resources.resx` and the release tracking files `AnalyzerReleases.*.md`. |
| `Source/Mockolate.Migration.Analyzers.CodeFixers` | `MoqCodeFixProvider` and `NSubstituteCodeFixProvider` on top of the shared base `AssertionCodeFixProvider`. Uses the root namespace `Mockolate.Migration.Analyzers`. |
| `Source/Mockolate.Migration` | The packable project. `Pack` collects the `.nupkg` and `.snupkg` from its `bin` folder. |
| `Tests/Mockolate.Migration.Tests` | Analyzer and code fix tests, plus the verifier helpers in `Verifiers/`. |
| `Tests/Mockolate.Migration.MoqPlayground` | Realistic Moq tests with the analyzers attached as analyzer references. |
| `Tests/Mockolate.Migration.NSubstitutePlayground` | The same for NSubstitute. |
| `Tests/Mockolate.Migration.Example.Tests` | A Mockolate example of the expected migration result (`MoqMigrationExamples.cs`). Not run by the pipeline. |
| `Pipeline/Build.csproj` | The Nuke build, started by `build.ps1`, `build.sh` and `build.cmd`. |

Both analyzer projects set `EnforceExtendedAnalyzerRules` and `IsRoslynComponent` and are not packable on their own. The analyzer project exposes its internals to the code fixer project.

## How a migration works

- An analyzer reports one diagnostic per mock creation: `MoqAnalyzer` on `new Mock<T>(...)` (also target-typed) and on Moq factory methods whose name ends in `Mock`, `NSubstituteAnalyzer` on `Substitute.For`, `ForPartsOf` and `ForTypeForwardingTo`.
- The base `AssertionCodeFixProvider` registers one code action per diagnostic (title from the descriptor, batch fix-all) and calls `ConvertAssertionAsync` with the flagged expression.
- The concrete fixer resolves the mock symbol, then collects every related call site in the compilation unit through `FindAndBuild...Replacements` methods (setups, property setups, callbacks, verifications, events, `.Object` accesses for Moq). All replacements are applied in one `ReplaceNodes` call, and `using Mockolate;` (plus `using Mockolate.Verify;` when needed) is added.
- When a rewrite is not safe, the original code stays and a `// TODO` comment is emitted (see `BuildNestedMockTodoTrivia` in the Moq fixer and the `CallInfo` handling in the NSubstitute fixer). The fixers detect the document's line ending before inserting trivia.

## Adding a migration end to end

Most changes extend an existing fixer; a new diagnostic is only needed for a new source library.

1. Write a failing test in the matching partial class of `MoqCodeFixProviderTests` or `NSubstituteCodeFixProviderTests` (for example `MoqCodeFixProviderTests.SetupTests.cs`). Source and expected code go into raw string literals, the diagnostic span is marked with `[|...|]`, and the test is verified with `Verifier.VerifyCodeFixAsync(source, fixedSource)`.
2. Implement the rewrite in `MoqCodeFixProvider.cs` or `NSubstituteCodeFixProvider.cs`: add a `FindAndBuild...Replacements` method or extend an existing one, and wire it into `ConvertAssertionAsync`.
3. If the analyzer has to flag something new, change the analyzer and add tests to `MoqAnalyzerTests.cs` or `NSubstituteAnalyzerTests.cs` using `Verifier.VerifyAnalyzerAsync`.
4. Cover the pattern in the matching Playground project. If the construct was listed in `UnsupportedFeatureTests.cs`, move it to the fitting test class there.
5. Update the "Supported ... migrations" table and, where useful, the examples in `README.md`.

For a new source library:

1. Add a descriptor to `Rules.cs` with the next free `MockolateM###` ID, add `<ID>Title`, `<ID>MessageFormat`, `<ID>Description` and `<ID>CodeFixTitle` to `Resources.resx` (regenerating `Resources.Designer.cs`), and record the rule in `AnalyzerReleases.Unshipped.md`.
2. Add an analyzer next to `MoqAnalyzer.cs` and a fixer deriving from `AssertionCodeFixProvider(Rules.<YourRule>)`.
3. Add the library to `Directory.Packages.props` and to the `ReferenceAssemblies` in the verifiers, add test classes and a Playground project, and register the Playground in `Mockolate.Migration.slnx` and in `UnitTestProjects` in `Pipeline/Build.UnitTest.cs`.
4. Document the diagnostic in the README table.
