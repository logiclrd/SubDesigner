using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using HelixToolkit.Wpf;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			var importer = new ModelImporter();

			var model = importer.Load("Mug.obj", Dispatcher);

			_textureImage = FindTexture(model);

			model.Children.Add(new AmbientLight(Color.FromRgb(128, 128, 128)));

			var rotationTransform = new RotateTransform3D();

			_rotation = new AxisAngleRotation3D();
			_rotation.Axis = new Vector3D(0, 1, 0);

			rotationTransform.Rotation = _rotation;

			var pitchTransform = new RotateTransform3D();

			pitchTransform.Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -25);

			var transformGroup = new Transform3DGroup();

			transformGroup.Children.Add(rotationTransform);
			transformGroup.Children.Add(pitchTransform);

			_visual = new ModelVisual3D();
			_visual.Content = model;
			_visual.Transform = transformGroup;

			v3dViewport.Camera =
				new PerspectiveCamera()
				{
					Position = new Point3D(0, 0, -25),
					LookDirection = new Vector3D(0, 0, 1),
					UpDirection = new Vector3D(0, 1, 0),
					NearPlaneDistance = 0,
					FarPlaneDistance = 50,
					FieldOfView = 45
				};

			var timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);

			timer.Tick += Timer_Tick;
			timer.Interval = TimeSpan.FromMilliseconds(100);
			timer.Start();

			v3dViewport.Children.Add(_visual);
			v3dViewport.Children.Add(new DefaultLights());
		}

		WriteableBitmap? FindTexture(Model3DGroup model)
		{
			var geometryModel = model.Children.OfType<GeometryModel3D>().First();

			var materialGroup = geometryModel.Material as MaterialGroup;

			if (materialGroup != null)
			{
				materialGroup.Children.RemoveAt(1);

				var diffuseMaterial = materialGroup.Children.OfType<DiffuseMaterial>().SingleOrDefault();

				if (diffuseMaterial != null)
				{
					var imageBrush = diffuseMaterial.Brush as ImageBrush;

					if (imageBrush != null)
					{
						var imageSource = imageBrush.ImageSource as BitmapSource;

						if (imageSource != null)
						{
							var writeableBitmap = new WriteableBitmap(imageSource);

							imageBrush.ImageSource = writeableBitmap;

							return writeableBitmap;
						}
					}
				}
			}

			return null;
		}

		Random _rnd = new Random();
		WriteableBitmap? _textureImage;

		private void Timer_Tick(object? sender, EventArgs e)
		{
			if (_textureImage == null)
				return;

			var x1 = _rnd.Next(2048);
			var x2 = _rnd.Next(2048);
			var y1 = _rnd.Next(855);
			var y2 = _rnd.Next(855);

			if (_textureImage.TryLock(new Duration(TimeSpan.FromMilliseconds(50))))
			{
				const int PixelColour = unchecked((int)0xFFFF0000);

				unsafe
				{
					byte *backBuffer = (byte *)_textureImage.BackBuffer;

					try
					{
						int w = _textureImage.PixelWidth;
						int h = _textureImage.PixelHeight;
						int stride = _textureImage.BackBufferStride;

						int dx = Math.Abs(x2 - x1);
						int dy = Math.Abs(y2 - y1);

						if (dx > dy)
						{
							if (x1 > x2)
							{
								Swap(ref x1, ref x2);
								Swap(ref y1, ref y2);
							}

							dy = y2 - y1;

							for (int x = x1; x <= x2; x++)
							{
								int y = (x - x1) * dy / dx + y1;

								if ((x >= 0) && (x < w) && (y >= 0) && (y < h))
								{
									int o = y * _textureImage.BackBufferStride + x * 4;

									byte *pixelPointer = &backBuffer[o];

									*(int *)pixelPointer = PixelColour;
								}
							}
						}
						else
						{
							if (y1 > y2)
							{
								Swap(ref x1, ref x2);
								Swap(ref y1, ref y2);
							}

							dx = x2 - x1;

							for (int y = y1; y <= y2; y++)
							{
								int x = (y - y1) * dx / dy + x1;

								if ((x >= 0) && (x < w) && (y >= 0) && (y < h))
								{
									int o = y * _textureImage.BackBufferStride + x * 4;

									byte *pixelPointer = &backBuffer[o];

									*(int *)pixelPointer = PixelColour;
								}
							}
						}
					}
					finally
					{
						_textureImage.AddDirtyRect(new Int32Rect(0, 0, _textureImage.PixelWidth, _textureImage.PixelHeight));
						_textureImage.Unlock();
					}
				}
			}
		}

		static void Swap<T>(ref T a, ref T b)
		{
			T c = a;
			a = b;
			b = c;
		}

		ModelVisual3D _visual;
		AxisAngleRotation3D _rotation;

		bool _dragging = false;
		Point _dragPosition;
		double _dragStartAngle;

		private void v3dViewport_MouseDown(object sender, MouseButtonEventArgs e)
		{
			_dragging = (e.LeftButton == MouseButtonState.Pressed);

			var clickPosition = Mouse.GetPosition(v3dViewport);

			_dragPosition = new Point(
				clickPosition.X - v3dViewport.ActualWidth * 0.5,
				v3dViewport.ActualHeight * 0.5 - clickPosition.Y);

			_dragStartAngle = _rotation.Angle;

			v3dViewport.CaptureMouse();
		}

		private void v3dViewport_MouseMove(object sender, MouseEventArgs e)
		{
			if (_dragging && (_rotation != null))
			{
				Point mousePosition = Mouse.GetPosition(v3dViewport);

				Point relativeMousePosition = new Point(
					mousePosition.X - v3dViewport.ActualWidth * 0.5,
					mousePosition.Y - v3dViewport.ActualHeight * 0.5);

				double dx = relativeMousePosition.X - _dragPosition.X;
				double dy = relativeMousePosition.Y - _dragPosition.Y;

				var angleDelta = dx;

				_rotation.Angle = _dragStartAngle + angleDelta;

				/*
				double mouseAngle = Math.Atan2(dy, dx);

				double axisAngle = mouseAngle + Math.PI / 2;

				Vector3D axis = new Vector3D(Math.Cos(axisAngle) * 4, Math.Sin(axisAngle) * 4, 0);

				double rotation = 0.01 * Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));

				var group = (Transform3DGroup)_visual.Transform;
				QuaternionRotation3D r = new QuaternionRotation3D(new Quaternion(axis, rotation * 180 / Math.PI));
				group.Children.Add(new RotateTransform3D(r));

				_dragPosition = relativeMousePosition;
				*/
			}
		}

		private void v3dViewport_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_dragging = (e.LeftButton == MouseButtonState.Pressed);
			v3dViewport.ReleaseMouseCapture();
		}
	}
}
