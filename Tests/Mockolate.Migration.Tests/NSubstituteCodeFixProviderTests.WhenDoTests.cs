using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.NSubstituteAnalyzer,
	Mockolate.Migration.Analyzers.NSubstituteCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class NSubstituteCodeFixProviderTests
{
	public sealed class WhenDoTests
	{
		[Test]
		public async Task WhenMethod_Do_RewritesToSetupDo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				namespace MyCode;

				public interface IFoo { void Bar(string x); }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						int counter = 0;
						sub.When(x => x.Bar("hello")).Do(call => counter++);
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { void Bar(string x); }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						int counter = 0;
						sub.Mock.Setup.Bar("hello").Do(() => counter++);
					}
				}
				""");

		[Test]
		public async Task WhenMethod_DoNotCallBase_RewritesToSkippingBaseClass()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				namespace MyCode;

				public class Foo { public virtual void Bar(string x) { } }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<Foo>()|];
						sub.When(x => x.Bar("hello")).DoNotCallBase();
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				namespace MyCode;

				public class Foo { public virtual void Bar(string x) { } }

				public class Tests
				{
					public void Test()
					{
						var sub = Foo.CreateMock();
						sub.Mock.Setup.Bar("hello").SkippingBaseClass();
					}
				}
				""");

		[Test]
		public async Task WhenMethod_DoWithBareCallInfoReference_AddsTodoComment()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using System;
				using NSubstitute;

				namespace MyCode;

				public interface IFoo { void Bar(int x); }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						sub.When(x => x.Bar(1)).Do(call => Console.WriteLine(call));
					}
				}
				""",
				"""
				using System;
				using NSubstitute;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { void Bar(int x); }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						// TODO(MockolateM002): review CallInfo usage manually — Mockolate's Do/Returns take typed parameters, not CallInfo
						sub.Mock.Setup.Bar(1).Do(call => Console.WriteLine(call));
					}
				}
				""");

		[Test]
		public async Task WhenMethod_WithArgMatcher_TransformsMatcher()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				namespace MyCode;

				public interface IFoo { void Bar(string x); }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						int counter = 0;
						sub.When(x => x.Bar(Arg.Any<string>())).Do(_ => counter++);
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { void Bar(string x); }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						int counter = 0;
						sub.Mock.Setup.Bar(It.IsAny<string>()).Do(() => counter++);
					}
				}
				""");
	}
}
