using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for Text.xaml
	/// </summary>
	public partial class Text : UserControl
	{
		public Text()
		{
			InitializeComponent();

			var fontFamilyPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(FontFamilyProperty, typeof(Control));

			fontFamilyPropertyDescriptor.AddValueChanged(
				this,
				Text_FontFamilyChanged);
		}

		string? _string;
		bool _bold;
		bool _italic;
		bool _underline;
		Color _fill;
		Color _outline;
		TextCurve? _curve;

		public string? String
		{
			get { return _string; }
			set
			{
				_string = value;
				Render();
			}
		}

		void Text_FontFamilyChanged(object? sender, EventArgs e)
		{
			Render();
		}

		public bool Bold
		{
			get { return _bold; }
			set
			{
				_bold = value;
				Render();
			}
		}

		public bool Italic
		{
			get { return _italic; }
			set
			{
				_italic = value;
				Render();
			}
		}

		public bool Underline
		{
			get { return _underline; }
			set
			{
				_underline = value;
				Render();
			}
		}

		public Color Fill
		{
			get { return _fill; }
			set
			{
				_fill = value;
				Render();
			}
		}

		public Color Outline
		{
			get { return _outline; }
			set
			{
				_outline = value;
				Render();
			}
		}

		public TextCurve? Curve
		{
			get { return _curve; }
			set
			{
				_curve = value;
				Render();
			}
		}

		void Render()
		{
			cnvLayout.Children.Clear();

			if (String == null)
				return;

			double textLength = 0.0;

			var fillBrush = new SolidColorBrush(_fill);

			foreach (char ch in String)
			{
				var tb = new TextBlock();

				tb.Text = ch.ToString();
				tb.FontFamily = FontFamily;
				tb.FontSize = FontSize;

				if (_bold)
					tb.FontWeight = FontWeights.Bold;
				if (_italic)
					tb.FontStyle = FontStyles.Italic;

				tb.Foreground = fillBrush;

				cnvLayout.Children.Add(tb);

				tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

				textLength += tb.DesiredSize.Width;
			}

			PathFigure curveFigure;

			if (Curve == null)
				curveFigure = TextCurve.FlatLine(textLength).ToPathFigure();
			else
				curveFigure = Curve.ToPathFigure();

			var pathLength = GetPathFigureLength(curveFigure);

			double scalingFactor = pathLength / textLength;

			var curveGeometry = new PathGeometry(new[] { curveFigure });

			double baseLine = scalingFactor * FontSize * FontFamily.Baseline;

			double progress = 0.0;

			var scaleTransform = new ScaleTransform(scalingFactor, scalingFactor);

			foreach (var tb in cnvLayout.Children.OfType<TextBlock>())
			{
				double width = scalingFactor * tb.DesiredSize.Width;

				double widthOnPath = width / pathLength;

				progress += 0.5 * widthOnPath;

				curveGeometry.GetPointAtFractionLength(
					progress,
					out var point,
					out var tangent);

				var angle = Math.Atan2(tangent.Y, tangent.X) * 57.295779;

				var rotateTransform = new RotateTransform(angle, width * 0.5, baseLine);
				var translateTransform = new TranslateTransform(point.X - width * 0.5, point.Y - baseLine);

				var transform = new TransformGroup();

				transform.Children.Add(scaleTransform);
				transform.Children.Add(rotateTransform);
				transform.Children.Add(translateTransform);

				tb.RenderTransform = transform;

				progress += 0.5 * widthOnPath;
			}

			if (_underline)
			{
				var underlinePath = new Path();

				underlinePath.Data = curveGeometry;
				underlinePath.Stroke = new SolidColorBrush(_fill);
				underlinePath.StrokeThickness = scalingFactor;

				cnvLayout.Children.Add(underlinePath);
			}

			if (_outline.A > 0)
			{
				var culture = CultureInfo.GetCultureInfo("en-us");

				var typeface = new Typeface(
					FontFamily,
					_italic ? FontStyles.Italic : FontStyles.Normal,
					_bold ? FontWeights.Bold : FontWeights.Normal,
					FontStretches.Normal);

				var outlines = new List<Path>();

				foreach (var tb in cnvLayout.Children.OfType<TextBlock>())
				{
					var formattedText = new FormattedText(
						tb.Text,
						culture,
						FlowDirection.LeftToRight,
						typeface,
						FontSize,
						Brushes.Black,
						pixelsPerDip: 1.0);

					var outlineGeometry = formattedText.BuildGeometry(default(Point));

					var outline = new Path();

					outline.Stroke = new SolidColorBrush(_outline);
					outline.StrokeThickness = scalingFactor * 0.5;

					outline.Data = outlineGeometry;

					outline.RenderTransform = tb.RenderTransform;

					outlines.Add(outline);
				}

				foreach (var outline in outlines)
					cnvLayout.Children.Add(outline);
			}

			cnvLayout.Arrange(new Rect(RenderSize));
			cnvLayout.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

			CenterContent();
		}

		void CenterContent()
		{
			var bounds = VisualTreeHelper.GetDescendantBounds(cnvLayout);

			cnvLayout.RenderTransform = new TranslateTransform(
				0.5 * (ActualWidth - bounds.Width),
				0.5 * (ActualHeight - bounds.Height));
		}

		public void FitContent()
		{
			var bounds = VisualTreeHelper.GetDescendantBounds(cnvLayout);

			cnvLayout.RenderTransform = new TranslateTransform(
				-bounds.Left,
				-bounds.Top);

			cnvLayout.Width = bounds.Width;
			cnvLayout.Height = bounds.Height;

			Width = bounds.Width;
			Height = bounds.Height;
		}

		public void RestoreSize(double width, double height)
		{
			cnvLayout.Width = width;
			cnvLayout.Height = height;

			Width = width;
			Height = height;
		}

		static double GetPathFigureLength(PathFigure figure)
		{
			var flattened = figure.GetFlattenedPathFigure();

			double totalLength = 0;

			var pt = figure.StartPoint;

			foreach (var segment in flattened.Segments)
			{
				if (segment is LineSegment lineSegment)
				{
					totalLength += (lineSegment.Point - pt).Length;
					pt = lineSegment.Point;
				}
				else if (segment is PolyLineSegment polyLineSegment)
				{
					foreach (var point in polyLineSegment.Points)
					{
						totalLength += (point - pt).Length;
						pt = point;
					}
				}
			}

			return totalLength;
		}
	}
}
