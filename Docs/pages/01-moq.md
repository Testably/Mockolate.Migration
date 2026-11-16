---
description: How the Mockolate.Migration code fix rewrites Moq mocks, setups and verifications to Mockolate.
---

import ApplyFix from '@site/src/components/ApplyFix';

# Migrating from Moq

The `MockolateM001` code fix rewrites `new Mock<T>(…)` to `T.CreateMock(…)`, changes the declared type from `Mock<T>` to `T`, drops every `.Object` access and moves setups and verifications to `Mock.Setup` and `Mock.Verify`:

<ApplyFix id="MockolateM001" migrateTo="Mockolate">

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
dispenser.Setup(m => m.Dispense("Dark", It.IsAny<int>())).Returns(true);
dispenser.Setup(m => m.Dispense("Milk", It.IsAny<int>())).Returns(false);
VendingMachine sut = new VendingMachine(dispenser.Object);

sut.Buy("Dark", 4);
sut.Buy("Milk", 2);

dispenser.Verify(m => m.Dispense("Dark", 4), Times.Once());
dispenser.Verify(m => m.Dispense("Milk", It.IsAny<int>()), Times.Once());
dispenser.Verify(m => m.Dispense("White", It.IsAny<int>()), Times.Never());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);
dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>()).Returns(false);
VendingMachine sut = new VendingMachine(dispenser);

sut.Buy("Dark", 4);
sut.Buy("Milk", 2);

dispenser.Mock.Verify.Dispense("Dark", 4).Once();
dispenser.Mock.Verify.Dispense("Milk", It.IsAny<int>()).Once();
dispenser.Mock.Verify.Dispense("White", It.IsAny<int>()).Never();
```

</ApplyFix>

## Supported Moq constructs

The tables list the constructs the code fix rewrites. Everything else [needs manual review](./00-index.md#what-needs-manual-review). The examples use the `IChocolateDispenser` interface from the [migration overview](./00-index.md#side-by-side) and the `VendingMachine` from [Getting started](../01-getting-started.mdx); the code fix keeps your variable names, so the mock keeps its name and `sut` stays the system under test.

### Creating mocks

| Moq                                  | Mockolate                                                              |
|--------------------------------------|------------------------------------------------------------------------|
| `new Mock<T>()`                      | `T.CreateMock()`                                                       |
| `new Mock<T>(MockBehavior.Loose)`    | `T.CreateMock()`                                                       |
| `new Mock<T>(MockBehavior.Strict)`   | `T.CreateMock(Mockolate.MockBehavior.Default.ThrowingWhenNotSetup())`  |
| `mock.Object`                        | `mock` (the `.Object` access is dropped)                               |

The mock is created on the mocked type, and every `.Object` access is dropped, because the mock already implements the interface:

<ApplyFix id="MockolateM001" migrateTo="Mockolate">

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
VendingMachine sut = new VendingMachine(dispenser.Object);
IChocolateDispenser dispenserObject = dispenser.Object;
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
VendingMachine sut = new VendingMachine(dispenser);
IChocolateDispenser dispenserObject = dispenser;
```

</ApplyFix>

Strict mocks get a [behavior](../04-create-mocks.md#customizing-mock-behavior) that throws when no setup matches, written as `Mockolate.MockBehavior` because Moq has a `MockBehavior` as well:

<ApplyFix id="MockolateM001" migrateTo="Mockolate">

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>(MockBehavior.Strict);
dispenser.Setup(m => m.Dispense("Dark", 1)).Returns(true);
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock(Mockolate.MockBehavior.Default.ThrowingWhenNotSetup());
dispenser.Mock.Setup.Dispense("Dark", 1).Returns(true);
```

</ApplyFix>

### Setups

| Moq                                                       | Mockolate                                                              |
|-----------------------------------------------------------|------------------------------------------------------------------------|
| `mock.Setup(m => m.Method(args))`                         | `mock.Mock.Setup.Method(args)`                                         |
| `mock.Setup(m => m.Prop)`, `mock.SetupGet(m => m.Prop)`   | `mock.Mock.Setup.Prop`                                                 |
| `mock.SetupProperty(m => m.Prop)`                         | `mock.Mock.Setup.Prop.Register()`                                      |
| `mock.SetupProperty(m => m.Prop, value)`                  | `mock.Mock.Setup.Prop.InitializeWith(value)`                           |
| `mock.SetupSequence(…).Returns(a).Returns(b)`             | `mock.Mock.Setup.Method(…).Returns(a).Returns(b).Returns(default(T)).Forever()` |
| `mock.SetupSequence(…).Pass().Throws(e)` (void method)    | `mock.Mock.Setup.Method(…).DoesNotThrow().Throws(e).OnlyOnce()`        |
| `.Returns(…)`, `.ReturnsAsync(…)`, `.Throws(…)`           | kept on the Mockolate setup                                            |
| `.Callback(…)`                                            | `.Do(…)`                                                               |
| `mock.InSequence(sequence).Setup(…)`                      | `mock.Mock.Setup.Method(…)` (the `InSequence` wrapper is removed)      |

`Callback` becomes `Do`, and a `SetupSequence` becomes a setup with the same chain of `Returns`, which Mockolate uses as a [sequence](../05-setup/01-methods.md#sequences). A Mockolate sequence starts over after its last step, while Moq returns the default value, so the code fix ends the chain with `.Returns(default(T)).Forever()`:

<ApplyFix id="MockolateM001" migrateTo="Mockolate">

```csharp title="Before (Moq)"
// highlight-next-line
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
int dispensed = 0;
// highlight-start
dispenser.Setup(m => m.Dispense("Dark", It.IsAny<int>()))
    .Callback((string type, int amount) => dispensed += amount)
// highlight-end
    .Returns(true);
// highlight-next-line
dispenser.SetupSequence(m => m.Dispense("Milk", It.IsAny<int>()))
    .Returns(true)
    .Returns(false);
// highlight-next-line
dispenser.SetupProperty(m => m.TotalDispensed, 10);
```

```csharp title="After (Mockolate)"
// highlight-next-line
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
int dispensed = 0;
// highlight-start
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>())
    .Do((string type, int amount) => dispensed += amount)
// highlight-end
    .Returns(true);
// highlight-next-line
dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>())
    .Returns(true)
    .Returns(false)
// highlight-next-line
    .Returns(default(bool)).Forever();
// highlight-next-line
dispenser.Mock.Setup.TotalDispensed.InitializeWith(10);
```

</ApplyFix>

### Verifications

| Moq                                                       | Mockolate                                                              |
|-----------------------------------------------------------|------------------------------------------------------------------------|
| `mock.Verify(m => m.Method(args), Times.Once())`          | `mock.Mock.Verify.Method(args).Once()`                                 |
| `mock.Verify(m => m.Method(args))`                        | `mock.Mock.Verify.Method(args).AtLeastOnce()`                          |
| `mock.VerifyGet(m => m.Prop, times)`                      | `mock.Mock.Verify.Prop.Got()` with the count                           |
| `mock.VerifySet(m => m.Prop = value, times)`              | `mock.Mock.Verify.Prop.Set(value)` with the count                      |
| A non-literal `Times` argument (e.g. a variable)          | the Moq call is kept, with a `// TODO` comment                         |

The `Times` argument becomes a method call at the end of the verification, and a verification without `Times` checks for at least one call:

<ApplyFix id="MockolateM001" migrateTo="Mockolate">

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
dispenser.Verify(m => m.Dispense("Dark", 4));
dispenser.Verify(m => m.Dispense("Milk", It.IsAny<int>()), Times.Exactly(2));
dispenser.Verify(m => m.Dispense("White", It.IsAny<int>()), Times.AtMost(3));
dispenser.VerifyGet(m => m.TotalDispensed, Times.Once());
dispenser.VerifySet(m => m.TotalDispensed = 10, Times.Never());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Verify.Dispense("Dark", 4).AtLeastOnce();
dispenser.Mock.Verify.Dispense("Milk", It.IsAny<int>()).Exactly(2);
dispenser.Mock.Verify.Dispense("White", It.IsAny<int>()).AtMost(3);
dispenser.Mock.Verify.TotalDispensed.Got().Once();
dispenser.Mock.Verify.TotalDispensed.Set(10).Never();
```

</ApplyFix>

### Argument matchers

| Moq                                                       | Mockolate                                                              |
|-----------------------------------------------------------|------------------------------------------------------------------------|
| `It.IsAny<T>()`                                           | `It.IsAny<T>()`                                                        |
| `It.Is<T>(predicate)`                                     | `It.Satisfies<T>(predicate)`                                           |
| `It.IsInRange(a, b, Range.Inclusive)`                     | `It.IsInRange(a, b)`                                                   |
| `It.IsRegex(pattern)`                                     | `It.Matches(pattern).AsRegex().CaseSensitive()`                        |
| `It.IsRegex(pattern, options)`                            | `It.Matches(pattern).AsRegex(options).CaseSensitive()`                 |
| `It.Ref<T>.IsAny`, `out` parameters                       | `It.IsAnyRef<T>()`, `It.IsOut(() => value)`                            |

`It.IsAny` stays as it is, while `It.Is` with a predicate becomes `It.Satisfies` and `It.IsRegex` becomes `It.Matches(…).AsRegex(…)`. `It.Matches` ignores the case by default, so the code fix appends `CaseSensitive()` to match the case like Moq does; a `RegexOptions.IgnoreCase` option still applies. See [parameter matching](../06-parameter-matching.md) for the Mockolate matchers.

<ApplyFix id="MockolateM001" migrateTo="Mockolate">

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
dispenser.Setup(m => m.Dispense(It.Is<string>(type => type.StartsWith("D")), It.IsAny<int>())).Returns(true);
dispenser.Setup(m => m.Dispense(It.IsRegex("^M"), It.IsAny<int>())).Returns(false);
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense(It.Satisfies<string>(type => type.StartsWith("D")), It.IsAny<int>()).Returns(true);
dispenser.Mock.Setup.Dispense(It.Matches("^M").AsRegex().CaseSensitive(), It.IsAny<int>()).Returns(false);
```

</ApplyFix>

### Events

| Moq                                                        | Mockolate                                                              |
|------------------------------------------------------------|------------------------------------------------------------------------|
| `mock.Raise(m => m.Event += null, eventArgs)`              | `mock.Mock.Raise.Event(null, eventArgs)`                               |
| `mock.Raise(m => m.Event += null, args)` (custom delegate) | `mock.Mock.Raise.Event(args)`                                          |
| `mock.VerifyAdd(m => m.Event += …, times)`                 | `mock.Mock.Verify.Event.Subscribed()` with the count                   |
| `mock.VerifyRemove(m => m.Event -= …, times)`              | `mock.Mock.Verify.Event.Unsubscribed()` with the count                 |

Raising an event goes through `Mock.Raise`, and `VerifyAdd` and `VerifyRemove` verify the subscriptions:

<ApplyFix id="MockolateM001" migrateTo="Mockolate">

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
dispenser.Raise(m => m.ChocolateDispensed += null, "Dark", 2);
dispenser.VerifyAdd(m => m.ChocolateDispensed += It.IsAny<ChocolateDispensedDelegate>(), Times.Once());
dispenser.VerifyRemove(m => m.ChocolateDispensed -= It.IsAny<ChocolateDispensedDelegate>(), Times.Never());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Raise.ChocolateDispensed("Dark", 2);
dispenser.Mock.Verify.ChocolateDispensed.Subscribed().Once();
dispenser.Mock.Verify.ChocolateDispensed.Unsubscribed().Never();
```

</ApplyFix>

### Nested mocks

| Moq                                                       | Mockolate                                                              |
|-----------------------------------------------------------|------------------------------------------------------------------------|
| `mock.Setup(m => m.Child.Prop)`                           | `mock.Child.Mock.Setup.Prop` and a `// TODO` comment                   |

Moq creates the mock for `Child` automatically, Mockolate does not; set up the parent to return a mock for `Child` yourself (see [what needs manual review](./00-index.md#what-needs-manual-review)).

## Behavioral differences

The migrated code compiles, but behaves differently from Moq in these cases:

- **Class mocks call the base class.** A Moq mock calls the base class implementation only with `CallBase = true`, while a Mockolate class mock calls it for virtual members by default, even when a setup matches. To keep the Moq behavior, pass `Mockolate.MockBehavior.Default.SkippingBaseClass()` to `CreateMock` (see [customizing mock behavior](../04-create-mocks.md#customizing-mock-behavior)).
- **Setups no longer check the call order.** The code fix removes the `InSequence(…)` wrapper. Mockolate checks the order after the calls instead: add a [`Then(…)`](../07-verify-interactions.md#call-order) chain to the verification.
- **Failure messages are dropped.** `mock.Verify(m => m.Method(args), "message")` becomes `mock.Mock.Verify.Method(args).AtLeastOnce()` without the message.
