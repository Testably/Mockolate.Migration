---
description: How the Mockolate.Migration code fix rewrites Moq mocks, setups and verifications to Mockolate.
---

import ApplyFix from '@site/src/components/ApplyFix';

# Migrating from Moq

The `MockolateM001` code fix rewrites `new Mock<T>(…)` to `T.CreateMock(…)`, changes the declared type from `Mock<T>` to `T`, drops every `.Object` access and moves setups and verifications to `Mock.Setup` and `Mock.Verify`.

The examples use the `IChocolateDispenser` interface from the [migration overview](/Mockolate/migration/) and the `VendingMachine` from [Getting started](../../01-getting-started.mdx). The code fix keeps your variable names, so the mock keeps its name and `sut` stays the system under test.

A typical test sets up the mock, passes it to the system under test and verifies the calls afterwards:

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

## Creating mocks

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

Strict mocks get a [behavior](../../04-create-mocks.md#customizing-mock-behavior) that throws when no setup matches, written as `Mockolate.MockBehavior` because Moq has a `MockBehavior` as well:

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

## Setups

`Callback` becomes `Do`, and a `SetupSequence` becomes a setup with the same chain of `Returns`, which Mockolate uses as a [sequence](../../05-setup/01-methods.md#sequences):

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
    .Returns(false);
// highlight-next-line
dispenser.Mock.Setup.TotalDispensed.InitializeWith(10);
```

</ApplyFix>

## Verifications

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

## Argument matchers

`It.IsAny` stays as it is, while `It.Is` with a predicate becomes `It.Satisfies` and `It.IsRegex` becomes `It.Matches(…).AsRegex()`:

<ApplyFix id="MockolateM001" migrateTo="Mockolate">

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
dispenser.Setup(m => m.Dispense(It.Is<string>(type => type.StartsWith("D")), It.IsAny<int>())).Returns(true);
dispenser.Setup(m => m.Dispense(It.IsRegex("^M"), It.IsAny<int>())).Returns(false);
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense(It.Satisfies<string>(type => type.StartsWith("D")), It.IsAny<int>()).Returns(true);
dispenser.Mock.Setup.Dispense(It.Matches("^M").AsRegex(), It.IsAny<int>()).Returns(false);
```

</ApplyFix>

## Events

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

## Supported Moq constructs

| Moq                                                       | Mockolate                                                              |
|-----------------------------------------------------------|------------------------------------------------------------------------|
| `new Mock<T>()`                                           | `T.CreateMock()`                                                       |
| `new Mock<T>(MockBehavior.Strict)`                        | `T.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup())`            |
| `mock.Object`                                             | `mock` (the `.Object` access is dropped)                               |
| `mock.Setup(m => m.Method(args))`                         | `mock.Mock.Setup.Method(args)`                                         |
| `mock.Setup(m => m.Prop)`, `mock.SetupGet(m => m.Prop)`   | `mock.Mock.Setup.Prop`                                                 |
| `mock.SetupProperty(m => m.Prop)`                         | `mock.Mock.Setup.Prop.Register()`                                      |
| `mock.SetupProperty(m => m.Prop, value)`                  | `mock.Mock.Setup.Prop.InitializeWith(value)`                           |
| `mock.SetupSequence(…).Returns(a).Returns(b)`             | `mock.Mock.Setup.Method(…).Returns(a).Returns(b)`                      |
| `.Returns(…)`, `.ReturnsAsync(…)`, `.Throws(…)`           | kept on the Mockolate setup                                            |
| `.Callback(…)`                                            | `.Do(…)`                                                               |
| `mock.InSequence(sequence).Setup(…)`                      | `mock.Mock.Setup.Method(…)` (the `InSequence` wrapper is removed)      |
| `mock.Verify(m => m.Method(args), Times.Once())`          | `mock.Mock.Verify.Method(args).Once()`                                 |
| `mock.VerifyGet(…)`, `mock.VerifySet(…)`                  | `mock.Mock.Verify.Prop.Got(…)`, `mock.Mock.Verify.Prop.Set(…)`         |
| `mock.Raise(m => m.Event += null, args)`                  | `mock.Mock.Raise.Event(sender, args)`                                  |
| `mock.VerifyAdd(m => m.Event += …, times)`                | `mock.Mock.Verify.Event.Subscribed()` with the count                   |
| `mock.VerifyRemove(m => m.Event -= …, times)`             | `mock.Mock.Verify.Event.Unsubscribed()` with the count                 |
| A non-literal `Times` argument (e.g. a variable)          | the Moq call is kept, with a `// TODO` comment                         |
| `It.IsAny<T>()`                                           | `It.IsAny<T>()`                                                        |
| `It.Is<T>(predicate)`                                     | `It.Satisfies<T>(predicate)`                                           |
| `It.IsInRange(a, b, Range.Inclusive)`                     | `It.IsInRange(a, b)`                                                   |
| `It.IsRegex(pattern, options)`                            | `It.Matches(pattern).AsRegex(options)`                                 |
| `It.Ref<T>.IsAny`, `out` parameters                       | `It.IsAnyRef<T>()`, `It.IsOut(() => value)`                            |
| `mock.Setup(m => m.Child.Prop)`                           | `mock.Child.Mock.Setup.Prop` and a `// TODO` comment                   |

See [parameter matching](../../06-parameter-matching.md) for the Mockolate matchers, and [what needs manual review](/Mockolate/migration/automated/#what-needs-manual-review) for the cases the code fix cannot finish on its own.
