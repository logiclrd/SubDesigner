using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SubDesigner
{
	public class StampCollection
	{
		public StampCollection(string name)
		{
			Name = name;
		}

		public string Name;
		public List<Stamp> Stamps = new List<Stamp>();
		public List<Stamp> LowResolutionStamps = new List<Stamp>();

		public void ReduceResolution()
		{
			var scaledBitmaps = new Dictionary<BitmapSource, BitmapSource>();

			long before, after;

			before = after = 0;

			foreach (var bitmap in Stamps.Select(stamp => stamp.BitmapSource).OfType<BitmapSource>().Distinct())
			{
				before += bitmap.PixelWidth * bitmap.PixelHeight * 4;
				scaledBitmaps[bitmap] = ReduceResolution(bitmap);
				after += scaledBitmaps[bitmap].PixelWidth * scaledBitmaps[bitmap].PixelHeight * 4;
			}

			System.Diagnostics.Debug.WriteLine("before: {0}    after: {1}", before, after);

			foreach (var stamp in Stamps)
			{
				if (!(stamp.BitmapSource is CroppedBitmap croppedBitmap))
				{
					if (scaledBitmaps.TryGetValue(stamp.BitmapSource!, out var scaledBitmapSource))
						stamp.BitmapSource = scaledBitmapSource;
				}
				else if (scaledBitmaps.TryGetValue(croppedBitmap.Source, out var scaledBitmapSource))
				{
					if (stamp.Descriptor != null)
					{
						double scaleFactor = scaledBitmapSource.Width > scaledBitmapSource.Height
							? stamp.BitmapSource.Width / (double)scaledBitmapSource.Width
							: stamp.BitmapSource.Height / (double)scaledBitmapSource.Height;

						string[] parts = stamp.Descriptor.Split("::");

						if (parts.Length != 2)
							continue;

						string[] crop = parts[0].Split(":");
						string fileName = parts[1];

						if (crop.Length != 4)
							continue;

						if (!int.TryParse(crop[0], out int x))
							continue;
						if (!int.TryParse(crop[1], out int y))
							continue;
						if (!int.TryParse(crop[2], out int w))
							continue;
						if (!int.TryParse(crop[3], out int h))
							continue;

						x = (int)Math.Round(x / scaleFactor);
						y = (int)Math.Round(y / scaleFactor);
						w = (int)Math.Round(w / scaleFactor);
						h = (int)Math.Round(h / scaleFactor);

						stamp.BitmapSource = new CroppedBitmap(
							scaledBitmapSource,
							new Int32Rect(x, y, w, h));
					}
				}
			}
		}

		BitmapSource ReduceResolution(BitmapSource bitmap)
		{
			const double TargetPixelCount = 200_000;

			int w = bitmap.PixelWidth;
			int h = bitmap.PixelHeight;

			int pixels = w * h;

			if (pixels < TargetPixelCount)
				return bitmap;

			double scaleFactor = Math.Sqrt(pixels / TargetPixelCount);

			var drawingVisual = new DrawingVisual();

			using (var context = drawingVisual.RenderOpen())
			{
				var container = new DrawingGroup();

				RenderOptions.SetBitmapScalingMode(container, BitmapScalingMode.HighQuality);

				container.Children.Add(new ImageDrawing(bitmap, new Rect(0, 0, w, h)));

				context.DrawDrawing(container);
			}

			var resizedImage = new RenderTargetBitmap(
				(int)Math.Round(w / scaleFactor),
				(int)Math.Round(h / scaleFactor),
				96.0 / scaleFactor,
				96.0 / scaleFactor,
				PixelFormats.Default);

			resizedImage.Render(drawingVisual);

			return resizedImage;
		}

		public void RestoreResolution()
		{
			this.LowResolutionStamps = new List<Stamp>(this.Stamps);

			var bitmapByFileName = new Dictionary<string, BitmapSource>();

			foreach (var bitmapFileName in Stamps.Select(stamp => stamp.BitmapFileName!).Distinct())
			{
				var bitmap = new BitmapImage();

				using (var fileStream = File.OpenRead(bitmapFileName))
				{
					bitmap.BeginInit();
					bitmap.CacheOption = BitmapCacheOption.OnLoad;
					bitmap.StreamSource = fileStream;
					bitmap.EndInit();
				}

				bitmap.Freeze();

				bitmapByFileName[bitmapFileName] = bitmap;
			}

			foreach (var stamp in Stamps)
			{
				if (bitmapByFileName.TryGetValue(stamp.BitmapFileName!, out var bitmapSource))
				{
					stamp.BitmapSource = bitmapSource;

					if (stamp.Descriptor != null)
					{
						string[] parts = stamp.Descriptor.Split("::");

						if (parts.Length != 2)
							continue;

						string[] crop = parts[0].Split(":");
						string fileName = parts[1];

						if (crop.Length != 4)
							continue;

						if (!int.TryParse(crop[0], out int x))
							continue;
						if (!int.TryParse(crop[1], out int y))
							continue;
						if (!int.TryParse(crop[2], out int w))
							continue;
						if (!int.TryParse(crop[3], out int h))
							continue;

						stamp.BitmapSource = new CroppedBitmap(
							bitmapSource,
							new Int32Rect(x, y, w, h));
					}
				}
			}
		}

		public void ReleaseHighResolution()
		{
			this.Stamps = new List<Stamp>(this.LowResolutionStamps);

			GC.Collect();
		}
	}
}
