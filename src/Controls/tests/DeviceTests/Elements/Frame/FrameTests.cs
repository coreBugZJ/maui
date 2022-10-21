using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	public class FrameStub : Frame, IStubBase
	{
		public double MaximumWidth { get; set; }
		public double MaximumHeight { get; set; }
		public double MinimumWidth { get; set; }
		public double MinimumHeight { get; set; }
		public Visibility Visibility { get; set; }
		public Semantics Semantics { get; set; }
		double IStubBase.Width { get; set; }
		double IStubBase.Height { get; set; }
		Paint IStubBase.Background { get; set; }
		IShape IStubBase.Clip { get; set; }
		IElement IStubBase.Parent { get; set; }
	}

	[Category(TestCategory.Frame)]
	public class FrameHandlerTest : HandlerTestBase<FrameRenderer, FrameStub>
	{
		public FrameHandlerTest()
		{

		}
	}

	[Category(TestCategory.Frame)]
	public partial class FrameTests : ControlsHandlerTestBase
	{
		void SetupBuilder()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Frame, FrameRenderer>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});
		}

		[Fact(DisplayName = "Basic Frame Test")]
		public async Task BasicFrameTest()
		{
			SetupBuilder();

			var frame = new Frame()
			{
				HeightRequest = 300,
				WidthRequest = 300,
				Content = new Label()
				{
					VerticalOptions = LayoutOptions.Center,
					HorizontalOptions = LayoutOptions.Center,
					Text = "Hello Frame"
				}
			};

			var labelFrame =
				await InvokeOnMainThreadAsync(() =>
					frame.ToHandler(MauiContext).PlatformView.AttachAndRun(async () =>
					{
						(frame as IView).Measure(300, 300);
						(frame as IView).Arrange(new Graphics.Rect(0, 0, 300, 300));

						await OnFrameSetToNotEmpty(frame.Content);

						return frame.Content.Frame;

					})
				);


			// validate label is centered in the frame
			Assert.True(Math.Abs(((300 - labelFrame.Width) / 2) - labelFrame.X) < 1);
			Assert.True(Math.Abs(((300 - labelFrame.Height) / 2) - labelFrame.Y) < 1);
		}
	}
}