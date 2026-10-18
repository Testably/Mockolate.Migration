using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.MoqAnalyzer,
	Mockolate.Migration.Analyzers.MoqCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class MoqCodeFixProviderTests
{
	public sealed class VerifyTests
	{
		[Test]
		public async Task Method_WithItTransforms_MigratesItCalls()
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

		[Test]
		public async Task NestedMethod_MigratesViaNavigationChain()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				public interface IBar { int Compute(int x); }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Verify(m => m.Child.Compute(It.IsAny<int>()), Times.Once());
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				public interface IBar { int Compute(int x); }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): register the nested 'mock.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						mock.Child.Mock.Verify.Compute(It.IsAny<int>()).Once();
					}
				}
				""");

		[Test]
		public async Task WithFailMessage_MigratesVerifyToAtLeastOnce()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				public interface IFoo { bool Bar(string x); }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Verify(m => m.Bar(It.IsAny<string>()), "Bar was not called");
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

		[Test]
		public async Task WithNoTimes_MigratesVerifyToAtLeastOnce()
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

		[Test]
		public async Task WithTimesBetweenExclusive_AdjustsBounds()
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

		[Test]
		public async Task WithTimesBetweenExclusive_NonLiteralBounds_AdjustsBoundsUsingAddSubtract()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				public interface IFoo { bool Bar(string x); }

				public class Tests
				{
					public void Test(int low, int high)
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Verify(m => m.Bar(It.IsAny<string>()), Times.Between(low, high, Range.Exclusive));
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
					public void Test(int low, int high)
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Bar(It.IsAny<string>()).Between(low + 1, high - 1);
					}
				}
				""");

		[Test]
		[Arguments("Times.Never", ".Never()")]
		[Arguments("Times.Never()", ".Never()")]
		[Arguments("Times.Once", ".Once()")]
		[Arguments("Times.Once()", ".Once()")]
		[Arguments("Times.AtLeastOnce", ".AtLeastOnce()")]
		[Arguments("Times.AtLeastOnce()", ".AtLeastOnce()")]
		[Arguments("Times.AtLeast(3)", ".AtLeast(3)")]
		[Arguments("Times.AtMostOnce", ".AtMostOnce()")]
		[Arguments("Times.AtMostOnce()", ".AtMostOnce()")]
		[Arguments("Times.AtMost(4)", ".AtMost(4)")]
		[Arguments("Times.Exactly(5)", ".Exactly(5)")]
		[Arguments("Times.Between(3, 5, Range.Inclusive)", ".Between(3, 5)")]
		public async Task WithTimesProperty_MigratesVerify(string moqTimes, string mockolateTimes)
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

		[Test]
		public async Task WithUnrecognizedMultiLineTimesArg_KeepsTodoOnOneLine()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				public interface IFoo { void Bar(); }

				public class Tests
				{
					public void Test(bool flag)
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Verify(m => m.Bar(), flag
							? Times.Once()
							: Times.Never());
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				public interface IFoo { void Bar(); }

				public class Tests
				{
					public void Test(bool flag)
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): translate the Times argument 'flag ? Times.Once() : Times.Never()' manually
						mock.Verify(m => m.Bar(), flag
							? Times.Once()
							: Times.Never());
					}
				}
				""");

		[Test]
		public async Task WithUnrecognizedTimesArg_PreservesOriginalVerifyCall()
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
						// TODO(MockolateM001): translate the Times argument 'timesVar' manually
						mock.Verify(m => m.Bar(), timesVar);
					}
				}
				""");
	}
}
