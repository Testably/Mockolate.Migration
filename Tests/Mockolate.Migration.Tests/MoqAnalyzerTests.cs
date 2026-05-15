using Mockolate.Migration.Analyzers;
using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Migration.Analyzers.MoqAnalyzer>;

namespace Mockolate.Migration.Tests;

public class MoqAnalyzerTests
{
	[Fact]
	public async Task MoqFactoryMethodEndingInMock_AsArgument_IsNotFlagged()
		=> await Verifier.VerifyAnalyzerAsync("""
		                                      using Moq;

		                                      public interface IFoo { }

		                                      namespace Moq.TestHelpers
		                                      {
		                                          public static class Factory
		                                          {
		                                              public static T MyMock<T>() where T : class => null!;
		                                          }
		                                      }

		                                      public class Tests
		                                      {
		                                      	public void Consume(IFoo foo) { }
		                                      	public void Test()
		                                      	{
		                                      		Consume(Moq.TestHelpers.Factory.MyMock<IFoo>());
		                                      	}
		                                      }
		                                      """);

	[Fact]
	public async Task MoqFactoryMethodEndingInMock_ChainedCall_IsFlagged()
		=> await Verifier.VerifyAnalyzerAsync("""
		                                      using Moq;

		                                      public interface IFoo { int Bar(); }

		                                      namespace Moq.TestHelpers
		                                      {
		                                          public static class Factory
		                                          {
		                                              public static T MyMock<T>() where T : class => null!;
		                                          }
		                                      }

		                                      public class Tests
		                                      {
		                                      	public void Test()
		                                      	{
		                                      		var value = {|#0:Moq.TestHelpers.Factory.MyMock<IFoo>().Bar()|};
		                                      	}
		                                      }
		                                      """,
			Verifier.Diagnostic(Rules.MoqRule)
				.WithLocation(0));

	[Fact]
	public async Task NewMockExplicit_IsFlagged()
		=> await Verifier.VerifyAnalyzerAsync("""
		                                      using Moq;

		                                      public interface IFoo { }

		                                      public class Tests
		                                      {
		                                      	public void Test()
		                                      	{
		                                      		var mock = {|#0:new Mock<IFoo>()|};
		                                      	}
		                                      }
		                                      """,
			Verifier.Diagnostic(Rules.MoqRule)
				.WithLocation(0));

	[Fact]
	public async Task NewMockTargetTyped_IsFlagged()
		=> await Verifier.VerifyAnalyzerAsync("""
		                                      using Moq;

		                                      public interface IFoo { }

		                                      public class Tests
		                                      {
		                                      	public void Test()
		                                      	{
		                                      		Mock<IFoo> mock = {|#0:new()|};
		                                      	}
		                                      }
		                                      """,
			Verifier.Diagnostic(Rules.MoqRule)
				.WithLocation(0));

	[Fact]
	public async Task NonMoqInvocation_IsNotFlagged()
		=> await Verifier.VerifyAnalyzerAsync("""
		                                      public interface IFoo { int Bar(); }

		                                      public class Tests
		                                      {
		                                      	public IFoo CreateMock() => null!;
		                                      	public void Test()
		                                      	{
		                                      		var value = CreateMock().Bar();
		                                      	}
		                                      }
		                                      """);
}
