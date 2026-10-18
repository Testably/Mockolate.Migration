using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.NSubstituteAnalyzer,
	Mockolate.Migration.Analyzers.NSubstituteCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class NSubstituteCodeFixProviderTests
{
	public sealed class ClearReceivedCallsTests
	{
		[Test]
		public async Task ClearReceivedCalls_IsRewrittenToClearAllInteractions()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				namespace MyCode;

				public interface IFoo { void Bar(int x); }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						sub.Bar(1);
						sub.ClearReceivedCalls();
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { void Bar(int x); }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						sub.Bar(1);
						sub.Mock.ClearAllInteractions();
					}
				}
				""");

		[Test]
		public async Task UserDefinedClearReceivedCalls_IsNotRewritten()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				namespace MyCode;

				public interface IFoo { void Bar(int x); void ClearReceivedCalls(); }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						sub.Bar(1);
						sub.ClearReceivedCalls();
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { void Bar(int x); void ClearReceivedCalls(); }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						sub.Bar(1);
						sub.ClearReceivedCalls();
					}
				}
				""");
	}
}
