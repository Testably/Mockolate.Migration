---
description: Migrate existing Moq and NSubstitute tests to Mockolate automatically with the analyzer and code fixes of Mockolate.Migration.
---

import PackageInstall from '@site/src/components/PackageInstall';

# Mockolate.Migration

`Mockolate.Migration` is a Roslyn analyzer with code fixes that migrate [Moq](https://github.com/devlooped/moq) and [NSubstitute](https://nsubstitute.github.io/) tests to [Mockolate](/Mockolate/). It automates the rewrites that the [migration guide](/Mockolate/migration/) shows side by side: the analyzer reports every mock it can migrate, and the code fix rewrites the whole mock in one step.

## Installation

The migration package contains only the analyzer and the code fixes, no runtime code, and does not reference Mockolate. Add [Mockolate](../../01-getting-started.mdx) to the test project you want to migrate yourself, so that the migrated code still compiles after you remove the migration package:

<PackageInstall package="Mockolate" />

Then add the migration package:

<PackageInstall package="Mockolate.Migration" developmentDependency />

## How it works

The analyzer reports each mock it can migrate, such as `new Mock<T>(…)` or `Substitute.For<T>(…)`, as a warning. The code fix rewrites the construction, all setups and verifications of that mock, the event wiring and, for Moq, every `.Object` access, and adds `using Mockolate;`.

| Diagnostic      | Source library                     | Code fix title                     |
|-----------------|------------------------------------|------------------------------------|
| `MockolateM001` | [Moq](./01-moq.md)                 | *Migrate Moq to Mockolate*         |
| `MockolateM002` | [NSubstitute](./02-nsubstitute.md) | *Migrate NSubstitute to Mockolate* |

What the code fix rewrites, with examples and the supported constructs, is described per source library in [Migrating from Moq](./01-moq.md) and [Migrating from NSubstitute](./02-nsubstitute.md).

## Recommended workflow

1. **Install Mockolate and the migration package** (see [Installation](#installation)). Your Moq or NSubstitute tests keep compiling next to it, and every mock the code fix can migrate is reported as warning `MockolateM001` or `MockolateM002`.
2. **Apply the code fix** in your IDE or from the command line (see below).
3. **Remove `using Moq;` or `using NSubstitute;`** from the migrated files once nothing else in them uses the source library. The code fix keeps the directive, and with Moq, `It` is ambiguous between `Moq.It` and `Mockolate.It` until you remove it.
4. **Build, run your tests and check [what needs manual review](#what-needs-manual-review).**
5. **Remove the source library and the migration package** (see [Removing the package](#removing-the-package)).

### In the IDE

Apply the code fix in your IDE (Visual Studio, Rider or VS Code with C# Dev Kit). One code fix migrates one mock; to migrate many mocks at once, use *Fix all occurrences* for the document, project or solution.

### From the command line

Apply every available code fix in bulk:

```shell
dotnet format analyzers --diagnostics MockolateM001 MockolateM002
```

## What needs manual review

The code fix leaves a `// TODO(MockolateM001)` or `// TODO(MockolateM002)` comment where it cannot finish the migration on its own. Search for them after you apply the code fix:

- **Nested mocks**: Moq and NSubstitute create mocks for nested members (`m => m.Child.Prop`) automatically. Mockolate does not, so set up the parent to return a mock for `Child` yourself.
- **Moq sequences whose steps are not chained to `SetupSequence(…)`**, for example when the setup is stored in a variable. After the last step, Moq returns the default value (or does nothing for a `void` method), while a Mockolate [sequence](../../05-setup/01-methods.md#sequences) starts over. End the sequence as the comment describes.
- **[CallInfo callbacks](./02-nsubstitute.md#callinfo-callbacks)** that cannot be rewritten safely, for example when they use `call` directly, compute the index at runtime, assign `out` or `ref` arguments, call an ambiguous `call.Arg<T>()`, or declare local variables with the same names as the parameters of the method.
- **Moq `Times` arguments that the code fix cannot translate**, for example a variable or a conditional expression. The Moq verification stays as it is.

The code fix does not mark these cases:

- **Call order**: the code fix removes Moq's `InSequence(…)` wrapper from the setups, so they no longer check the order of the calls (see the [behavioral differences of Moq](./01-moq.md#behavioral-differences)).
- **Moq verifications with both a `Times` argument and a failure message**: the code fix keeps them as they are, so they no longer compile. Rewrite them by hand.
- **Constructs that are not in the tables of supported [Moq](./01-moq.md#supported-moq-constructs) and [NSubstitute](./02-nsubstitute.md#supported-nsubstitute-constructs) constructs**, for example Moq's `Verifiable()`, `VerifyAll()`, `Protected()`, `As<T>()` or `Mock.Of<T>()`, and NSubstitute's `Arg.Do<T>(…)`, `Received.InOrder(…)` or `Configure()`. Rewrite them by hand; the [translation table](/Mockolate/migration/#translation-table) shows the Mockolate equivalents.

Migrated code can also compile and still behave differently; check the behavioral differences of [Moq](./01-moq.md#behavioral-differences) and [NSubstitute](./02-nsubstitute.md#behavioral-differences).

## Suppressing the warning

If you do not want to migrate a mock, suppress the warning at the call site:

```csharp
#pragma warning disable MockolateM001
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
#pragma warning restore MockolateM001
```

or for a file or folder with an entry in `.editorconfig`:

```text title=".editorconfig"
[**/Legacy/**.cs]
dotnet_diagnostic.MockolateM001.severity = none
dotnet_diagnostic.MockolateM002.severity = none
```

## Removing the package

Once no warning is left, remove Moq or NSubstitute, and then the migration package, which only adds analyzer overhead from here on:

```shell
dotnet remove package Moq
dotnet remove package NSubstitute
dotnet remove package Mockolate.Migration
```

Mockolate stays in the project; that is the library the tests now depend on.
