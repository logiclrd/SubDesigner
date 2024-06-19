using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for TextEditor.xaml
	/// </summary>
	public partial class TextEditor : UserControl
	{
		public TextEditor()
		{
			InitializeComponent();

			foreach (var fontFamily in Fonts.SystemFontFamilies)
				cboFontFace.Items.Add(fontFamily);

			cboFontFace.SelectedIndex = new Random().Next(cboFontFace.Items.Count);

			SelectColour(Colors.Black, primary: true);

			this.Loaded +=
				(_, _) =>
				{
					tPreview.String = txtText.Text;
					tPreview.Curve = ceCurve.GetCurve();
				};
		}

		public string AddButtonText
		{
			get { return cmdAddText.Content?.ToString() ?? "Add Text"; }
			set { cmdAddText.Content = value; }
		}

		public void LoadText(Text text)
		{
			txtText.Text = text.String;
			tbBold.IsChecked = text.Bold;
			tbItalic.IsChecked = text.Italic;
			tbUnderline.IsChecked = text.Underline;
			if (text.Curve is TextCurve curve)
				ceCurve.SetCurve(curve);
			cboFontFace.SelectedItem = text.FontFamily;

			SelectColour(text.Fill, true);
			SelectColour(text.Outline, false);
		}

		public new event EventHandler<Text>? AddText;
		public event EventHandler? Close;

		protected virtual void OnAddText()
		{
			var text = tPreview;

			grdLayout.Children.Remove(text);

			AddText?.Invoke(this, text);
		}

		protected virtual void OnClose()
		{
			Close?.Invoke(this, EventArgs.Empty);
		}

		private void cpColour_ColourSelected(object sender, EventArgs e)
		{
			Color selectedColour = cpColour.SelectedColour;
			var isPrimary = cpColour.Primary;

			if (cpColour.GetColourSwatchLocation(selectedColour) is Rect swatchRectRelativeToColourPicker)
			{
				var selectedColourControl = isPrimary ? rFill : rBorder;

				Rect swatchRect = cpColour.TransformToVisual(grdColour).TransformBounds(swatchRectRelativeToColourPicker);
				Rect selectRect = LayoutInformation.GetLayoutSlot(selectedColourControl);

				var animGeometry = new RectangleGeometry();

				var animVisual = new Path();

				animVisual.Fill = new SolidColorBrush(selectedColour);
				animVisual.Data = animGeometry;

				Grid.SetColumnSpan(animVisual, grdColour.ColumnDefinitions.Count);
				Grid.SetRowSpan(animVisual, grdColour.RowDefinitions.Count);

				grdColour.Children.Add(animVisual);

				var animation = new RectAnimation();

				animation.From = swatchRect;
				animation.To = selectRect;
				animation.Duration = TimeSpan.FromSeconds(0.2);

				animation.Completed +=
					(_, _) =>
					{
						grdColour.Children.Remove(animVisual);
						SelectColour(selectedColour, isPrimary);
					};

				animGeometry.BeginAnimation(RectangleGeometry.RectProperty, animation);
			}
			else
				SelectColour(selectedColour, isPrimary);
		}

		private void cNoBrush_MouseDown(object sender, MouseButtonEventArgs e)
		{
			bool primary = (e.ChangedButton == MouseButton.Left) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

			SelectColour(Colors.Transparent, primary);
		}

		void SelectColour(Color colour, bool primary)
		{
			if (primary)
			{
				rFill.Fill = new SolidColorBrush(colour);
				tPreview.Fill = colour;
			}
			else
			{
				rBorder.Fill = new SolidColorBrush(colour);
				tPreview.Outline = colour;
			}
		}

		private void cmdAddText_Click(object sender, RoutedEventArgs e)
		{
			OnAddText();
			OnClose();
		}

		private void cmdCancel_Click(object sender, RoutedEventArgs e)
		{
			OnClose();
		}

		private void cboFontFace_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (tPreview != null)
				tPreview.FontFamily = (FontFamily)cboFontFace.SelectedItem;
		}

		private void txtText_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (tPreview != null)
				tPreview.String = txtText.Text;

			lblTextCharacterCount.Content = txtText.Text.Length.ToString();
		}

		private void tbBold_Checked(object sender, RoutedEventArgs e)
		{
			if (tPreview != null)
				tPreview.Bold = true;
		}

		private void tbBold_Unchecked(object sender, RoutedEventArgs e)
		{
			if (tPreview != null)
				tPreview.Bold = false;
		}

		private void tbItalic_Checked(object sender, RoutedEventArgs e)
		{
			if (tPreview != null)
				tPreview.Italic = true;
		}

		private void tbItalic_Unchecked(object sender, RoutedEventArgs e)
		{
			if (tPreview != null)
				tPreview.Italic = false;
		}

		private void tbUnderline_Checked(object sender, RoutedEventArgs e)
		{
			if (tPreview != null)
				tPreview.Underline = true;
		}

		private void tbUnderline_Unchecked(object sender, RoutedEventArgs e)
		{
			if (tPreview != null)
				tPreview.Underline = false;
		}

		private void ceCurve_CurveChanged(object sender, EventArgs e)
		{
			if (tPreview != null)
				tPreview.Curve = ceCurve.GetCurve();
		}
	}
}
