using Verifier = Mockolate.Migration.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Migration.Analyzers.MoqAnalyzer,
	Mockolate.Migration.Analyzers.MoqCodeFixProvider>;

namespace Mockolate.Migration.Tests;

public partial class MoqCodeFixProviderTests
{
	public sealed class VerifyEventTests
	{
		[Test]
		public async Task VerifyAdd_OnDelegateProperty_DoesNotMigrateToSubscribed()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				namespace MyCode;

				public interface IFoo { Action MyHandler { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyAdd(m => m.MyHandler += It.IsAny<Action>(), Times.AtLeastOnce);
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { Action MyHandler { get; set; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.{|CS1061:VerifyAdd|}(m => m.MyHandler += It.IsAny<Action>(), Times.AtLeastOnce);
					}
				}
				""");

		[Test]
		public async Task VerifyAdd_WithCustomDelegate_MigratesSubscribed()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;

				namespace MyCode;

				public delegate void FooDelegate(string type, int amount);
				public interface IFoo { event FooDelegate MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyAdd(m => m.MyEvent += It.IsAny<FooDelegate>(), Times.AtLeastOnce);
					}
				}
				""",
				"""
				using Moq;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public delegate void FooDelegate(string type, int amount);
				public interface IFoo { event FooDelegate MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.MyEvent.Subscribed().AtLeastOnce();
					}
				}
				""");

		[Test]
		public async Task VerifyAdd_WithNestedEvent_MigratesViaNavigationChain()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				namespace MyCode;

				public interface IBar { event EventHandler MyEvent; }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyAdd(m => m.Child.MyEvent += It.IsAny<EventHandler>(), Times.Never);
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IBar { event EventHandler MyEvent; }
				public interface IFoo { IBar Child { get; } }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): register the nested 'mock.Child' chain explicitly in the mock setup (Mockolate doesn't auto-mock recursively)
						{|CS1929:mock.Child|}.Mock.Verify.MyEvent.Subscribed().Never();
					}
				}
				""");

		[Test]
		public async Task VerifyAdd_WithoutTimes_MigratesToSubscribedAtLeastOnce()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyAdd(m => m.MyEvent += It.IsAny<EventHandler>());
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.MyEvent.Subscribed().AtLeastOnce();
					}
				}
				""");

		[Test]
		public async Task VerifyAdd_WithTimesOnce_MigratesToSubscribedOnce()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyAdd(m => m.MyEvent += It.IsAny<EventHandler>(), Times.Once());
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.MyEvent.Subscribed().Once();
					}
				}
				""");

		[Test]
		public async Task VerifyAdd_WithUntranslatableTimes_PreservesOriginalCallWithTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var times = Times.Once();
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyAdd(m => m.MyEvent += It.IsAny<EventHandler>(), times);
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var times = Times.Once();
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): translate the Times argument 'times' manually
						mock.{|CS1061:VerifyAdd|}(m => m.MyEvent += It.IsAny<EventHandler>(), times);
					}
				}
				""");

		[Test]
		public async Task VerifyRemove_WithoutTimes_MigratesToUnsubscribedAtLeastOnce()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyRemove(m => m.MyEvent -= It.IsAny<EventHandler>());
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.MyEvent.Unsubscribed().AtLeastOnce();
					}
				}
				""");

		[Test]
		public async Task VerifyRemove_WithTimesExactly_MigratesToUnsubscribedExactly()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyRemove(m => m.MyEvent -= It.IsAny<EventHandler>(), Times.Exactly(2));
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;
				using Mockolate.Verify;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test()
					{
						var mock = IFoo.CreateMock();
						mock.Mock.Verify.MyEvent.Unsubscribed().Exactly(2);
					}
				}
				""");

		[Test]
		public async Task VerifyRemove_WithUntranslatableTimes_PreservesOriginalCallWithTodo()
			=> await Verifier.VerifyCodeFixAsync(
				"""
				using Moq;
				using System;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test(Func<Times> times)
					{
						var mock = [|new Mock<IFoo>()|];
						mock.VerifyRemove(m => m.MyEvent -= It.IsAny<EventHandler>(), times);
					}
				}
				""",
				"""
				using Moq;
				using System;
				using Mockolate;

				namespace MyCode;

				public interface IFoo { event EventHandler MyEvent; }

				public class Tests
				{
					public void Test(Func<Times> times)
					{
						var mock = IFoo.CreateMock();
						// TODO(MockolateM001): translate the Times argument 'times' manually
						mock.{|CS1061:VerifyRemove|}(m => m.MyEvent -= It.IsAny<EventHandler>(), times);
					}
				}
				""");
	}
}
