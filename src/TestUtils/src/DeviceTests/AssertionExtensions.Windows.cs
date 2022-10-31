using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.DirectX;
using Windows.Storage.Streams;
using Xunit;
using WColor = Windows.UI.Color;

namespace Microsoft.Maui.DeviceTests
{
	public static partial class AssertionExtensions
	{
		public static Task<string> CreateColorAtPointErrorAsync(this CanvasBitmap bitmap, WColor expectedColor, int x, int y) =>
			CreateColorErrorAsync(bitmap, $"Expected {expectedColor} at point {x},{y} in renderered view.");

		public static async Task<string> CreateColorErrorAsync(this CanvasBitmap bitmap, string message) =>
			$"{message} This is what it looked like:<img>{await bitmap.ToBase64StringAsync()}</img>";

		public static async Task<string> CreateEqualErrorAsync(this CanvasBitmap bitmap, CanvasBitmap other, string message) =>
			$"{message} This is what it looked like: <img>{await bitmap.ToBase64StringAsync()}</img> and <img>{await other.ToBase64StringAsync()}</img>";

		public static async Task<string> ToBase64StringAsync(this CanvasBitmap bitmap)
		{
			using var ms = new InMemoryRandomAccessStream();
			await bitmap.SaveAsync(ms, CanvasBitmapFileFormat.Png);

			using var ms2 = new MemoryStream();
			await ms.AsStreamForRead().CopyToAsync(ms2);

			return Convert.ToBase64String(ms2.ToArray());
		}

		public static WColor ColorAtPoint(this CanvasBitmap bitmap, int x, int y, bool includeAlpha = false)
		{
			var pixel = bitmap.GetPixelColors(x, y, 1, 1)[0];
			return includeAlpha
				? pixel
				: WColor.FromArgb(255, pixel.R, pixel.G, pixel.B);
		}

		public static Task AttachAndRunAsync(this FrameworkElement view, Action action) =>
			view.AttachAndRunAsync(() =>
			{
				action();
				return Task.FromResult(true);
			});

		public static Task<T> AttachAndRunAsync<T>(this FrameworkElement view, Func<T> action) =>
			view.AttachAndRunAsync(() =>
			{
				var result = action();
				return Task.FromResult(result);
			});

		public static Task AttachAndRunAsync(this FrameworkElement view, Func<Task> action) =>
			view.AttachAndRunAsync(async () =>
			{
				await action();
				return true;
			});

		public static async Task<T> AttachAndRunAsync<T>(this FrameworkElement view, Func<Task<T>> action)
		{
			if (view.Parent is Border wrapper)
				view = wrapper;

			TaskCompletionSource? tcs = null;
			TaskCompletionSource? unloadedTcs = null;
			Window? window = null;

			if (view.Parent == null)
			{
				// prepare to wait for element to be in the UI
				tcs = new TaskCompletionSource();
				unloadedTcs = new TaskCompletionSource();

				view.Loaded += OnViewLoaded;

				// attach to the UI
				Grid grid;
				window = new Window
				{
					Content = new Grid
					{
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
						Children =
						{
							(grid = new Grid
							{
								Width = view.Width,
								Height = view.Height,
								Children =
								{
									view
								}
							})
						}
					}
				};
				window.Activate();

				// wait for element to be loaded
				await tcs.Task;
				view.Unloaded += OnViewUnloaded;

				// continue with the run
				T result;
				try
				{
					result = await Run(action);
					grid.Children.Clear();
				}
				finally
				{
					await unloadedTcs.Task;
					await Task.Delay(10);
					window.Close();
				}

				return result;
			}
			else
			{
				return await Run(action);
			}

			static async Task<T> Run(Func<Task<T>> action)
			{
				return await action();
			}

			void OnViewLoaded(object sender, RoutedEventArgs e)
			{
				view.Loaded -= OnViewLoaded;
				tcs?.SetResult();
			}

			void OnViewUnloaded(object sender, RoutedEventArgs e)
			{
				view.Unloaded -= OnViewUnloaded;
				unloadedTcs?.SetResult();
			}
		}

		public static Task<CanvasBitmap> ToBitmapAsync(this FrameworkElement view) =>
			view.AttachAndRunAsync(async () =>
			{
				if (view.Parent is Border wrapper)
					view = wrapper;

				var bmp = new RenderTargetBitmap();
				await bmp.RenderAsync(view);
				var pixels = await bmp.GetPixelsAsync();
				var width = bmp.PixelWidth;
				var height = bmp.PixelHeight;

				var device = CanvasDevice.GetSharedDevice();

				return CanvasBitmap.CreateFromBytes(device, pixels, width, height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
			});

		public static CanvasBitmap AssertColorAtPoint(this CanvasBitmap bitmap, WColor expectedColor, uint x, uint y) =>
			bitmap.AssertColorAtPoint(expectedColor, (int)x, (int)y);

		public static CanvasBitmap AssertColorAtPoint(this CanvasBitmap bitmap, WColor expectedColor, int x, int y)
		{
			var actualColor = bitmap.ColorAtPoint(x, y);

			if (!actualColor.IsEquivalent(expectedColor))
				Assert.Equal(expectedColor, actualColor);

			return bitmap;
		}

		public static CanvasBitmap AssertColorAtCenter(this CanvasBitmap bitmap, WColor expectedColor) =>
			bitmap.AssertColorAtPoint(expectedColor, bitmap.SizeInPixels.Width / 2, bitmap.SizeInPixels.Height / 2);

		public static CanvasBitmap AssertColorAtBottomLeft(this CanvasBitmap bitmap, WColor expectedColor) =>
			bitmap.AssertColorAtPoint(expectedColor, 0, 0);

		public static CanvasBitmap AssertColorAtBottomRight(this CanvasBitmap bitmap, WColor expectedColor)
			=> bitmap.AssertColorAtPoint(expectedColor, bitmap.SizeInPixels.Width - 1, 0);

		public static CanvasBitmap AssertColorAtTopLeft(this CanvasBitmap bitmap, WColor expectedColor)
			=> bitmap.AssertColorAtPoint(expectedColor, 0, bitmap.SizeInPixels.Height - 1);

		public static CanvasBitmap AssertColorAtTopRight(this CanvasBitmap bitmap, WColor expectedColor)
			=> bitmap.AssertColorAtPoint(expectedColor, bitmap.SizeInPixels.Width - 1, bitmap.SizeInPixels.Height - 1);

		public static async Task<CanvasBitmap> AssertContainsColorAsync(this CanvasBitmap bitmap, WColor expectedColor)
		{
			var colors = bitmap.GetPixelColors();

			foreach (var c in colors)
			{
				if (c.IsEquivalent(expectedColor))
				{
					return bitmap;
				}
			}

			Assert.True(false, await CreateColorErrorAsync(bitmap, $"Color {expectedColor} not found."));
			return bitmap;
		}

		public static Task<CanvasBitmap> AssertContainsColorAsync(this FrameworkElement view, Maui.Graphics.Color expectedColor) =>
			AssertContainsColorAsync(view, expectedColor.ToWindowsColor());

		public static async Task<CanvasBitmap> AssertContainsColorAsync(this FrameworkElement view, WColor expectedColor)
		{
			var bitmap = await view.ToBitmapAsync();
			return await AssertContainsColorAsync(bitmap, expectedColor);
		}

		public static async Task<CanvasBitmap> AssertColorAtPointAsync(this FrameworkElement view, WColor expectedColor, int x, int y)
		{
			var bitmap = await view.ToBitmapAsync();
			return bitmap.AssertColorAtPoint(expectedColor, x, y);
		}

		public static async Task<CanvasBitmap> AssertColorAtCenterAsync(this FrameworkElement view, WColor expectedColor)
		{
			var bitmap = await view.ToBitmapAsync();
			return bitmap.AssertColorAtCenter(expectedColor);
		}

		public static async Task<CanvasBitmap> AssertColorAtBottomLeftAsync(this FrameworkElement view, WColor expectedColor)
		{
			var bitmap = await view.ToBitmapAsync();
			return bitmap.AssertColorAtBottomLeft(expectedColor);
		}

		public static async Task<CanvasBitmap> AssertColorAtBottomRightAsync(this FrameworkElement view, WColor expectedColor)
		{
			var bitmap = await view.ToBitmapAsync();
			return bitmap.AssertColorAtBottomRight(expectedColor);
		}

		public static async Task<CanvasBitmap> AssertColorAtTopLeftAsync(this FrameworkElement view, WColor expectedColor)
		{
			var bitmap = await view.ToBitmapAsync();
			return bitmap.AssertColorAtTopLeft(expectedColor);
		}

		public static async Task<CanvasBitmap> AssertColorAtTopRightAsync(this FrameworkElement view, WColor expectedColor)
		{
			var bitmap = await view.ToBitmapAsync();
			return bitmap.AssertColorAtTopRight(expectedColor);
		}

		public static async Task AssertEqualAsync(this CanvasBitmap bitmap, CanvasBitmap other)
		{
			Assert.NotNull(bitmap);
			Assert.NotNull(other);

			Assert.Equal(bitmap.SizeInPixels, other.SizeInPixels);

			Assert.True(IsMatching(), await CreateEqualErrorAsync(bitmap, other, $"Images did not match."));

			bool IsMatching()
			{
				var first = bitmap.GetPixelColors();
				var second = other.GetPixelColors();
				for (int i = 0; i < first.Length; i++)
				{
					if (first[i] != second[i])
						return false;
				}
				return true;
			}
		}

		public static TextTrimming ToPlatform(this LineBreakMode mode) =>
			mode switch
			{
				LineBreakMode.NoWrap => TextTrimming.Clip,
				LineBreakMode.WordWrap => TextTrimming.None,
				LineBreakMode.CharacterWrap => TextTrimming.WordEllipsis,
				LineBreakMode.HeadTruncation => TextTrimming.WordEllipsis,
				LineBreakMode.TailTruncation => TextTrimming.CharacterEllipsis,
				LineBreakMode.MiddleTruncation => TextTrimming.WordEllipsis,
				_ => throw new ArgumentOutOfRangeException(nameof(mode))
			};
	}
}