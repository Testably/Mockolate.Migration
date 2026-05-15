using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.NSubstituteAnalyzer,
	Mockolate.Migration.Analyzers.NSubstituteCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class NSubstituteCodeFixProviderTests
{
	public sealed class NestedTests
	{
		[Fact]
		public async Task NestedMethod_RewritesAndAddsTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				public interface IBar { int Compute(int x); }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						sub.Child.Compute(1).Returns(42);
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				public interface IBar { int Compute(int x); }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						// TODO: register the nested 'sub.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						sub.Child.Mock.Setup.Compute(1).Returns(42);
					}
				}
				""");

		[Fact]
		public async Task NestedProperty_RewritesAndAddsTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				public interface IBar { string Name { get; } }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						sub.Child.Name.Returns("baz");
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;

				public interface IBar { string Name { get; } }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						// TODO: register the nested 'sub.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						sub.Child.Mock.Setup.Name.Returns("baz");
					}
				}
				""");

		[Fact]
		public async Task NestedReceivedProperty_Got_RewritesAndAddsTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				public interface IBar { string Name { get; } }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						_ = sub.Child.Received(2).Name;
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;
				using Mockolate.Verify;

				public interface IBar { string Name { get; } }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						// TODO: register the nested 'sub.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						sub.Child.Mock.Verify.Name.Got().Exactly(2);
					}
				}
				""");

		[Fact]
		public async Task NestedReceivedProperty_Set_RewritesAndAddsTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				public interface IBar { string Name { get; set; } }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						sub.Child.Received(2).Name = "baz";
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;
				using Mockolate.Verify;

				public interface IBar { string Name { get; set; } }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						// TODO: register the nested 'sub.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						sub.Child.Mock.Verify.Name.Set("baz").Exactly(2);
					}
				}
				""");

		[Fact]
		public async Task NestedReceivedMethod_RewritesToVerifyOnNestedMock()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using NSubstitute;

				public interface IBar { int Compute(int x); }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						sub.Child.Received(2).Compute(1);
					}
				}
				""",
				"""
				using NSubstitute;
				using Mockolate;
				using Mockolate.Verify;

				public interface IBar { int Compute(int x); }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						// TODO: register the nested 'sub.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						sub.Child.Mock.Verify.Compute(1).Exactly(2);
					}
				}
				""");

		[Fact]
		public async Task NestedRaiseEvent_RewritesAndAddsTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using System;
				using NSubstitute;

				public interface IBar { event EventHandler MyEvent; }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = [|Substitute.For<IFoo>()|];
						sub.Child.MyEvent += Raise.EventWith(EventArgs.Empty);
					}
				}
				""",
				"""
				using System;
				using NSubstitute;
				using Mockolate;

				public interface IBar { event EventHandler MyEvent; }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var sub = IFoo.CreateMock();
						// TODO: register the nested 'sub.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						sub.Child.Mock.Raise.MyEvent(null, EventArgs.Empty);
					}
				}
				""");
	}
}
