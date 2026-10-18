using Mockolate.Migration.NSubstitutePlayground.Domain;
using NSubstitute;

namespace Mockolate.Migration.NSubstitutePlayground;

/// <summary>Argument matchers: Arg.Any / Arg.Is / Arg.Compat. (Arg.Do/Arg.Invoke are in the Unsupported file.)</summary>
public class ArgumentMatcherTests
{
	[Test]
	public async Task ArgAny_MatchesAnyValue()
	{
		IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
		dispenser.Dispense(Arg.Any<string>(), Arg.Any<int>()).Returns(true);

		await That(dispenser.Dispense("Dark", 1)).IsTrue();
		await That(dispenser.Dispense("Milk", 99)).IsTrue();
	}

	[Test]
	public async Task ArgCompat_AnyAndIs_WorkLikePlainArg()
	{
		IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
		dispenser.Dispense(Arg.Compat.Any<string>(), Arg.Compat.Is<int>(i => i > 0)).Returns(true);

		await That(dispenser.Dispense("Dark", 5)).IsTrue();
		await That(dispenser.Dispense("Dark", 0)).IsFalse();
	}

	[Test]
	public async Task ArgIs_Predicate_MatchesEvenAmounts()
	{
		IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
		dispenser.Dispense("Dark", Arg.Is<int>(i => i % 2 == 0)).Returns(true);

		await That(dispenser.Dispense("Dark", 4)).IsTrue();
		await That(dispenser.Dispense("Dark", 3)).IsFalse();
	}

	[Test]
	public async Task ArgIs_Value_MatchesExactValue()
	{
		IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
		dispenser.Dispense(Arg.Is("Dark"), Arg.Is(2)).Returns(true);

		await That(dispenser.Dispense("Dark", 2)).IsTrue();
		await That(dispenser.Dispense("Dark", 3)).IsFalse();
	}

	[Test]
	public async Task PlainValue_IsUsedAsExactMatch()
	{
		IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
		dispenser.Dispense("Milk", 3).Returns(true);

		await That(dispenser.Dispense("Milk", 3)).IsTrue();
		await That(dispenser.Dispense("Milk", 4)).IsFalse();
	}
}
