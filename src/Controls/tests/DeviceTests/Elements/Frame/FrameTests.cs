﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Shapes;
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
		public double MaximumWidth
		{
			get => base.MaximumWidthRequest;
			set => base.MaximumWidthRequest = value;
		}

		public double MaximumHeight
		{
			get => base.MaximumHeightRequest;
			set => base.MaximumHeightRequest = value;
		}

		public double MinimumWidth
		{
			get => base.MinimumWidthRequest;
			set => base.MinimumWidthRequest = value;
		}

		public double MinimumHeight
		{
			get => base.MinimumHeightRequest;
			set => base.MinimumHeightRequest = value;
		}

		public Visibility Visibility
		{
			get => base.IsVisible ? Visibility.Visible : Visibility.Hidden;
			set
			{
				if (value == Visibility.Visible)
					base.IsVisible = true;
				else
					base.IsVisible = false;
			}
		}

		public Semantics Semantics
		{
			get;
			set;
		}

		double IStubBase.Width
		{
			get => base.WidthRequest;
			set => base.WidthRequest = value;
		}

		double IStubBase.Height
		{
			get => base.HeightRequest;
			set => base.HeightRequest = value;
		}

		Paint IStubBase.Background
		{
			get => base.Background;
			set => base.Background = value;
		}

		IShape IStubBase.Clip
		{
			get;
			set;
		}

		IShape IView.Clip
		{
			get => (this as IStubBase).Clip;
		}


		IElement IStubBase.Parent
		{
			get => (IElement)base.Parent;
			set => base.Parent = (Element)value;
		}
	}

	[Category(TestCategory.Frame)]
	public class FrameHandlerTest : HandlerTestBase<FrameRendererTest, FrameStub>
	{
		public FrameHandlerTest()
		{

		}
	}

	public class FrameRendererTest : FrameRenderer
	{
#if ANDROID

		public FrameRendererTest() : base(MauiProgramDefaults.DefaultContext)
		{
		}

#endif
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