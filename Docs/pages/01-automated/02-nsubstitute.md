---
description: How the Mockolate.Migration code fix rewrites NSubstitute substitutes, setups, verifications and CallInfo callbacks to Mockolate.
---

import ApplyFix from '@site/src/components/ApplyFix';

# Migrating from NSubstitute

The `MockolateM002` code fix rewrites `Substitute.For<T>(…)` to `T.CreateMock(…)` and moves the setups (`substitute.Method(args).Returns(…)`) and verifications (`substitute.Received().Method(…)`) to `Mock.Setup` and `Mock.Verify`:

<ApplyFix id="MockolateM002" migrateTo="Mockolate">

```csharp title="Before (NSubstitute)"
IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
dispenser.Dispense("Dark", Arg.Any<int>()).Returns(true);
dispenser.Dispense("Milk", Arg.Any<int>()).Returns(false);
VendingMachine sut = new VendingMachine(dispenser);

sut.Buy("Dark", 4);
sut.Buy("Milk", 2);

dispenser.Received(1).Dispense("Dark", 4);
dispenser.Received().Dispense("Milk", Arg.Any<int>());
dispenser.DidNotReceive().Dispense("White", Arg.Any<int>());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);
dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>()).Returns(false);
VendingMachine sut = new VendingMachine(dispenser);

sut.Buy("Dark", 4);
sut.Buy("Milk", 2);

dispenser.Mock.Verify.Dispense("Dark", 4).Once();
dispenser.Mock.Verify.Dispense("Milk", It.IsAny<int>()).AtLeastOnce();
dispenser.Mock.Verify.Dispense("White", It.IsAny<int>()).Never();
```

</ApplyFix>

## Supported NSubstitute constructs

The tables list the constructs the code fix rewrites. Everything else [needs manual review](./00-index.md#what-needs-manual-review). The examples use the `IChocolateDispenser` interface from the [migration guide](/Mockolate/migration/) and the `VendingMachine` from [Getting started](../../01-getting-started.mdx); the code fix keeps your variable names, so the mock keeps its name and `sut` stays the system under test.

### Creating substitutes

| NSubstitute                                                 | Mockolate                                                                    |
|-------------------------------------------------------------|------------------------------------------------------------------------------|
| `Substitute.For<T>()`, `Substitute.For<T>(args)`            | `T.CreateMock()`, `T.CreateMock(args)`                                       |
| `Substitute.For<T, IOther>()`                               | `T.CreateMock().Implementing<IOther>()`                                      |
| `Substitute.ForPartsOf<T>()`                                | `T.CreateMock()` (class mocks call the base class by default)                |
| `Substitute.ForTypeForwardingTo<TInterface, TClass>(args)`  | `TInterface.CreateMock().Wrapping(new TClass(args))`                         |

### Setups

| NSubstitute                                                 | Mockolate                                                                    |
|-------------------------------------------------------------|------------------------------------------------------------------------------|
| `sub.Method(args).Returns(value)`                           | `sub.Mock.Setup.Method(args).Returns(value)`                                 |
| `sub.Method(args).Returns(a, b, c)`                         | `sub.Mock.Setup.Method(args).Returns(a).Returns(b).Returns(c).Forever()`     |
| `.Throws<TException>()`, `.Throws(exception)`               | `.Throws<TException>()`, `.Throws(exception)`                                |
| `.ReturnsForAnyArgs(value)`, `.ThrowsForAnyArgs<E>()`       | `.AnyParameters().Returns(value)`, `.AnyParameters().Throws<E>()`            |
| `.Returns(value).AndDoes(callback)`                         | `.Returns(value).Do(callback)`                                               |
| `sub.Property.Returns(value)`                               | `sub.Mock.Setup.Property.Returns(value)`                                     |
| `sub.When(x => x.Method(args)).Do(callback)`                | `sub.Mock.Setup.Method(args).Do(callback)`                                   |
| `sub.When(x => x.Method(args)).DoNotCallBase()`             | `sub.Mock.Setup.Method(args).SkippingBaseClass()`                            |

Multiple return values become a [sequence](../../05-setup/01-methods.md#sequences) that ends with `.Forever()`, so the last value repeats as in NSubstitute, and `ReturnsForAnyArgs` ignores the arguments with `AnyParameters()`:

<ApplyFix id="MockolateM002" migrateTo="Mockolate">

```csharp title="Before (NSubstitute)"
IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
dispenser.Dispense("Dark", 1).Returns(true, true, false);
dispenser.Dispense("Milk", 1).ReturnsForAnyArgs(true);
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense("Dark", 1).Returns(true).Returns(true).Returns(false).Forever();
dispenser.Mock.Setup.Dispense("Milk", 1).AnyParameters().Returns(true);
```

</ApplyFix>

`When(…).Do(…)` becomes a setup with a `Do` callback:

<ApplyFix id="MockolateM002" migrateTo="Mockolate">

```csharp title="Before (NSubstitute)"
// highlight-next-line
IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
int refills = 0;
// highlight-next-line
dispenser.When(x => x.Dispense("Milk", Arg.Any<int>())).Do(_ => refills++);
```

```csharp title="After (Mockolate)"
// highlight-next-line
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
int refills = 0;
// highlight-next-line
dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>()).Do(() => refills++);
```

</ApplyFix>

### Verifications

| NSubstitute                                                 | Mockolate                                                                    |
|-------------------------------------------------------------|------------------------------------------------------------------------------|
| `sub.Received().Method(args)`                               | `sub.Mock.Verify.Method(args).AtLeastOnce()`                                 |
| `sub.Received(n).Method(args)`                              | `sub.Mock.Verify.Method(args).Exactly(n)` (`.Once()` for 1)                  |
| `sub.DidNotReceive().Method(args)`                          | `sub.Mock.Verify.Method(args).Never()`                                       |
| `sub.ReceivedWithAnyArgs().Method(…)`                       | `sub.Mock.Verify.Method(…).AnyParameters().AtLeastOnce()`                    |
| `sub.DidNotReceiveWithAnyArgs().Method(…)`                  | `sub.Mock.Verify.Method(…).AnyParameters().Never()`                          |
| `_ = sub.Received().Prop`, `sub.Received().Prop = value`    | `sub.Mock.Verify.Prop.Got().AtLeastOnce()`, `….Set(value).AtLeastOnce()`     |
| `sub.ClearReceivedCalls()`                                  | `sub.Mock.ClearAllInteractions()`                                            |

`Received` and `DidNotReceive` move to `Mock.Verify`, with the expected number of calls at the end:

<ApplyFix id="MockolateM002" migrateTo="Mockolate">

```csharp title="Before (NSubstitute)"
IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
dispenser.Received().Dispense("Dark", 4);
dispenser.Received(2).Dispense("Milk", Arg.Any<int>());
dispenser.DidNotReceive().Dispense("White", Arg.Any<int>());
dispenser.ReceivedWithAnyArgs().Dispense("Dark", 1);
_ = dispenser.Received().TotalDispensed;
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Verify.Dispense("Dark", 4).AtLeastOnce();
dispenser.Mock.Verify.Dispense("Milk", It.IsAny<int>()).Exactly(2);
dispenser.Mock.Verify.Dispense("White", It.IsAny<int>()).Never();
dispenser.Mock.Verify.Dispense("Dark", 1).AnyParameters().AtLeastOnce();
dispenser.Mock.Verify.TotalDispensed.Got().AtLeastOnce();
```

</ApplyFix>

### Argument matchers

| NSubstitute                                                 | Mockolate                                                                    |
|-------------------------------------------------------------|------------------------------------------------------------------------------|
| `Arg.Any<T>()`                                              | `It.IsAny<T>()`                                                              |
| `Arg.Is<T>(predicate)`                                      | `It.Satisfies<T>(predicate)`                                                 |
| `Arg.Is(value)`, `Arg.Is<T>(value)`                         | `It.Is(value)`, `It.Is<T>(value)`                                            |
| `Arg.Compat.Any<T>()` and the other `Arg.Compat` matchers   | the same as the matching `Arg` matcher                                       |

See [parameter matching](../../06-parameter-matching.md) for the Mockolate matchers.

### CallInfo callbacks

In NSubstitute, the callbacks of `Returns(call => …)`, `When(…).Do(call => …)` and `AndDoes(call => …)` receive a `CallInfo` with the arguments of the call. The Mockolate callbacks take the parameters of the method instead, so the code fix rewrites the lambda:

- If the lambda does not use `call`, the parameter is dropped: `Do(call => count++)` becomes `Do(() => count++)`.
- If the lambda only reads arguments at a constant position (`call.ArgAt<T>(0)`, `call[0]`) or by a unique type (`call.Arg<T>()`), it gets the parameters of the method, and each access is replaced with the parameter: `Returns(call => call.ArgAt<int>(1) > 0)` becomes `Returns((string type, int amount) => amount > 0)`.
- Otherwise the lambda is kept as it is and marked with a `// TODO` comment. See [what needs manual review](./00-index.md#what-needs-manual-review).

Both rewrites in one example:

<ApplyFix id="MockolateM002" migrateTo="Mockolate">

```csharp title="Before (NSubstitute)"
// highlight-next-line
IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
int count = 0;
// highlight-start
dispenser.Dispense("Dark", Arg.Any<int>()).Returns(call => call.ArgAt<int>(1) > 0);
dispenser.When(x => x.Dispense("Milk", Arg.Any<int>())).Do(call => count++);
// highlight-end
```

```csharp title="After (Mockolate)"
// highlight-next-line
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
int count = 0;
// highlight-start
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns((string type, int amount) => amount > 0);
dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>()).Do(() => count++);
// highlight-end
```

</ApplyFix>

### Events

| NSubstitute                                                 | Mockolate                                                                    |
|-------------------------------------------------------------|------------------------------------------------------------------------------|
| `sub.Event += Raise.Event()`                                | `sub.Mock.Raise.Event(null, EventArgs.Empty)`                                |
| `sub.Event += Raise.EventWith(sender, args)`                | `sub.Mock.Raise.Event(sender, args)` (`null` without a sender)               |
| `sub.Event += Raise.Event<TDelegate>(args)`                 | `sub.Mock.Raise.Event(args)`                                                 |

Raised events go through `Mock.Raise`, with the arguments of the delegate:

<ApplyFix id="MockolateM002" migrateTo="Mockolate">

```csharp title="Before (NSubstitute)"
IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
dispenser.ChocolateDispensed += Raise.Event<ChocolateDispensedDelegate>("Dark", 2);
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Raise.ChocolateDispensed("Dark", 2);
```

</ApplyFix>

### Nested substitutes

| NSubstitute                                                 | Mockolate                                                                    |
|-------------------------------------------------------------|------------------------------------------------------------------------------|
| `sub.Child.Method(args).Returns(value)`                     | `sub.Child.Mock.Setup.Method(args).Returns(value)` and a `// TODO` comment   |

NSubstitute creates the substitute for `Child` automatically, Mockolate does not; set up the parent to return a mock for `Child` yourself (see [what needs manual review](./00-index.md#what-needs-manual-review)).

## Behavioral differences

The migrated code compiles, but behaves differently from NSubstitute in this case:

- **Class substitutes call the base class.** The code fix creates both `Substitute.For<T>()` and `Substitute.ForPartsOf<T>()` as `T.CreateMock()`, and a Mockolate class mock calls the base class implementation of virtual members by default, even when a setup matches. To keep the behavior of `Substitute.For<T>()` for a class, pass `MockBehavior.Default.SkippingBaseClass()` to `CreateMock` (see [customizing mock behavior](../../04-create-mocks.md#customizing-mock-behavior)).
