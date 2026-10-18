using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.MoqAnalyzer,
	Mockolate.Migration.Analyzers.MoqCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class MoqCodeFixProviderTests
{
	public sealed class PropertyTests
	{
		[Test]
		public async Task SetupGet_MigratesToSetupPropertyAccess()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.SetupGet(m => m.Name).Returns("bar");
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Setup.Name.Returns("bar");
					}
				}
				""");

		[Test]
		public async Task VerifyGet_WithFailMessage_MigratesToGotAtLeastOnce()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyGet(m => m.Name, "Name was not read");
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Name.Got().AtLeastOnce();
					}
				}
				""");

		[Test]
		public async Task VerifyGet_WithoutTimes_MigratesToGotAtLeastOnce()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyGet(m => m.Name);
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Name.Got().AtLeastOnce();
					}
				}
				""");

		[Test]
		public async Task VerifyGet_WithTimesOnce_MigratesToGotOnce()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyGet(m => m.Name, Times.Once());
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Name.Got().Once();
					}
				}
				""");

		[Test]
		public async Task VerifyGet_WithUntranslatableTimes_PreservesOriginalCallWithTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test(Times timesVar)
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyGet(m => m.Name, timesVar);
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test(Times timesVar)
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): translate the Times argument 'timesVar' manually
						mock.{|CS1061:VerifyGet|}(m => m.Name, timesVar);
					}
				}
				""");

		[Test]
		public async Task VerifySet_WithLiteral_PassesValueDirectly()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifySet(m => m.Name = "foo");
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Name.Set("foo").AtLeastOnce();
					}
				}
				""");

		[Test]
		public async Task VerifySet_WithTimesNever_MigratesToSetNever()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifySet(m => m.Name = "foo", Times.Never);
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Name.Set("foo").Never();
					}
				}
				""");

		[Test]
		public async Task VerifySet_WithUntranslatableTimes_PreservesOriginalCallWithTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test(Times timesVar)
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifySet(m => m.Name = "foo", timesVar);
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test(Times timesVar)
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): translate the Times argument 'timesVar' manually
						mock.{|CS0411:VerifySet|}(m => m.Name = "foo", timesVar);
					}
				}
				""");

		[Test]
		public async Task VerifySet_WithItIsAnyMatcher_PreservesMatcher()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { int Value { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifySet(m => m.Value = It.IsAny<int>(), Times.Once());
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { int Value { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Value.Set({|CS0104:It|}.IsAny<int>()).Once();
					}
				}
				""");

		[Test]
		public async Task VerifySet_WithItIsInRange_TransformsMatcher()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { int Value { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifySet(m => m.Value = It.IsInRange(1, 5, Range.Inclusive));
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { int Value { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Value.Set({|CS0104:It|}.IsInRange(1, 5)).AtLeastOnce();
					}
				}
				""");

		[Test]
		public async Task VerifySet_WithItIsInRangeExclusive_PreservesChainedMatcher()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { int Value { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifySet(m => m.Value = It.IsInRange(1, 5, Range.Exclusive));
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { int Value { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Value.Set({|CS0104:It|}.IsInRange(1, 5).Exclusive()).AtLeastOnce();
					}
				}
				""");

		[Test]
		public async Task VerifySet_WithItIsRegex_PreservesChainedMatcher()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifySet(m => m.Name = It.IsRegex("^foo$"), Times.Once());
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { string Name { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.Name.Set({|CS0104:It|}.Matches("^foo$").AsRegex()).Once();
					}
				}
				""");

		[Test]
		public async Task VerifyGet_WithNestedProperty_UsesNavigationChain()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IBar { string Name { get; set; } }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyGet(m => m.Child.Name, Times.Exactly(2));
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IBar { string Name { get; set; } }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): register the nested 'mock.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						{|CS1929:mock.Child|}.Mock.Verify.Name.Got().Exactly(2);
					}
				}
				""");
	}
}
