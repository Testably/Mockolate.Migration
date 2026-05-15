using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.MoqAnalyzer,
	Mockolate.Migration.Analyzers.MoqCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class MoqCodeFixProviderTests
{
	public sealed class CallbackTests
	{
		[Fact]
		public async Task WithCallbackAfterThrows_MigratedToDo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				public interface IFoo { bool Bar(string x, int y); }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Setup(m => m.Bar(It.IsAny<string>(), It.IsAny<int>()))
							.Throws(new NotSupportedException("foo"))
							.Callback<string, int>((x, y) => { });
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;

				public interface IFoo { bool Bar(string x, int y); }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Setup.Bar(It.IsAny<string>(), It.IsAny<int>())
							.Throws(new NotSupportedException("foo"))
							.Do((x, y) => { });
					}
				}
				""");

		[Fact]
		public async Task WithCallbackMultipleTypeArgs_MigratedToDoWithoutTypeArgs()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				public interface IFoo { bool Bar(string x, int y); }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Setup(m => m.Bar(It.IsAny<string>(), It.IsAny<int>()))
							.Callback<string, int>((x, y) => { })
							.Returns(true);
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
						mock.Mock.Setup.Bar(It.IsAny<string>(), It.IsAny<int>())
							.Do((x, y) => { })
							.Returns(true);
					}
				}
				""");

		[Fact]
		public async Task WithCallbackNoTypeArgs_MigratedToDo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				public interface IFoo { void Bar(); }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Setup(m => m.Bar())
							.Callback(() => { });
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				public interface IFoo { void Bar(); }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Setup.Bar()
							.Do(() => { });
					}
				}
				""");

		[Fact]
		public async Task WithCallbackSingleTypeArg_MigratedToDoWithoutTypeArgs()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				public interface IFoo { bool Bar(string x); }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Setup(m => m.Bar(It.IsAny<string>()))
							.Callback<string>(x => { })
							.Returns(true);
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
						mock.Mock.Setup.Bar(It.IsAny<string>())
							.Do(x => { })
							.Returns(true);
					}
				}
				""");

		[Fact]
		public async Task WithNestedSetup_PreservesNestedMockTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				public interface IBar { bool Bar(string x); }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.Setup(m => m.Child.Bar(It.IsAny<string>()))
							.Returns(true)
							.Callback<string>(x => { });
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				public interface IBar { bool Bar(string x); }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): register the nested 'mock.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						mock.Child.Mock.Bar(It.IsAny<string>())
							.Returns(true)
							.Do(x => { });
					}
				}
				""");
	}
}
