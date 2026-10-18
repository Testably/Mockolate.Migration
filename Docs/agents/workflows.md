# Development Workflows

## Build and test

Build and test from the repository root with the SDK pinned in `global.json`:

```bash
dotnet build Mockolate.Migration.slnx
dotnet test
dotnet test --project Tests/Mockolate.Migration.Tests --no-build
dotnet test --project Tests/Mockolate.Migration.MoqPlayground --no-build
dotnet test --project Tests/Mockolate.Migration.NSubstitutePlayground --no-build
```

Test projects target `net10.0` and use TUnit with aweXpect. They run on the Microsoft.Testing.Platform, which `global.json` selects for `dotnet test`: without arguments it runs the whole solution, a single project is passed with `--project`. Pass `--no-build` after building the solution, because building a single test project fails with `NU5039` (the packable `Source/Mockolate.Migration` only finds the README when it is built as part of the solution).

Filter with `--treenode-filter` (`/<assembly>/<namespace>/<class>/<test>`). Code fix tests are nested classes, so the class name uses `+` between the outer and the inner class:

```bash
dotnet test --project Tests/Mockolate.Migration.Tests --no-build --treenode-filter "/*/*/MoqAnalyzerTests/*"
dotnet test --project Tests/Mockolate.Migration.Tests --no-build --treenode-filter "/*/*/MoqCodeFixProviderTests+SetupTests/*"
dotnet test --project Tests/Mockolate.Migration.Tests --no-build --treenode-filter "/*/*/NSubstituteCodeFixProviderTests+CallInfoTests+*/*"
```

A full solution build compiles the analyzer projects several times in parallel (once per analyzer reference). If it fails once with `MSB4018` in `GenerateDepsFile`, build again. Building the Playground projects reports `MockolateM001`/`MockolateM002` warnings; that is intended.

## Build pipeline

`Pipeline/Build.csproj` is a Fallout build. Run it with `./build.ps1 <Target>` (Windows) or `./build.sh <Target>`; `./build.ps1 --help` lists the targets.

| Target | What it does |
|---|---|
| `Compile` | Cleans `Artifacts/` and `TestResults/`, restores, calculates the version with GitVersion and builds the solution. |
| `UnitTests` | Runs the test projects in `UnitTestProjects` (`Pipeline/Build.UnitTest.cs`): both Playgrounds, `Mockolate.Migration.Example.Tests` and `Mockolate.Migration.Tests`, with `dotnet test` on every target framework, each writing `<project>_<framework>.trx` and `<project>_<framework>.cobertura.xml` to `TestResults/`. |
| `CodeCoverage` | `UnitTests`, then merges the coverage into `TestResults/reports/SonarQube.xml`. |
| `UpdateReadme` | Writes `Artifacts/README.md` with a changelog badge for the package. |
| `Pack` | `UpdateReadme` and `Compile`, then moves the packages of `Source/Mockolate.Migration` to `Artifacts/Packages`. Fails when a `.nupkg` contains no `analyzers/dotnet/cs/*.dll`. |
| `CodeAnalysis` | The default target. Wraps `Compile` and `CodeCoverage` in a SonarCloud analysis; needs a Sonar token. |

Locally the pipeline builds in `Debug`. On Windows, `Compile` then fails because it rebuilds `Pipeline/Build.csproj` while that build is running; pass `--configuration Release` (as CI does implicitly):

```powershell
./build.ps1 UnitTests --configuration Release
```

For day-to-day work, `dotnet build` and `dotnet test` are enough.

## CI workflows

- `.github/workflows/ci.yml` runs on pull requests to `main`: `./build.ps1 CodeCoverage` (or `build.sh`) on Ubuntu, Windows and macOS, SonarCloud analysis (skipped for Dependabot and forks), and a test result summary.
- `.github/workflows/build.yml` runs on pushes to `main` and on `v*` tags: the same test and analysis jobs plus `Pack`. For a tag, the `push` job publishes the packages to nuget.org through NuGet trusted publishing, creates a GitHub release for non-pre-release tags, and `finalize-release` comments on the released issues and pull requests.
- `.github/workflows/notify-docs-site.yml` triggers a rebuild of the docs site in `Testably/Testably.Site` after a successful `Build` on `main`.
- `.github/mergify.yml` squash-merges Dependabot PRs once the three unit test jobs pass.

## Documentation

`README.md` is the user documentation. `Docs/pages/00-index.md` embeds it through the `{README}` placeholder and is published to the docs site at https://docs.testably.org/Mockolate/Migration, so keep the README self-contained. Author and agent docs live in `Docs/agents/` and are not published.
