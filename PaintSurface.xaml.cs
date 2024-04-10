using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for PaintSurface.xaml
	/// </summary>
	public partial class PaintSurface : UserControl
	{
		public PaintSurface()
		{
			InitializeComponent();
		}

		public void AddStamp(Point location, ImageSource stampBitmap, Size initialSize)
		{
			location = new Point(
				location.X * 2048 / this.ActualWidth,
				location.Y * 855 / this.ActualHeight);

			initialSize = new Size(
				initialSize.Width * 2048 / this.ActualWidth,
				initialSize.Height * 855 / this.ActualHeight);

			var image = new Image();

			image.Width = initialSize.Width;
			image.Height = initialSize.Height;
			image.Source = stampBitmap;

			Canvas.SetLeft(image, location.X);
			Canvas.SetTop(image, location.Y);

			cnvContents.Children.Add(image);
		}
	}
}
