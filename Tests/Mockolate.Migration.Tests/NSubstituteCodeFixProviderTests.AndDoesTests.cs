using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.NSubstituteAnalyzer,
	Mockolate.Migration.Analyzers.NSubstituteCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class NSubstituteCodeFixProviderTests
{
	public sealed class AndDoesTests
	{
		[Test]
		public async Task AndDoes_AfterMultiArgReturns_RewritesToDoOnFinalCall()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				namespace MyCode;

				public interface IFoo { int Bar(int x); }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						int counter = 0;
						sub.Bar(1).Returns(1, 2, 3).AndDoes(call => counter++);
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { int Bar(int x); }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						int counter = 0;
						sub.Mock.Setup.Bar(1).Returns(1).Returns(2).Returns(3).For(int.MaxValue).Do(() => counter++);
					}
				}
				""");

		[Test]
		public async Task AndDoes_AfterReturns_RewritesToDo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				namespace MyCode;

				public interface IFoo { int Bar(int x); }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						int counter = 0;
						sub.Bar(1).Returns(42).AndDoes(call => counter++);
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { int Bar(int x); }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						int counter = 0;
						sub.Mock.Setup.Bar(1).Returns(42).Do(() => counter++);
					}
				}
				""");
	}
}
