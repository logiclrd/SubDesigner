using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for PrintPreview.xaml
	/// </summary>
	public partial class PrintPreview : UserControl
	{
		public PrintPreview()
		{
			InitializeComponent();

			_rotation = new AxisAngleRotation3D();
			_rotation.Axis = new Vector3D(0, 1, 0);
		}

		AxisAngleRotation3D _rotation;

		public void CloneViewport3D(Viewport3D viewport)
		{
			v3dViewport.Camera =
				new PerspectiveCamera()
				{
					Position = new Point3D(0, 0, -40),
					LookDirection = new Vector3D(0, 0, 1),
					UpDirection = new Vector3D(0, 1, 0),
					NearPlaneDistance = 0,
					FarPlaneDistance = 50,
					FieldOfView = 45
				};

			var rotationTransform = new RotateTransform3D();

			rotationTransform.Rotation = _rotation;

			var pitchTransform = new RotateTransform3D();

			pitchTransform.Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -25);

			var transformGroup = new Transform3DGroup();

			transformGroup.Children.Add(rotationTransform);
			transformGroup.Children.Add(pitchTransform);

			foreach (var child in viewport.Children)
			{
				if (child is ModelVisual3D visual)
				{
					var clonedVisual = new ModelVisual3D();

					clonedVisual.Content = visual.Content;
					clonedVisual.Transform = transformGroup;

					v3dViewport.Children.Add(clonedVisual);
				}
				else
					v3dViewport.Children.Add(child);
			}
		}

		public int MugIndex
		{
			get => _mugIndex;
			set
			{
				_mugIndex = value;

				rMugIndex.Text = value.ToString();
			}
		}

		public UIElement? Visual
		{
			get => _visual;
			set => _visual = value;
		}

		int _mugIndex;
		UIElement? _visual;

		public event EventHandler<bool>? Close;

		protected virtual void OnClose(bool proceeded)
		{
			Close?.Invoke(this, proceeded);
		}

		bool _dragging = false;
		IInputElement? _dragSender;
		Point _dragPosition;
		double _dragStartAngle;

		public void NotifyDisplayed()
		{
			var tmrStartAnimation = new DispatcherTimer();

			tmrStartAnimation.Interval = TimeSpan.FromSeconds(3);
			tmrStartAnimation.Tick +=
				(_, _) =>
				{
					tmrStartAnimation.IsEnabled = false;

					var textSizeAnimation = new DoubleAnimation();

					textSizeAnimation.From = tbDesignNumber.FontSize;
					textSizeAnimation.To = 30;
					textSizeAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));

					tbDesignNumber.BeginAnimation(TextBlock.FontSizeProperty, textSizeAnimation);
					tbRememberThis.BeginAnimation(TextBlock.FontSizeProperty, textSizeAnimation);

					var viewportAnimation = new ThicknessAnimation();

					viewportAnimation.From = v3dViewport.Margin;
					viewportAnimation.To = new Thickness(0, 0, 0, 30);
					viewportAnimation.Duration = textSizeAnimation.Duration;

					v3dViewport.BeginAnimation(Viewport3D.MarginProperty, viewportAnimation);
				};

			tmrStartAnimation.IsEnabled = true;
		}

		private void StartMugDrag(object sender, MouseButtonEventArgs e)
		{
			_dragSender = (IInputElement)sender;

			_dragging = (e.LeftButton == MouseButtonState.Pressed);

			var clickPosition = Mouse.GetPosition(_dragSender);

			_dragPosition = new Point(
				clickPosition.X - v3dViewport.ActualWidth * 0.5,
				v3dViewport.ActualHeight * 0.5 - clickPosition.Y);

			_dragStartAngle = _rotation.Angle;

			_dragSender.CaptureMouse();
		}

		private void DoMugDrag(object sender, MouseEventArgs e)
		{
			if (_dragging && (sender == _dragSender))
			{
				Point mousePosition = Mouse.GetPosition(_dragSender);

				Point relativeMousePosition = new Point(
					mousePosition.X - v3dViewport.ActualWidth * 0.5,
					mousePosition.Y - v3dViewport.ActualHeight * 0.5);

				var angleDelta = relativeMousePosition.X - _dragPosition.X;

				_rotation.Angle = _dragStartAngle + angleDelta;
			}
		}

		private void EndMugDrag(object sender, MouseButtonEventArgs e)
		{
			if (sender == _dragSender)
			{
				_dragging = (e.LeftButton == MouseButtonState.Pressed);

				if (!_dragging)
				{
					_dragSender.ReleaseMouseCapture();
					_dragSender = null;
				}
			}
		}

		private void cmdProceed_Click(object sender, RoutedEventArgs e)
		{
			if (_visual == null)
			{
				MessageBox.Show("Visual is not initialized", "Internal Error");
				return;
			}

			var render = new RenderTargetBitmap(4096, 1710, 192, 192, PixelFormats.Pbgra32);

			_visual.Measure(new Size(4096, 1710));
			_visual.UpdateLayout();

			render.Render(_visual);

			var flipped = new TransformedBitmap();

			flipped.BeginInit();
			flipped.Source = render;
			flipped.Transform = new ScaleTransform(scaleX: -1, scaleY: 1);
			flipped.EndInit();

			try
			{
				var printFolder = Path.Combine(Constants.MugDesignsFolder, "Print");

				if (!Directory.Exists(printFolder))
					Directory.CreateDirectory(printFolder);

				var encoder = new PngBitmapEncoder();

				encoder.Frames.Add(BitmapFrame.Create(flipped));
				using (var stream = File.OpenWrite(Path.Combine(printFolder, _mugIndex + ".png")))
					encoder.Save(stream);
			}
			catch
			{
				grdError.Visibility = Visibility.Visible;
				return;
			}

			OnClose(true);
		}

		private void cmdNotYet_Click(object sender, RoutedEventArgs e)
		{
			OnClose(false);
		}
	}
}
