---
description: Migrate from Moq, NSubstitute or FakeItEasy to Mockolate, automatically with the code fixes of Mockolate.Migration or by hand with a side-by-side translation table.
---

import PackageInstall from '@site/src/components/PackageInstall';
import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Migration

Coming from **[Moq](https://github.com/devlooped/moq)**, **[NSubstitute](https://nsubstitute.github.io/)** or **[FakeItEasy](https://fakeiteasy.github.io/)**? There are two ways to move your tests over:

- **Automatically**: for Moq and NSubstitute, the `Mockolate.Migration` package reports every mock it can migrate and rewrites it with a code fix (see [Automated migration](#automated-migration)). What the code fix rewrites is described in [Migrating from Moq](./01-moq.md) and [Migrating from NSubstitute](./02-nsubstitute.md).
- **By hand**: the [side by side](#side-by-side) comparison shows the same test in each library, and the [translation table](#translation-table) translates the most common calls.

## Automated migration

`Mockolate.Migration` is a Roslyn analyzer with code fixes that migrate Moq and NSubstitute tests to [Mockolate](/Mockolate/). It automates the rewrites that the [translation table](#translation-table) shows: the analyzer reports every mock it can migrate, and the code fix rewrites the whole mock in one step.

### Installation

The migration package contains only the analyzer and the code fixes, no runtime code, and does not reference Mockolate. Add [Mockolate](../01-getting-started.mdx) to the test project you want to migrate yourself, so that the migrated code still compiles after you remove the migration package:

<PackageInstall package="Mockolate" />

Then add the migration package:

<PackageInstall package="Mockolate.Migration" developmentDependency />

### How it works

The analyzer reports each mock it can migrate, such as `new Mock<T>(…)` or `Substitute.For<T>(…)`, as a warning. The code fix rewrites the construction, all setups and verifications of that mock, the event wiring and, for Moq, every `.Object` access, and adds `using Mockolate;`.

| Diagnostic      | Source library                     | Code fix title                     |
|-----------------|------------------------------------|------------------------------------|
| `MockolateM001` | [Moq](./01-moq.md)                 | *Migrate Moq to Mockolate*         |
| `MockolateM002` | [NSubstitute](./02-nsubstitute.md) | *Migrate NSubstitute to Mockolate* |

What the code fix rewrites, with examples and the supported constructs, is described per source library in [Migrating from Moq](./01-moq.md) and [Migrating from NSubstitute](./02-nsubstitute.md).

### Recommended workflow

1. **Install Mockolate and the migration package** (see [Installation](#installation)). Your Moq or NSubstitute tests keep compiling next to it, and every mock the code fix can migrate is reported as warning `MockolateM001` or `MockolateM002`.
2. **Apply the code fix** in your IDE or from the command line (see below).
3. **Remove `using Moq;` or `using NSubstitute;`** from the migrated files once nothing else in them uses the source library. The code fix keeps the directive, and with Moq, `It` is ambiguous between `Moq.It` and `Mockolate.It` until you remove it.
4. **Build, run your tests and check [what needs manual review](#what-needs-manual-review).**
5. **Remove the source library and the migration package** (see [Removing the package](#removing-the-package)).

#### In the IDE

Apply the code fix in your IDE (Visual Studio, Rider or VS Code with C# Dev Kit). One code fix migrates one mock; to migrate many mocks at once, use *Fix all occurrences* for the document, project or solution.

#### From the command line

Apply every available code fix in bulk:

```shell
dotnet format analyzers --diagnostics MockolateM001 MockolateM002
```

### What needs manual review

The code fix leaves a `// TODO(MockolateM001)` or `// TODO(MockolateM002)` comment where it cannot finish the migration on its own. Search for them after you apply the code fix:

- **Nested mocks**: Moq and NSubstitute create mocks for nested members (`m => m.Child.Prop`) automatically. Mockolate does not, so set up the parent to return a mock for `Child` yourself.
- **Moq sequences whose steps are not chained to `SetupSequence(…)`**, for example when the setup is stored in a variable. After the last step, Moq returns the default value (or does nothing for a `void` method), while a Mockolate [sequence](../05-setup/01-methods.md#sequences) starts over. End the sequence as the comment describes.
- **[CallInfo callbacks](./02-nsubstitute.md#callinfo-callbacks)** that cannot be rewritten safely, for example when they use `call` directly, compute the index at runtime, assign `out` or `ref` arguments, call an ambiguous `call.Arg<T>()`, or declare local variables with the same names as the parameters of the method.
- **Moq `Times` arguments that the code fix cannot translate**, for example a variable or a conditional expression. The Moq verification stays as it is.

The code fix does not mark these cases:

- **Call order**: the code fix removes Moq's `InSequence(…)` wrapper from the setups, so they no longer check the order of the calls (see the [behavioral differences of Moq](./01-moq.md#behavioral-differences)).
- **Moq verifications with both a `Times` argument and a failure message**: the code fix keeps them as they are, so they no longer compile. Rewrite them by hand.
- **Constructs that are not in the tables of supported [Moq](./01-moq.md#supported-moq-constructs) and [NSubstitute](./02-nsubstitute.md#supported-nsubstitute-constructs) constructs**, for example Moq's `Verifiable()`, `VerifyAll()`, `Protected()`, `As<T>()` or `Mock.Of<T>()`, and NSubstitute's `Arg.Do<T>(…)`, `Received.InOrder(…)` or `Configure()`. Rewrite them by hand; the [translation table](#translation-table) shows the Mockolate equivalents.

Migrated code can also compile and still behave differently; check the behavioral differences of [Moq](./01-moq.md#behavioral-differences) and [NSubstitute](./02-nsubstitute.md#behavioral-differences).

### Suppressing the warning

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

### Removing the package

Once no warning is left, remove Moq or NSubstitute, and then the migration package, which only adds analyzer overhead from here on:

```shell
dotnet remove package Moq
dotnet remove package NSubstitute
dotnet remove package Mockolate.Migration
```

Mockolate stays in the project; that is the library the tests now depend on.

## Side by side

Select the library you know; the choice applies to all examples on this page. All examples use this interface:

```csharp
public delegate void ChocolateDispensedDelegate(string type, int amount);
public interface IChocolateDispenser
{
    int this[string type] { get; set; }
    int TotalDispensed { get; set; }
    bool Dispense(string type, int amount);
    event ChocolateDispensedDelegate ChocolateDispensed;
}
```

<Tabs groupId="mocking-library">
<TabItem value="moq" label="Moq" default>

```csharp title="Moq"
// Setup
Mock<IChocolateDispenser> mock = new Mock<IChocolateDispenser>();
mock.Setup(x => x.Dispense("Dark", 2)).Returns(true);
mock.SetupGet(x => x["Dark"]).Returns(10);

// Usage
int available = mock.Object["Dark"];
bool success = mock.Object.Dispense("Dark", 2);
mock.Raise(m => m.ChocolateDispensed += null, "Dark", 2);

// Verification
mock.Verify(x => x.Dispense("Dark", 2), Times.Once());
mock.VerifyGet(x => x["Dark"], Times.Once());
```

</TabItem>
<TabItem value="nsubstitute" label="NSubstitute">

```csharp title="NSubstitute"
// Setup
IChocolateDispenser substitute = Substitute.For<IChocolateDispenser>();
substitute.Dispense("Dark", 2).Returns(true);
substitute["Dark"].Returns(10);

// Usage
int available = substitute["Dark"];
bool success = substitute.Dispense("Dark", 2);
substitute.ChocolateDispensed += Raise.Event<ChocolateDispensedDelegate>("Dark", 2);

// Verification
substitute.Received(1).Dispense("Dark", 2);
_ = substitute.Received(1)["Dark"];
```

</TabItem>
<TabItem value="fakeiteasy" label="FakeItEasy">

```csharp title="FakeItEasy"
// Setup
IChocolateDispenser fake = A.Fake<IChocolateDispenser>();
A.CallTo(() => fake.Dispense("Dark", 2)).Returns(true);
A.CallTo(() => fake["Dark"]).Returns(10);

// Usage
int available = fake["Dark"];
bool success = fake.Dispense("Dark", 2);
fake.ChocolateDispensed += Raise.FreeForm.With("Dark", 2);

// Verification
A.CallTo(() => fake.Dispense("Dark", 2)).MustHaveHappenedOnceExactly();
A.CallTo(() => fake["Dark"]).MustHaveHappenedOnceExactly();
```

</TabItem>
</Tabs>

```csharp title="Mockolate"
// Setup
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense("Dark", 2).Returns(true);
dispenser.Mock.Setup["Dark"].Returns(10);

// Usage
int available = dispenser["Dark"];
bool success = dispenser.Dispense("Dark", 2);
dispenser.Mock.Raise.ChocolateDispensed("Dark", 2);

// Verification
dispenser.Mock.Verify.Dispense("Dark", 2).Once();
dispenser.Mock.Verify["Dark"].Got().Once();
```

## Translation table

<Tabs groupId="mocking-library">
<TabItem value="moq" label="Moq" default>

| Moq                                                   | Mockolate                                                       |
|-------------------------------------------------------|-----------------------------------------------------------------|
| `new Mock<T>()`                                       | `T.CreateMock()`                                                |
| `mock.Object`                                         | the mock itself                                                 |
| `new Mock<T>(MockBehavior.Strict)`                    | `T.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup())`     |
| `new Mock<T>(arg1, arg2)`                             | `T.CreateMock([arg1, arg2])`                                    |
| `mock.As<IOther>()`                                   | `T.CreateMock().Implementing<IOther>()`                         |
| `mock.CallBase = true`                                | the default for class mocks; turn it off with `SkippingBaseClass()` |
| `It.IsAny<T>()`                                       | `It.IsAny<T>()`                                                 |
| `It.Is<T>(x => …)`                                    | `It.Satisfies<T>(x => …)`                                       |
| `It.IsIn(a, b)`, `It.IsInRange(a, b, Range.Inclusive)` | `It.IsOneOf(a, b)`, `It.IsInRange(a, b)`                       |
| `It.IsRegex(pattern)`                                 | `It.Matches(pattern).AsRegex()`                                 |
| `.Returns(…)`, `.ReturnsAsync(…)`, `.Throws<T>()`     | `.Returns(…)`, `.ReturnsAsync(…)`, `.Throws<T>()`               |
| `SetupSequence(…).Returns(a).Returns(b)`              | `Setup.X(…).Returns(a).Returns(b)`                              |
| `.Callback(…)`                                        | `.Do(…)`                                                        |
| `mock.SetupProperty(x => x.P, 42)`                    | `Mock.Setup.P.InitializeWith(42)`                               |
| `mock.SetupGet(x => x.P).Returns(42)`                 | `Mock.Setup.P.Returns(42)`                                      |
| `mock.Protected().Setup<bool>("Name", …)`             | `Mock.SetupProtected.Name(…)`                                   |
| `mock.Raise(x => x.E += null, …)`                     | `Mock.Raise.E(…)`                                               |
| `mock.Verify(x => x.M(…), Times.Once())`              | `Mock.Verify.M(…).Once()`                                       |
| `Times.Never()`, `Times.AtLeastOnce()`, `Times.Exactly(n)` | `.Never()`, `.AtLeastOnce()`, `.Exactly(n)`                |
| `mock.VerifyGet(x => x.P)`, `mock.VerifySet(x => x.P = 42)` | `Mock.Verify.P.Got()`, `Mock.Verify.P.Set(42)`            |
| `mock.VerifyNoOtherCalls()`                           | `Mock.VerifyThatAllInteractionsAreVerified()` (returns a `bool`) |

</TabItem>
<TabItem value="nsubstitute" label="NSubstitute">

| NSubstitute                                           | Mockolate                                                       |
|-------------------------------------------------------|-----------------------------------------------------------------|
| `Substitute.For<T>()`                                 | `T.CreateMock()`                                                |
| `Substitute.ForPartsOf<T>()`                          | `T.CreateMock()`; class mocks call the base class by default    |
| `Substitute.For<T, IOther>()`                         | `T.CreateMock().Implementing<IOther>()`                         |
| `Arg.Any<T>()`                                        | `It.IsAny<T>()`                                                 |
| `Arg.Is<T>(x => …)`                                   | `It.Satisfies<T>(x => …)`                                       |
| `Arg.Do<T>(x => …)`                                   | `It.IsAny<T>().Do(x => …)`                                      |
| `.Returns(a)`                                         | `Mock.Setup.M(…).Returns(a)`                                    |
| `.Returns(a, b)`                                      | `.Returns(a).Returns(b).Forever()`                              |
| `.Returns(callInfo => …)`                             | `.Returns((arg1, arg2) => …)`                                   |
| `.Throws(exception)`                                  | `.Throws(exception)`                                            |
| `.When(x => x.M(…)).Do(…)`, `.AndDoes(…)`             | `Mock.Setup.M(…).Do(…)`                                         |
| `Raise.Event<TDelegate>(…)`                           | `Mock.Raise.E(…)`                                               |
| `Received(n)`                                         | `.Exactly(n)`                                                   |
| `Received()`, `DidNotReceive()`                       | `.AtLeastOnce()`, `.Never()`                                    |
| `Received.InOrder(…)`                                 | `.Then(…)`                                                      |
| `ClearReceivedCalls()`                                | `Mock.ClearAllInteractions()`                                   |

</TabItem>
<TabItem value="fakeiteasy" label="FakeItEasy">

| FakeItEasy                                            | Mockolate                                                       |
|-------------------------------------------------------|-----------------------------------------------------------------|
| `A.Fake<T>()`                                         | `T.CreateMock()`                                                |
| `A.Fake<T>(o => o.Strict())`                          | `T.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup())`     |
| `A.Fake<T>(o => o.Wrapping(real))`                    | `T.CreateMock().Wrapping(real)`                                 |
| `A.Fake<T>(o => o.Implements<IOther>())`              | `T.CreateMock().Implementing<IOther>()`                         |
| `A<T>._`, `A<T>.Ignored`                              | `It.IsAny<T>()`                                                 |
| `A<T>.That.Matches(x => …)`                           | `It.Satisfies<T>(x => …)`                                       |
| `A.CallTo(() => fake.M(…)).Returns(a)`                | `Mock.Setup.M(…).Returns(a)`                                    |
| `.ReturnsLazily((T1 a, T2 b) => …)`                   | `.Returns((a, b) => …)`                                         |
| `.ReturnsNextFromSequence(a, b)`                      | `.Returns(a).Returns(b)`                                        |
| `.Throws<T>()`                                        | `.Throws<T>()`                                                  |
| `.Invokes(…)`                                         | `.Do(…)`                                                        |
| `.CallsBaseMethod()`                                  | the default for class mocks                                     |
| `Raise.FreeForm.With(…)`                              | `Mock.Raise.E(…)`                                               |
| `MustHaveHappenedOnceExactly()`                       | `.Once()`                                                       |
| `MustHaveHappened()`, `MustNotHaveHappened()`         | `.AtLeastOnce()`, `.Never()`                                    |
| `MustHaveHappened(n, Times.Exactly)`                  | `.Exactly(n)`                                                   |
| `.MustHaveHappened().Then(…)`                         | `.Then(…)`                                                      |

</TabItem>
</Tabs>

:::warning[Differences to watch out for]

- A `Returns`/`Throws` [sequence](../05-setup/01-methods.md#sequences) starts over after its last entry. Mark the last
  entry with `.Forever()` to keep repeating it instead.
- Class mocks call the base class implementation by default, even when a setup matches. See
  [Core concepts](/Mockolate/core-concepts/#what-happens-when-the-mock-is-called).
- A later setup with the same arguments replaces an earlier one, so chain everything that belongs to one setup.

:::

For the complete Mockolate syntax on one page, see the [cheat sheet](../02-core-concepts/02-cheat-sheet.md).
