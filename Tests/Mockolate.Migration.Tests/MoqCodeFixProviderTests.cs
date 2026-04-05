using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.MoqAnalyzer,
	Mockolate.Migration.Analyzers.MoqCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public class MoqCodeFixProviderTests
{
	[Fact]
	public async Task NewMockExplicit_IsReplaced()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
				}
			}
			""");

	[Fact]
	public async Task NewMockExplicit_WithExistingMockolateUsing_DoesNotDuplicateUsing()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
				}
			}
			""");

	[Fact]
	public async Task NewMockExplicit_WithGenericSetupCall_PreservesTypeArguments()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar<T>(T x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar<int>(It.IsAny<int>())).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { bool Bar<T>(T x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar<int>(It.IsAny<int>()).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task NewMockExplicit_WithNestedSetupCall_SetupIsNotRewritten()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IBar { bool Bar(string x); }
			public interface IChild { IBar GrandChild { get; } }
			public interface IFoo { IChild Child { get; } }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Child.GrandChild.Bar(It.IsAny<string>())).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IBar { bool Bar(string x); }
			public interface IChild { IBar GrandChild { get; } }
			public interface IFoo { IChild Child { get; } }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Child.GrandChild.Mock.Bar(It.IsAny<string>()).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task NewMockExplicit_WithObjectAccess_RemovesObjectProperty()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					var obj = mock.Object;
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					var obj = mock;
				}
			}
			""");

	[Fact]
	public async Task NewMockExplicit_WithSetupCall_MigratesSetup()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar(It.IsAny<string>())).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar(It.IsAny<string>()).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task NewMockExplicit_WithSetupCallMultipleArgs_MigratesSetup()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(string x, int y); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { bool Bar(string x, int y); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar(It.IsAny<string>(), It.IsAny<int>()).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task NewMockTargetTyped_IsReplaced()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { }

			public class Tests
			{
				public void Test()
				{
					Mock<IFoo> mock = [|new()|];
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { }

			public class Tests
			{
				public void Test()
				{
					IFoo mock = IFoo.CreateMock();
				}
			}
			""");

	[Fact]
	public async Task SetupWithItIsIn_MigratedToIsOneOf()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar(It.IsIn<string>(new[] { "A", "B" }))).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar(It.IsOneOf<string>(new[] { "A", "B" })).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task SetupWithItIsInRangeExclusive_ChainsExclusive()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(int x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar(It.IsInRange<int>(1, 10, Range.Exclusive))).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { bool Bar(int x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar(It.IsInRange<int>(1, 10).Exclusive()).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task SetupWithItIsInRangeInclusive_DropsRangeArg()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(int x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar(Moq.It.IsInRange<int>(1, 10, Range.Inclusive))).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { bool Bar(int x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar(It.IsInRange<int>(1, 10)).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task SetupWithItIsLambda_MigratedToSatisfies()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar(It.Is<string>(s => s.StartsWith("A")))).Returns(true);
					mock.Setup(m => m.Bar(It.Is<string>((x) => x.StartsWith("B"))))
						.Returns(false);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar(It.Satisfies<string>(s => s.StartsWith("A"))).Returns(true);
					mock.Mock.Setup.Bar(It.Satisfies<string>((x) => x.StartsWith("B")))
						.Returns(false);
				}
			}
			""");

	[Fact]
	public async Task SetupWithItIsRegex_MigratedToMatchesAsRegex()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar(It.IsRegex("^A"))).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar(It.Matches("^A").AsRegex()).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task SetupWithItIsTwoArgs_MigratedToIs()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;
			using System;
			using System.Collections.Generic;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Setup(m => m.Bar(It.Is<string>("hello", StringComparer.OrdinalIgnoreCase))).Returns(true);
				}
			}
			""",
			"""
			using Moq;
			using System;
			using System.Collections.Generic;
			using Mockolate;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Setup.Bar(It.Is<string>("hello").Using(StringComparer.OrdinalIgnoreCase)).Returns(true);
				}
			}
			""");

	[Fact]
	public async Task VerifyWithItTransforms_MigratesItCalls()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(string x, int y); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Verify(m => m.Bar(It.IsRegex("^A"), It.Is<int>(n => n > 0)), Times.Once());
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;
			using Mockolate.Verify;

			public interface IFoo { bool Bar(string x, int y); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Verify.Bar(It.Matches("^A").AsRegex(), It.Satisfies<int>(n => n > 0)).Once();
				}
			}
			""");

	[Fact]
	public async Task VerifyWithNoTimes_MigratesVerifyToAtLeastOnce()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Verify(m => m.Bar(It.IsAny<string>()));
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;
			using Mockolate.Verify;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Verify.Bar(It.IsAny<string>()).AtLeastOnce();
				}
			}
			""");

	[Fact]
	public async Task VerifyWithTimesBetweenExclusive_AdjustsBounds()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Verify(m => m.Bar(It.IsAny<string>()), Times.Between(3, 5, Range.Exclusive));
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;
			using Mockolate.Verify;

			public interface IFoo { bool Bar(string x); }

			public class Tests
			{
				public void Test()
				{
					var mock = IFoo.CreateMock();
					mock.Mock.Verify.Bar(It.IsAny<string>()).Between(4, 4);
				}
			}
			""");

	[Theory]
	[InlineData("Times.Never", ".Never()")]
	[InlineData("Times.Never()", ".Never()")]
	[InlineData("Times.Once", ".Once()")]
	[InlineData("Times.Once()", ".Once()")]
	[InlineData("Times.AtLeastOnce", ".AtLeastOnce()")]
	[InlineData("Times.AtLeastOnce()", ".AtLeastOnce()")]
	[InlineData("Times.AtLeast(3)", ".AtLeast(3)")]
	[InlineData("Times.AtMostOnce", ".AtMostOnce()")]
	[InlineData("Times.AtMostOnce()", ".AtMostOnce()")]
	[InlineData("Times.AtMost(4)", ".AtMost(4)")]
	[InlineData("Times.Exactly(5)", ".Exactly(5)")]
	[InlineData("Times.Between(3, 5, Range.Inclusive)", ".Between(3, 5)")]
	public async Task VerifyWithTimesProperty_MigratesVerify(string moqTimes, string mockolateTimes)
		=> await Verifier.VerifyCodeFixAsync(
			$$"""
			  using Moq;

			  public interface IFoo { bool Bar(string x); }

			  public class Tests
			  {
			  	public void Test()
			  	{
			  		var mock = [|new Mock<IFoo>()|];
			  		mock.Verify(m => m.Bar(It.IsAny<string>()), {{moqTimes}});
			  	}
			  }
			  """,
			$$"""
			  using Moq;
			  using Mockolate;
			  using Mockolate.Verify;

			  public interface IFoo { bool Bar(string x); }

			  public class Tests
			  {
			  	public void Test()
			  	{
			  		var mock = IFoo.CreateMock();
			  		mock.Mock.Verify.Bar(It.IsAny<string>()){{mockolateTimes}};
			  	}
			  }
			  """);

	[Fact]
	public async Task VerifyWithUnrecognizedTimesArg_PreservesOriginalVerifyCall()
		=> await Verifier.VerifyCodeFixAsync(
			"""
			using Moq;

			public interface IFoo { void Bar(); }

			public class Tests
			{
				public void Test(Times timesVar)
				{
					var mock = [|new Mock<IFoo>()|];
					mock.Verify(m => m.Bar(), timesVar);
				}
			}
			""",
			"""
			using Moq;
			using Mockolate;

			public interface IFoo { void Bar(); }

			public class Tests
			{
				public void Test(Times timesVar)
				{
					var mock = IFoo.CreateMock();
					mock.Verify(m => m.Bar(), timesVar);
				}
			}
			""");
}
