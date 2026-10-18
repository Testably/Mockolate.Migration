using Mockolate.Migration.MoqPlayground.Domain;
using Moq;

namespace Mockolate.Migration.MoqPlayground;

using MockBehavior = Moq.MockBehavior;

/// <summary>Mock construction patterns.</summary>
public class CreationTests
{
	[Test]
	public async Task ClassMockWithConstructorArgs_IsCreatedWithThoseArgs()
	{
		// ChocolateRecipe has a parameterless ctor, but Moq supports passing args to base.
		Mock<ChocolateRecipe> recipe = new();
		recipe.SetupGet(r => r.Name).Returns("Praline");

		await That(recipe.Object.Name).IsEqualTo("Praline");
	}

	[Test]
	public async Task DefaultLooseMock_ReturnsDefaultsForUnsetMembers()
	{
		Mock<IChocolateDispenser> dispenser = new();

		// Loose Moq returns default(bool) = false when not set up.
		bool dispensed = dispenser.Object.Dispense("Dark", 1);

		await That(dispensed).IsFalse();
	}

	[Test]
	public async Task ExplicitLooseMock_IsEquivalentToDefault()
	{
		Mock<IChocolateDispenser> dispenser = new(MockBehavior.Loose);

		await That(dispenser.Object.Dispense("Dark", 1)).IsFalse();
	}

	[Test]
	public async Task ObjectAccess_IsUsedToReachTheMockedInstance()
	{
		Mock<IChocolateFactory> factory = new();
		factory.Setup(f => f.RegisterRecipe("Truffle")).Returns(true);

		bool registered = factory.Object.RegisterRecipe("Truffle");

		await That(registered).IsTrue();
	}

	[Test]
	public async Task StrictMock_ThrowsForUnsetMembers()
	{
		Mock<IChocolateDispenser> dispenser = new(MockBehavior.Strict);

		await That(() => dispenser.Object.Dispense("Dark", 1))
			.Throws<MockException>();
	}
}
