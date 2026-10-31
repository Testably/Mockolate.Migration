# Mockolate.Migration Agent Guide

Mockolate.Migration ships Roslyn analyzers and code fixers that rewrite Moq and NSubstitute usage to [Mockolate](https://github.com/aweXpect/Mockolate). It is published as the `Mockolate.Migration` NuGet package.

## Design Goals

Correctness is a must. The other goals are strong defaults and win in this order when they conflict: stable and compatible, host compatible, a consistent and pleasant API, a fast happy path, extensible, errors at compile time, documented. [Docs/agents/design-goals.md](Docs/agents/design-goals.md) explains each goal, the code rules, the checks before a commit and the goals of this repository.

## Project Constraints

- The SDK comes from `global.json`. Package versions are managed centrally in `Directory.Packages.props`; never put a version on a `PackageReference`.
- All projects under `Source/` target `netstandard2.0` and build with `TreatWarningsAsErrors` (`Source/Directory.Build.props`). Do not use APIs that are missing on `netstandard2.0`.
- The analyzers compile against `Microsoft.CodeAnalysis` 5.3.0. That version is the minimum compiler a consumer needs to load them, so raising it is a breaking change for users on older SDKs or IDEs.
- Diagnostic IDs follow `MockolateM###`: `MockolateM001` (Moq) and `MockolateM002` (NSubstitute). Descriptors live in `Rules.cs`, texts in `Resources.resx` (`<ID>Title`, `<ID>MessageFormat`, `<ID>Description`, `<ID>CodeFixTitle`), and every new ID goes into `AnalyzerReleases.Unshipped.md`.
- When a construct cannot be rewritten safely, keep the original code and emit a `// TODO` comment instead of guessing, as the nested-mock and `CallInfo` rewrites do.
- The Playground projects (`Tests/Mockolate.Migration.MoqPlayground`, `Tests/Mockolate.Migration.NSubstitutePlayground`) are real Moq and NSubstitute test suites with the analyzers attached. Keep them green against the source library; `UnsupportedFeatureTests.cs` lists what the migration does not handle yet.

## Validation

- Every change to an analyzer or code fixer needs tests in `Tests/Mockolate.Migration.Tests`, using the verifiers in `Verifiers/`: `[|...|]` marks the expected diagnostic span, and before/after code goes into C# raw string literals.
- Run the tests that cover your change, then the whole `Mockolate.Migration.Tests` project before pushing. CI also runs both Playground projects and `Mockolate.Migration.Example.Tests`.
- All tests run on TUnit and the Microsoft.Testing.Platform, which `global.json` selects for `dotnet test`: build the solution, then pass the project with `--project` and `--no-build`, and filter with `--treenode-filter` ([Workflows](Docs/agents/workflows.md)).
- The Moq and NSubstitute versions in ``Verifiers/CSharpCodeFixVerifier`2.cs`` and ``CSharpAnalyzerVerifier`1.cs`` are hard-coded; keep them in sync with `Directory.Packages.props`.
- A new or changed migration updates the tables in `README.md` and on the docs site page `Docs/pages/11-migration.md`.
- Keep each file's line endings and BOM as they are. Files are LF in git and CRLF in a Windows checkout; a bare LF inside a CRLF file changes the content of raw string literals. PowerShell rewrites can add or strip a UTF-8 BOM, so check `git diff` for U+FEFF.
- No em-dashes in new docs, XML docs or commit messages.

## Git Workflow

- Branch from `main` and open a pull request; PRs are squash-merged.
- PR titles and commit subjects follow conventional commits (`feat:`, `fix:`, `docs:`, `ci:`, `chore:`, `coverage:`). The commit subject is the PR title; the body explains why, with each paragraph on a single line. No co-author or attribution trailers.
- Sonar must report no new issues and coverage must stay above 90% (`CONTRIBUTING.md`).

## References

- [Design goals](Docs/agents/design-goals.md): the goals and code rules for every change, the checks before a commit and the goals of this repository.
- [Workflows](Docs/agents/workflows.md): build, test and documentation commands, pipeline targets, CI.
- [Project structure](Docs/agents/project-structure.md): source locations and how to add a migration end to end.
