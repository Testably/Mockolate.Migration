---
description: How the Mockolate.Migration code fix rewrites Moq mocks, setups and verifications to Mockolate.
---

# Migrating from Moq

The `MockolateM001` code fix rewrites `new Mock<T>(…)` to `T.CreateMock(…)`, changes the declared type from `Mock<T>` to `T`, drops every `.Object` access and moves setups and verifications to `Mock.Setup` and `Mock.Verify`.

The examples use the `IChocolateDispenser` interface from the [migration overview](/Mockolate/migration/) and the `VendingMachine` from [Getting started](../../01-getting-started.mdx). The code fix keeps your variable names, so the mock keeps its name and `sut` stays the system under test.

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
dispenser.Setup(m => m.Dispense("Dark", It.IsAny<int>())).Returns(true);
VendingMachine sut = new VendingMachine(dispenser.Object);

sut.Buy("Dark", 4);

dispenser.Verify(m => m.Dispense("Dark", It.IsAny<int>()), Times.Once());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);
VendingMachine sut = new VendingMachine(dispenser);

sut.Buy("Dark", 4);

dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Once();
```

Strict mocks get a [behavior](../../04-create-mocks.md#customizing-mock-behavior) that throws when no setup matches. Properties, raising events and verifying subscriptions are migrated as well:

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>(MockBehavior.Strict);
dispenser.SetupProperty(m => m.TotalDispensed, 10);
dispenser.Raise(m => m.ChocolateDispensed += null, "Dark", 2);
dispenser.VerifyAdd(m => m.ChocolateDispensed += It.IsAny<ChocolateDispensedDelegate>(), Times.Once());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
dispenser.Mock.Setup.TotalDispensed.InitializeWith(10);
dispenser.Mock.Raise.ChocolateDispensed("Dark", 2);
dispenser.Mock.Verify.ChocolateDispensed.Subscribed().Once();
```

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
