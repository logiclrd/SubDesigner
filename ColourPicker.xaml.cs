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
	/// Interaction logic for ColourPicker.xaml
	/// </summary>
	public partial class ColourPicker : UserControl
	{
		public ColourPicker()
		{
			InitializeComponent();

			for (int column = 0; column < NumberOfColumns; column++)
				grdSwatches.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
			for (int row = 0; row < NumberOfRows; row++)
				grdSwatches.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

			for (int row = 0; row < NumberOfRows; row++)
				for (int column = 0; column < NumberOfColumns; column++)
				{
					Color swatchColour;

					if (row == 0)
					{
						byte intensity = (byte)(column * 255 / (NumberOfColumns - 1));

						swatchColour = Color.FromArgb(255, intensity, intensity, intensity);
					}
					else
					{
						double hue = column * 360.0 / NumberOfColumns;
						double lightness = (row - 1) / (double)(NumberOfRows - 1) + 0.5 / NumberOfRows;

						swatchColour = ColorFromHSL(hue, 1.0, lightness);
					}

					var rect = new Rectangle();

					rect.Fill = new SolidColorBrush(swatchColour);

					Grid.SetRow(rect, row);
					Grid.SetColumn(rect, column);

					grdSwatches.Children.Add(rect);
				}
		}

		public event EventHandler? ColourSelected;

		protected virtual void OnColourSelected()
		{
			ColourSelected?.Invoke(this, EventArgs.Empty);
		}

		public Color SelectedColour;
		public bool Primary;

		static Color ColorFromHSL(double hue, double saturation, double lightness)
		{
			if (saturation < 0.0001)
			{
				byte value = (byte)lightness;

				return Color.FromArgb(255, value, value, value);
			}

			double hueValue = hue / 360.0;

			double max;

			if (lightness < 0.5)
				max = lightness * (1 + saturation);
			else
				max = (lightness + saturation) - (lightness * saturation);

			double min = lightness * 2 - max;

			return Color.FromArgb(
				255,
				(byte)(255 * RGBChannelFromHue(min, max, hueValue + 0.33333333333333333333333)),
				(byte)(255 * RGBChannelFromHue(min, max, hueValue)),
				(byte)(255 * RGBChannelFromHue(min, max, hueValue - 0.33333333333333333333333)));
		}

		static double RGBChannelFromHue(double m1, double m2, double h)
		{
			h = (h + 1d) % 1d;

			if (h < 0)
				h += 1;

			if (h * 6 < 1)
				return m1 + (m2 - m1) * 6 * h;
			else if (h * 2 < 1)
				return m2;
			else if (h * 3 < 2)
				return m1 + (m2 - m1) * 6 * (2d / 3d - h);
			else
				return m1;
		}

		const int NumberOfColumns = 14;
		const int NumberOfRows = 10;

		private void grdSwatches_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var mousePosition = e.GetPosition(grdSwatches);

			var swatch = grdSwatches.InputHitTest(mousePosition) as Rectangle;

			if (swatch != null)
			{
				var swatchFill = swatch.Fill as SolidColorBrush;

				if (swatchFill != null)
				{
					SelectedColour = swatchFill.Color;
					Primary = (e.ChangedButton == MouseButton.Left) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

					OnColourSelected();
				}
			}
		}
	}
}
