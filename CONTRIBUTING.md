# Contributor guide

## Pull requests

**Pull requests are welcome!**  
Please include a clear description of the changes you have made with your request; the title should follow
the [conventional commits](https://www.conventionalcommits.org/en/v1.0.0/) guideline.
All code should be covered by unit tests and comply with the coding guideline in this project.

### Technical expectations

As a framework for supporting unit testing, this project has a high standard for testing itself.  
In order to support this, static code analysis is performed
using [SonarCloud](https://sonarcloud.io/project/overview?id=Testably_Mockolate.Migration) with quality gate requiring to

- solve all issues reported by SonarCloud
- have a code coverage of > 90%

## Building and testing

`dotnet build Mockolate.Migration.slnx` builds everything; `./build.ps1 UnitTests --configuration Release` (or
`./build.sh UnitTests`) runs the tests like the CI.

All tests run on [TUnit](https://tunit.dev) and the Microsoft.Testing.Platform, which `global.json` selects for
`dotnet test`. After building the solution, pass a single project with `--project` and `--no-build`, and filter with
`--treenode-filter`:

```shell
dotnet test
dotnet test --project Tests/Mockolate.Migration.Tests --no-build
dotnet test --project Tests/Mockolate.Migration.Tests --no-build --treenode-filter "/*/*/MoqCodeFixProviderTests+SetupTests/*"
```
