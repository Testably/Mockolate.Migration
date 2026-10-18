using System.Threading;
using Microsoft.CodeAnalysis.Testing;
using Verifier =Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.MoqAnalyzer,
	Mockolate.Migration.Analyzers.MoqCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class MoqCodeFixProviderTests
{
	public sealed class NewMockTests
	{
		[Test]
		public async Task AsPropertyInitializer_IsReplaced()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public Mock<IFoo> MockProperty { get; } = [|new Mock<IFoo>()|];
				}
				""",
				"""
				using Moq;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public IFoo MockProperty { get; } = IFoo.CreateMock();
				}
				""");

		[Test]
		public async Task IsReplaced()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

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

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
					}
				}
				""");

		[Test]
		public async Task TargetTyped_IsReplaced(CancellationToken cancellationToken)
			=> await new Verifier.Test
			{
				CodeActionValidationMode = CodeActionValidationMode.None,
				TestCode = """
				using Moq;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						Mock<IFoo> mock = [|new()|];
					}
				}
				""",
				FixedCode = """
				using Moq;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						MyCode.IFoo mock = MyCode.IFoo.CreateMock();
					}
				}
				""",
			}.RunAsync(cancellationToken);

		[Test]
		public async Task WithExistingMockolateUsing_DoesNotDuplicateUsing()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using Mockolate;

				namespace MyCode;

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

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
					}
				}
				""");

		[Test]
		public async Task WithGenericSetupCall_PreservesTypeArguments()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

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

				namespace MyCode;

				public interface IFoo { bool Bar<T>(T x); }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Setup.Bar<int>({|CS0104:It|}.IsAny<int>()).Returns(true);
					}
				}
				""");

		[Test]
		public async Task WithLooseMockBehavior_IsReplacedWithoutArgument()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>(MockBehavior.Loose)|];
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
					}
				}
				""");

		[Test]
		public async Task WithNestedSetupCall_SetupIsNotRewritten()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

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

				namespace MyCode;

				public interface IBar { bool Bar(string x); }
				public interface IChild { IBar GrandChild { get; } }
				public interface IFoo { IChild Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): register the nested 'mock.Child.GrandChild' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						{|CS1929:mock.Child.GrandChild|}.Mock.Bar({|CS0104:It|}.IsAny<string>()).Returns(true);
					}
				}
				""");

		[Test]
		public async Task WithObjectAccess_RemovesObjectProperty()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

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

				namespace MyCode;

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

		[Test]
		public async Task WithQualifiedStrictMockBehavior_IsReplacedWithThrowingWhenNotSetup()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>(Moq.MockBehavior.Strict)|];
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock(Mockolate.MockBehavior.Default.ThrowingWhenNotSetup());
					}
				}
				""");

		[Test]
		public async Task WithSetupCall_MigratesSetup()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

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

				namespace MyCode;

				public interface IFoo { bool Bar(string x); }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Setup.Bar({|CS0104:It|}.IsAny<string>()).Returns(true);
					}
				}
				""");

		[Test]
		public async Task WithSetupCallMultipleArgs_MigratesSetup()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

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

				namespace MyCode;

				public interface IFoo { bool Bar(string x, int y); }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Setup.Bar({|CS0104:It|}.IsAny<string>(), {|CS0104:It|}.IsAny<int>()).Returns(true);
					}
				}
				""");

		[Test]
		public async Task WithStrictMockBehavior_IsReplacedWithThrowingWhenNotSetup()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>(MockBehavior.Strict)|];
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock(Mockolate.MockBehavior.Default.ThrowingWhenNotSetup());
					}
				}
				""");
	}
}
