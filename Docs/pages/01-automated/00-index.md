---
description: Migrate existing Moq and NSubstitute tests to Mockolate automatically with the Mockolate.Migration code fixes.
---

import PackageInstall from '@site/src/components/PackageInstall';

# Automated migration

The [Mockolate.Migration](https://www.nuget.org/packages/Mockolate.Migration) package contains a Roslyn analyzer and code fixes that migrate [Moq](https://github.com/devlooped/moq) and [NSubstitute](https://nsubstitute.github.io/) mocks to [Mockolate](/Mockolate/). The analyzer reports every mock it can migrate, and the code fix rewrites the whole mock in one step: the construction, all its setups and verifications, the event wiring and, for Moq, every `.Object` access.

| Diagnostic      | Source library                     | Code fix title                     |
|-----------------|------------------------------------|------------------------------------|
| `MockolateM001` | [Moq](./01-moq.md)                 | *Migrate Moq to Mockolate*         |
| `MockolateM002` | [NSubstitute](./02-nsubstitute.md) | *Migrate NSubstitute to Mockolate* |

## Installation

Install the package into the test project you want to migrate, next to [Mockolate](../../01-getting-started.mdx) itself:

<PackageInstall package="Mockolate.Migration" developmentDependency />

The package contains only the analyzer and the code fixes, no runtime code. You need it only while you migrate: once a project no longer uses Moq or NSubstitute, remove the reference again.

## Migrate your tests

After you install the package, each mock that can be migrated is reported as a warning (`MockolateM001` or `MockolateM002`). Apply the code fix in your IDE (Visual Studio, Rider or VS Code with C# Dev Kit). To migrate many mocks at once, use *Fix all occurrences* for the document, project or solution, or run the code fix from the command line:

```shell
dotnet format analyzers --diagnostics MockolateM001 MockolateM002
```

Then build, run your tests and check the [cases that need manual review](#what-needs-manual-review).

What the code fix rewrites, with examples and the supported constructs, is described per source library:

- [Migrating from Moq](./01-moq.md)
- [Migrating from NSubstitute](./02-nsubstitute.md)

## What needs manual review

The code fix leaves a `// TODO(MockolateM001)` or `// TODO(MockolateM002)` comment where it cannot finish the migration on its own. Search for them after you apply the fix:

- **Nested mocks**: Moq and NSubstitute create mocks for nested members (`m => m.Child.Prop`) automatically. Mockolate does not, so set up the parent to return a mock for `Child` yourself.
- **[CallInfo callbacks](./02-nsubstitute.md#callinfo-callbacks)** that cannot be rewritten safely, for example when they use `call` directly, compute the index at runtime, assign `out` or `ref` arguments, call an ambiguous `call.Arg<T>()`, or declare local variables with the same names as the parameters of the method.

The code fix does not mark these cases:

- **Call order**: the code fix removes Moq's `InSequence(…)` wrapper from the setups. Mockolate checks the order after the calls instead: add a [`Then(…)`](../../07-verify-interactions.md#call-order) chain to the verification.
- **Constructs that are not in the tables of supported [Moq](./01-moq.md#supported-moq-constructs) and [NSubstitute](./02-nsubstitute.md#supported-nsubstitute-constructs) constructs**, for example Moq's `Verifiable()`, `VerifyAll()`, `Protected()`, `As<T>()` or `Mock.Of<T>()`, and NSubstitute's `Arg.Do<T>(…)`, `Received.InOrder(…)` or `Configure()`. Rewrite them by hand; the [translation table](/Mockolate/migration/#translation-table) shows the Mockolate equivalents.
