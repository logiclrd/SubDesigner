using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for CurveEditor.xaml
	/// </summary>
	public partial class CurveEditor : UserControl
	{
		public CurveEditor()
		{
			InitializeComponent();

			EnableDrag(eStartPoint, SetStartPoint);
			EnableDrag(eEndPoint, SetEndPoint);
			EnableDrag(eBendPoint1, SetBendPoint1);
			EnableDrag(eBendPoint2, SetBendPoint2);

			using (PauseChangedEvents())
				SetCurve(CreateDefaultCurve());
		}

		int _pauseChangedEvents;

		public event EventHandler? CurveChanged;

		protected virtual void OnCurveChanged()
		{
			if (_pauseChangedEvents == 0)
				CurveChanged?.Invoke(this, EventArgs.Empty);
		}

		IDisposable PauseChangedEvents()
		{
			_pauseChangedEvents++;

			return new Pauser(() => _pauseChangedEvents--);
		}

		class Pauser : IDisposable
		{
			Action? _unpause;

			public Pauser(Action unpause)
			{
				_unpause = unpause;
			}

			public void Dispose()
			{
				_unpause?.Invoke();
				_unpause = null;
			}
		}

		void EnableDrag(FrameworkElement element, Action<Point> pointSetter)
		{
			bool _dragging = false;
			Point _dragStartPoint = default;

			element.MouseDown +=
				(sender, e) =>
				{
					if (e.ChangedButton == MouseButton.Left)
					{
						_dragging = true;
						element.CaptureMouse();
						_dragStartPoint = e.GetPosition(element);
					}
				};

			element.MouseMove +=
				(sender, e) =>
				{
					if (_dragging)
					{
						var delta = e.GetPosition(element) - (Vector)_dragStartPoint;

						Point newPosition = new Point(
							Canvas.GetLeft(element) + delta.X,
							Canvas.GetTop(element) + delta.Y);

						Canvas.SetLeft(element, newPosition.X);
						Canvas.SetTop(element, newPosition.Y);

						pointSetter(newPosition);
					}
				};

			element.MouseUp +=
				(sender, e) =>
				{
					_dragging = false;
					element.ReleaseMouseCapture();

					EnsureAllElementsAreVisible();
				};
		}

		const double FitMargin = 8;

		void EnsureAllElementsAreVisible()
		{
			Point startPoint = GetStartPoint();
			Point endPoint = GetEndPoint();
			Point bendPoint1 = GetBendPoint1();
			Point bendPoint2 = GetBendPoint2();

			Rect elementBounds = new Rect(startPoint, new Size(0, 0));

			elementBounds.Union(endPoint);

			if ((_curveMode != CurveMode.FlatLine) && (_curveMode != CurveMode.Line))
			{
				elementBounds.Union(bendPoint1);

				if (_curveMode == CurveMode.ComplexCurve)
					elementBounds.Union(bendPoint2);
			}

			if ((elementBounds.Left < 0) || (elementBounds.Right > cnvEditor.ActualWidth)
			 || (elementBounds.Top < 0) || (elementBounds.Bottom > cnvEditor.ActualHeight))
			{
				var fitBounds = new Rect(new Point(0, 0), new Size(cnvEditor.ActualWidth, cnvEditor.ActualHeight));

				double desiredAspectRatio = elementBounds.Width / elementBounds.Height;
				double spaceAspectRatio = fitBounds.Width / fitBounds.Height;

				if (desiredAspectRatio > spaceAspectRatio)
				{
					fitBounds.Height = elementBounds.Height * fitBounds.Width / elementBounds.Width;
					fitBounds.Y = (cnvEditor.ActualHeight - fitBounds.Height) / 2;
				}
				else
				{
					fitBounds.Width = elementBounds.Width * fitBounds.Height / elementBounds.Height;
					fitBounds.X = (cnvEditor.ActualWidth - fitBounds.Width) / 2;
				}

				fitBounds.Inflate(-FitMargin, -FitMargin);

				var anim = new RectAnimation();

				anim.From = elementBounds;
				anim.To = fitBounds;
				anim.Duration = TimeSpan.FromSeconds(0.4);

				var animTarget = new AnimationTarget();

				animTarget.StartRect = elementBounds;
				animTarget.EndRect = fitBounds;

				System.Diagnostics.Debug.WriteLine("Animate to: " + fitBounds);

				anim.Completed += (_, _) => System.Diagnostics.Debug.WriteLine("Animation completed");

				animTarget.RectChanged +=
					(_, _) =>
					{
						try
						{
							SetStartPoint(animTarget.TransformPoint(startPoint));
							SetEndPoint(animTarget.TransformPoint(endPoint));
							SetBendPoint1(animTarget.TransformPoint(bendPoint1));
							SetBendPoint2(animTarget.TransformPoint(bendPoint2));

							System.Diagnostics.Debug.WriteLine("Animation frame: " + animTarget.Rect);
						}
						catch
						{
							System.Diagnostics.Debug.WriteLine("Animation exception");
						}
					};

				animTarget.BeginAnimation(AnimationTarget.RectProperty, anim);
			}
		}

		class AnimationTarget : UIElement
		{
			public static DependencyProperty RectProperty = DependencyProperty.Register(nameof(Rect), typeof(Rect), typeof(AnimationTarget), new UIPropertyMetadata(RectChangedCallback));

			public Rect StartRect;
			public Rect EndRect;

			public Rect Rect
			{
				get => (Rect)GetValue(RectProperty);
				set => SetValue(RectProperty, value);
			}

			public Point TransformPoint(Point pt)
			{
				var currentRect = Rect;

				var relativePosition = pt - StartRect.TopLeft;

				relativePosition.X *= currentRect.Width / StartRect.Width;
				relativePosition.Y *= currentRect.Height / StartRect.Height;

				return (Point)(relativePosition + currentRect.TopLeft);
			}

			public event EventHandler? RectChanged;

			static void RectChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
			{
				((AnimationTarget)d).RectChanged?.Invoke(d, EventArgs.Empty);
			}
		}

		static TextCurve CreateDefaultCurve()
		{
			return new TextCurve() { StartPoint = new Point(100, 150), EndPoint = new Point(500, 100) };
		}

		public void SetCurve(TextCurve curve)
		{
			using (PauseChangedEvents())
			{
				if (curve.BendPoint == null)
					SetCurveMode(CurveMode.Line);
				else if (curve.BendPoint2 == null)
					SetCurveMode(CurveMode.SimpleCurve);
				else
					SetCurveMode(CurveMode.ComplexCurve);

				SetStartPoint(curve.StartPoint);
				SetEndPoint(curve.EndPoint);
				if (curve.BendPoint is Point pt)
					SetBendPoint1(pt);
				if (curve.BendPoint2 is Point pt2)
					SetBendPoint2(pt2);
			}

			OnCurveChanged();
		}

		public TextCurve GetCurve()
		{
			var curve = new TextCurve();

			curve.StartPoint = GetStartPoint();
			curve.EndPoint = GetEndPoint();

			if ((_curveMode != CurveMode.FlatLine) && (_curveMode != CurveMode.Line))
			{
				curve.BendPoint = GetBendPoint1();

				if (_curveMode == CurveMode.ComplexCurve)
					curve.BendPoint2 = GetBendPoint2();
			}

			return curve;
		}

		enum CurveMode
		{
			FlatLine,
			Line,
			SimpleCurve,
			ComplexCurve,
		}

		CurveMode _curveMode;
		bool _settingCurveMode = false;

		void SetCurveMode(CurveMode mode)
		{
			if (_settingCurveMode)
				return;

			_settingCurveMode = true;

			using (PauseChangedEvents())
			{
				try
				{
					_curveMode = mode;

					switch (mode)
					{
						case CurveMode.FlatLine:
						{
							var startPoint = GetStartPoint();
							var endPoint = GetEndPoint();

							if (endPoint.Y != startPoint.Y)
							{
								endPoint.Y = startPoint.Y;

								SetEndPoint(endPoint);
							}

							eStartPoint.Visibility = Visibility.Collapsed;
							eEndPoint.Visibility = Visibility.Collapsed;

							eBendPoint1.Visibility = Visibility.Collapsed;
							eBendPoint2.Visibility = Visibility.Collapsed;

							lBendIndicator1.Visibility = Visibility.Collapsed;
							lBendIndicator2.Visibility = Visibility.Collapsed;

							SetBendPoint1(GetEndPoint());
							SetBendPoint2(GetStartPoint());

							break;
						}
						case CurveMode.Line:
						{
							eStartPoint.Visibility = Visibility.Visible;
							eEndPoint.Visibility = Visibility.Visible;

							eBendPoint1.Visibility = Visibility.Collapsed;
							eBendPoint2.Visibility = Visibility.Collapsed;

							lBendIndicator1.Visibility = Visibility.Collapsed;
							lBendIndicator2.Visibility = Visibility.Collapsed;

							SetBendPoint1(GetEndPoint());
							SetBendPoint2(GetStartPoint());

							break;
						}
						case CurveMode.SimpleCurve:
						case CurveMode.ComplexCurve:
						{
							eStartPoint.Visibility = Visibility.Visible;
							eEndPoint.Visibility = Visibility.Visible;

							eBendPoint1.Visibility = Visibility.Visible;

							if (_curveMode == CurveMode.ComplexCurve)
								eBendPoint2.Visibility = Visibility.Visible;
							else
								eBendPoint2.Visibility = Visibility.Collapsed;

							lBendIndicator1.Visibility = Visibility.Visible;
							lBendIndicator2.Visibility = Visibility.Visible;

							if (mode == CurveMode.SimpleCurve)
								SetBendPoint2(GetBendPoint1());

							break;
						}
					}

					rbFlatLine.IsChecked = (_curveMode == CurveMode.FlatLine);
					rbLine.IsChecked = (_curveMode == CurveMode.Line);
					rbSimpleCurve.IsChecked = (_curveMode == CurveMode.SimpleCurve);
					rbComplexCurve.IsChecked = (_curveMode == CurveMode.ComplexCurve);
				}
				finally
				{
					_settingCurveMode = false;
				}
			}

			OnCurveChanged();
		}

		void SetStartPoint(Point pt)
		{
			Canvas.SetLeft(eStartPoint, pt.X);
			Canvas.SetTop(eStartPoint, pt.Y);

			lBendIndicator1.X1 = pt.X;
			lBendIndicator1.Y1 = pt.Y;

			if ((pPreview.Data is PathGeometry previewData) && previewData.Figures.Any())
				previewData.Figures[0].StartPoint = pt;

			if (_curveMode == CurveMode.Line)
				using (PauseChangedEvents())
					SetBendPoint2(pt);

			OnCurveChanged();
		}

		Point GetStartPoint()
		{
			return new Point(Canvas.GetLeft(eStartPoint), Canvas.GetTop(eStartPoint));
		}

		void SetEndPoint(Point pt)
		{
			Canvas.SetLeft(eEndPoint, pt.X);
			Canvas.SetTop(eEndPoint, pt.Y);

			lBendIndicator2.X1 = pt.X;
			lBendIndicator2.Y1 = pt.Y;

			if ((pPreview.Data is PathGeometry previewData) && previewData.Figures.Any()
			 && previewData.Figures[0].Segments.Any() && (previewData.Figures[0].Segments[0] is BezierSegment segment))
				segment.Point3 = pt;

			if (_curveMode == CurveMode.Line)
				using (PauseChangedEvents())
					SetBendPoint1(pt);

			OnCurveChanged();
		}

		Point GetEndPoint()
		{
			return new Point(Canvas.GetLeft(eEndPoint), Canvas.GetTop(eEndPoint));
		}

		void SetBendPoint1(Point pt)
		{
			Canvas.SetLeft(eBendPoint1, pt.X);
			Canvas.SetTop(eBendPoint1, pt.Y);

			lBendIndicator1.X2 = pt.X;
			lBendIndicator1.Y2 = pt.Y;

			if ((pPreview.Data is PathGeometry previewData) && previewData.Figures.Any()
			 && previewData.Figures[0].Segments.Any() && (previewData.Figures[0].Segments[0] is BezierSegment segment))
				segment.Point1 = pt;

			if (_curveMode == CurveMode.SimpleCurve)
				using (PauseChangedEvents())
					SetBendPoint2(pt);

			if (!_settingCurveMode)
				OnCurveChanged();
		}

		Point GetBendPoint1()
		{
			return new Point(Canvas.GetLeft(eBendPoint1), Canvas.GetTop(eBendPoint1));
		}

		void SetBendPoint2(Point pt)
		{
			Canvas.SetLeft(eBendPoint2, pt.X);
			Canvas.SetTop(eBendPoint2, pt.Y);

			lBendIndicator2.X2 = pt.X;
			lBendIndicator2.Y2 = pt.Y;

			if ((pPreview.Data is PathGeometry previewData) && previewData.Figures.Any()
			 && previewData.Figures[0].Segments.Any() && (previewData.Figures[0].Segments[0] is BezierSegment segment))
				segment.Point2 = pt;

			if (!_settingCurveMode)
				OnCurveChanged();
		}

		Point GetBendPoint2()
		{
			return new Point(Canvas.GetLeft(eBendPoint2), Canvas.GetTop(eBendPoint2));
		}

		private void rbFlatLine_Checked(object sender, RoutedEventArgs e)
		{
			SetCurveMode(CurveMode.FlatLine);
		}

		private void rbLine_Checked(object sender, RoutedEventArgs e)
		{
			SetCurveMode(CurveMode.Line);
		}

		private void rbSimpleCurve_Checked(object sender, RoutedEventArgs e)
		{
			var previousCurveMode = _curveMode;

			if (previousCurveMode == CurveMode.ComplexCurve)
			{
				Point p1 = GetBendPoint1();
				Point p2 = GetBendPoint2();

				Vector v = p2 - p1;

				Point midpoint = p1 + v * 0.5;

				SetBendPoint1(midpoint);
			}

			using (PauseChangedEvents())
			{
				SetCurveMode(CurveMode.SimpleCurve);

				if ((previousCurveMode == CurveMode.FlatLine) || (previousCurveMode == CurveMode.Line))
				{
					Point p1 = GetStartPoint();
					Point p2 = GetEndPoint();

					Vector v = p2 - p1;

					Point midpoint = p1 + v * 0.5;

					Vector normal = new Vector(v.Y, -v.X);

					SetBendPoint1(midpoint + normal / 6);
				}
			}

			OnCurveChanged();
		}

		private void rbComplexCurve_Checked(object sender, RoutedEventArgs e)
		{
			var previousCurveMode = _curveMode;

			using (PauseChangedEvents())
			{
				SetCurveMode(CurveMode.ComplexCurve);

				switch (previousCurveMode)
				{
					case CurveMode.FlatLine:
					case CurveMode.Line:
					{
						Point p1 = GetStartPoint();
						Point p2 = GetEndPoint();

						Vector v = p2 - p1;

						Vector normal = new Vector(v.Y, -v.X);

						SetBendPoint1(p1 + v * 0.3 + normal / 4);
						SetBendPoint2(p1 + v * 0.7 - normal / 4);

						break;
					}
					case CurveMode.SimpleCurve:
					{
						Point p1 = GetStartPoint();
						Point p2 = GetEndPoint();

						Vector v = p2 - p1;

						Point bend = GetBendPoint1();

						SetBendPoint1(bend - v / 5);
						SetBendPoint2(bend + v / 5);

						break;
					}
				}
			}

			OnCurveChanged();
		}
	}
}
