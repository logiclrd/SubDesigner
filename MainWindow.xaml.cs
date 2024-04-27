using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Xml.Serialization;

using HelixToolkit.Wpf;

using Microsoft.Win32;

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

			_rotation = new AxisAngleRotation3D();
			_rotation.Axis = new Vector3D(0, 1, 0);

			InitializeModel();
			InitializeStamps();

			var hasSelectionDescriptor = DependencyPropertyDescriptor.FromProperty(PaintSurface.HasSelectionProperty, typeof(PaintSurface));

			hasSelectionDescriptor.AddValueChanged(psPaintSurface, (_, _) => UpdateMugPreview());

			int parsedIndex = -1;

			Directory.CreateDirectory(Constants.MugDesignsFolder);

			_mugIndex = 1 + Directory.GetFiles(Constants.MugDesignsFolder, "*.mug.xml")
				.Select(path => Path.GetFileNameWithoutExtension(path))
				.Select(path => Path.GetFileNameWithoutExtension(path))
				.Where(name => int.TryParse(name, out parsedIndex))
				.Select(name => parsedIndex)
				.DefaultIfEmpty(-1)
				.Max();

			rMugNumber.Text = _mugIndex.ToString();
		}

		int _mugIndex;

		void InitializeModel()
		{
			// Import the model files
			var importer = new ModelImporter();

			var model = importer.Load("Mug.obj", Dispatcher);

			// Find the ImageSource that'll let us update the texture at runtime
			_textureImage = FindTexture(model);

			// Basic lighting
			model.Children.Add(new AmbientLight(Color.FromRgb(128, 128, 128)));

			// Set up rotation controls
			var rotationTransform = new RotateTransform3D();

			rotationTransform.Rotation = _rotation;

			var pitchTransform = new RotateTransform3D();

			pitchTransform.Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -25);

			var transformGroup = new Transform3DGroup();

			transformGroup.Children.Add(rotationTransform);
			transformGroup.Children.Add(pitchTransform);

			// Present the model
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

			v3dViewport.Children.Add(_visual);
			v3dViewport.Children.Add(new DefaultLights());

			Dispatcher.BeginInvoke(
				() => UpdateMugPreview(),
				DispatcherPriority.ApplicationIdle);
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

		XmlSerializer _mugSerializer = new XmlSerializer(typeof(MugDesign));

		void UpdateMugPreview()
		{
			if (_textureImage == null)
				return;

			var render = new RenderTargetBitmap(2048, 855, 96, 96, PixelFormats.Pbgra32);

			psPaintSurface.Measure(new Size(2048, 855));
			psPaintSurface.UpdateLayout();

			render.Render(psPaintSurface);

			_textureImage.Lock();

			try
			{
				var encoder = new PngBitmapEncoder();

				encoder.Frames.Add(BitmapFrame.Create(render));
				using (var stream = File.OpenWrite(Path.Combine(Constants.MugDesignsFolder, _mugIndex + ".png")))
					encoder.Save(stream);
			}
			catch { }

			var mugElements = psPaintSurface.Serialize();

			string mugFileName = Path.Combine(Constants.MugDesignsFolder, _mugIndex + ".mug.xml");

			if (File.Exists(mugFileName))
			{
				string backupMugFileName = Path.Combine(
					Constants.MugDesignsFolder,
					Path.GetFileNameWithoutExtension(mugFileName) + "_backup.mug.xml");

				try
				{
					File.Move(mugFileName, backupMugFileName, overwrite: true);
				}
				catch { }
			}

			using (var stream = File.OpenWrite(mugFileName))
				_mugSerializer.Serialize(stream, mugElements);

			try
			{
				render.CopyPixels(
					new Int32Rect(0, 0, 2048, 855),
					_textureImage.BackBuffer,
					_textureImage.BackBufferStride * _textureImage.PixelHeight,
					_textureImage.BackBufferStride);
			}
			finally
			{
				_textureImage.AddDirtyRect(new Int32Rect(0, 0, 2048, 855));
				_textureImage.Unlock();
			}
		}

		void InitializeStamps()
		{
			string stampsFolder = "Stamps";

			if (Directory.Exists(stampsFolder))
				stampsFolder = Path.GetFullPath(stampsFolder);
			else
			{
				stampsFolder = Path.Join(
					Path.GetDirectoryName(typeof(MainWindow).Assembly.Location),
					"Stamps");

				while (!Directory.Exists(stampsFolder))
				{
					string? parentFolder = Path.GetDirectoryName(Path.GetDirectoryName(stampsFolder));

					if (parentFolder == null)
						return;

					stampsFolder = Path.Join(parentFolder, "Stamps");
				}
			}

			string[] stampCollectionFolders = Directory.GetDirectories(stampsFolder);

			foreach (var stampCollectionFolder in stampCollectionFolders)
			{
				string collectionName = Path.GetFileName(stampCollectionFolder);

				string nameFile = Path.Join(stampCollectionFolder, "Name");

				if (File.Exists(nameFile))
					collectionName = new StreamReader(nameFile).ReadLine()!;

				var collection = new StampCollection(collectionName);

				string[] files = Directory.GetFiles(stampCollectionFolder, "*.png", SearchOption.AllDirectories);

				foreach (var file in files)
				{
					BitmapSource bitmap = new BitmapImage(new Uri(file));

					string itemsFile = file + ".items";

					if (!File.Exists(itemsFile))
					{
						var stamp = new Stamp();

						stamp.BitmapSource = bitmap;
						stamp.Descriptor = file;

						collection.Stamps.Add(stamp);
					}
					else
					{
						foreach (string item in File.ReadAllLines(itemsFile))
						{
							string[] parts = item.Split(',');

							if (parts.Length != 4)
								continue;

							if (!int.TryParse(parts[0], out int x))
								continue;
							if (!int.TryParse(parts[1], out int y))
								continue;
							if (!int.TryParse(parts[2], out int w))
								continue;
							if (!int.TryParse(parts[3], out int h))
								continue;

							var croppedBitmap = new CroppedBitmap(
								bitmap,
								new Int32Rect(x, y, w, h));

							var stamp = new Stamp();

							stamp.BitmapSource = croppedBitmap;
							stamp.Descriptor = $"{x}:{y}:{w}:{h}::{file}";

							collection.Stamps.Add(stamp);
						}
					}
				}

				_stampCollections.Add(collection);
			}
		}

		List<StampCollection> _stampCollections = new List<StampCollection>();

		Random _rnd = new Random();
		WriteableBitmap? _textureImage;

		ModelVisual3D? _visual;
		AxisAngleRotation3D _rotation;

		bool _dragging = false;
		IInputElement? _dragSender;
		Point _dragPosition;
		double _dragStartAngle;

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

		private void cmdSelectCollection_Click(object sender, RoutedEventArgs e)
		{
			grdLayout.IsEnabled = false;

			var collectionList = new ListBox();

			ScrollViewer.SetHorizontalScrollBarVisibility(collectionList, ScrollBarVisibility.Hidden);

			foreach (var collection in _stampCollections)
			{
				var preview = new StampCollectionPreview();

				preview.LoadCollection(collection);
				preview.HorizontalAlignment = HorizontalAlignment.Stretch;

				collectionList.Items.Add(preview);
			}

			collectionList.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			collectionList.SelectionChanged +=
				(_, _) =>
				{
					if (collectionList.SelectedItem is StampCollectionPreview selectedCollection)
					{
						grdTopLevel.Children.Remove(collectionList);
						grdLayout.IsEnabled = true;

						LoadStampCollection(selectedCollection.Collection!);
					}
				};

			grdTopLevel.Children.Add(collectionList);
		}

		private void LoadStampCollection(StampCollection collection)
		{
			spStamps.Children.Clear();

			foreach (var stamp in collection.Stamps)
			{
				var spacer = new Control() { Width = 20 };

				var image =
					new Image()
					{
						Source = stamp.BitmapSource,
						Tag = stamp.Descriptor,
						VerticalAlignment = VerticalAlignment.Stretch,
						StretchDirection = StretchDirection.DownOnly,
					};

				spStamps.Children.Add(spacer);
				spStamps.Children.Add(image);

				EnableDrag(image);
			}
		}

		const double MugStartAngle = 70;
		const double MugEndAngle = -235;

		void EnableDrag(Image image)
		{
			Image? dragImage = null;

			Vector clickOffset;

			image.MouseDown +=
				(sender, e) =>
				{
					var clickPosition = e.GetPosition(image);

					clickOffset = new Vector(clickPosition.X, clickPosition.Y);

					var dragCanvas = new Canvas();

					grdTopLevel.Children.Add(dragCanvas);

					dragImage = new Image();
					dragImage.Source = image.Source;
					dragImage.Width = image.ActualWidth;
					dragImage.Height = image.ActualHeight;

					void RepositionDragImage(Point mousePosition)
					{
						Canvas.SetLeft(dragImage, mousePosition.X - clickOffset.X);
						Canvas.SetTop(dragImage, mousePosition.Y - clickOffset.Y);
					}

					RepositionDragImage(e.GetPosition(dragCanvas));

					dragCanvas.Children.Add(dragImage);

					dragCanvas.CaptureMouse();

					dragCanvas.MouseMove +=
						(_, e2) =>
						{
							RepositionDragImage(e2.GetPosition(dragCanvas));

							var mousePosition = e2.GetPosition(psPaintSurface);

							var topLeft = mousePosition - clickOffset;
							var bottomRight = topLeft + new Vector(dragImage.ActualWidth, dragImage.ActualHeight);

							bool offLeftEdge = bottomRight.X < 0;
							bool offTopEdge = bottomRight.Y < 0;
							bool offRightEdge = topLeft.X > psPaintSurface.ActualWidth;
							bool offBottomEdge = topLeft.Y > psPaintSurface.ActualHeight;

							if (!offLeftEdge && !offTopEdge && !offRightEdge && !offBottomEdge)
							{
								var centreX = (topLeft.X + bottomRight.X) * 0.5;

								_rotation.Angle = MugStartAngle + centreX * (MugEndAngle - MugStartAngle) / 2048.0;
							}
						};

					dragCanvas.MouseUp +=
						(_, e2) =>
						{
							dragCanvas.ReleaseMouseCapture();

							var mousePosition = e2.GetPosition(psPaintSurface);

							double scaleFactor = psPaintSurface.ActualWidth / vbPaintSurfaceContainer.ActualWidth;

							var topLeft = mousePosition - scaleFactor * clickOffset;
							var bottomRight = topLeft + scaleFactor * new Vector(dragImage.ActualWidth, dragImage.ActualHeight);

							bool offLeftEdge = bottomRight.X < 0;
							bool offTopEdge = bottomRight.Y < 0;
							bool offRightEdge = topLeft.X > psPaintSurface.ActualWidth;
							bool offBottomEdge = topLeft.Y > psPaintSurface.ActualHeight;

							if (!offLeftEdge && !offTopEdge && !offRightEdge && !offBottomEdge)
							{
								psPaintSurface.AddStamp(topLeft, image.Source, (string)image.Tag, (Size)(bottomRight - topLeft));
								UpdateMugPreview();
							}

							grdTopLevel.Children.Remove(dragCanvas);

							if (dragImage != null)
							{
								grdLayout.Children.Remove(dragImage);
								dragImage = null;
							}
						};
				};
		}

		private void psPaintSurface_Open(object sender, UIElement e)
		{
			if ((e is TextHost textHost) && (textHost.Text is Text text))
			{
				var position = new Point(
					Canvas.GetLeft(e),
					Canvas.GetTop(e));

				var textEditor = new TextEditor();

				textEditor.AddButtonText = "Update Text";

				textEditor.HorizontalAlignment = HorizontalAlignment.Stretch;
				textEditor.VerticalAlignment = VerticalAlignment.Stretch;

				textEditor.LoadText(text);

				grdTopLevel.Children.Add(textEditor);
				grdLayout.IsEnabled = false;

				textEditor.AddText +=
					(_, newText) =>
					{
						psPaintSurface.DeleteItem(textHost);

						var newTextHost = psPaintSurface.AddText(position, newText);

						newTextHost.Width = textHost.Width;
						newTextHost.Height = textHost.Height;
						newTextHost.RenderTransform = textHost.RenderTransform;

						UpdateMugPreview();
					};

				textEditor.Close +=
					(_, _) =>
					{
						grdTopLevel.Children.Remove(textEditor);
						grdLayout.IsEnabled = true;
					};
			}
		}

		private void psPaintSurface_ChangeMade(object sender, UIElement? e)
		{
			if (e is UIElement item)
			{
				var centreX = Canvas.GetLeft(item) + item.RenderSize.Width * 0.5;

				_rotation.Angle = MugStartAngle + centreX * (MugEndAngle - MugStartAngle) / 2048.0;
			}

			UpdateMugPreview();
		}

		private void cmdAddText_Click(object sender, RoutedEventArgs e)
		{
			grdLayout.IsEnabled = false;

			var textEditor = new TextEditor();

			textEditor.HorizontalAlignment = HorizontalAlignment.Stretch;
			textEditor.VerticalAlignment = VerticalAlignment.Stretch;

			grdTopLevel.Children.Add(textEditor);
			grdLayout.IsEnabled = false;

			textEditor.AddText +=
				(_, text) =>
				{
					psPaintSurface.AddText(text);
				};

			textEditor.Close +=
				(_, _) =>
				{
					grdTopLevel.Children.Remove(textEditor);
					grdLayout.IsEnabled = true;
				};
		}

		private void cmdPrintIt_Click(object sender, RoutedEventArgs e)
		{
			var printPreview = new PrintPreview();

			printPreview.CloneViewport3D(v3dViewport);

			printPreview.Visual = psPaintSurface;
			printPreview.MugIndex = _mugIndex;

			grdTopLevel.Children.Add(printPreview);
			grdLayout.IsEnabled = false;

			printPreview.NotifyDisplayed();

			printPreview.Close +=
				(_, proceeded) =>
				{
					grdTopLevel.Children.Remove(printPreview);

					if (proceeded)
					{
						var printQueued = new PrintQueued();

						grdTopLevel.Children.Add(printQueued);

						printQueued.Close +=
							(_, _) =>
							{
								grdTopLevel.Children.Remove(printQueued);
								grdLayout.IsEnabled = true;
							};
					}
					else
					{
						grdLayout.IsEnabled = true;
					}
				};
		}

		private void cmdNewMug_Click(object sender, RoutedEventArgs e)
		{
			StartNewMug();
		}

		void StartNewMug()
		{
			_mugIndex++;
			rMugNumber.Text = _mugIndex.ToString();
			psPaintSurface.ClearItems();
		}

		private void tbMugDesignNumber_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
			{
				var ofdDialog = new OpenFileDialog();

				ofdDialog.InitialDirectory = Constants.MugDesignsFolder;
				ofdDialog.Filter = "Mug Designs (*.mug.xml)|*.png";
				ofdDialog.Multiselect = false;
				ofdDialog.Title = "Select Saved Mug Design";
				ofdDialog.DefaultExt = ".png";

				bool? result = ofdDialog.ShowDialog();

				if (result ?? false)
				{
					StartNewMug();

					MugDesign design;

					try
					{
						string actualFileName = ofdDialog.FileName.Replace(".png", ".mug.xml");

						using (var stream = File.OpenRead(actualFileName))
							design = (MugDesign)_mugSerializer.Deserialize(stream!)!;
					}
					catch (Exception ex)
					{
						MessageBox.Show("Couldn't load the mug design. The file might be corrupt.\n\n" + ex.GetType().Name + ": " + ex.Message, "Error Loading Mug Design", MessageBoxButton.OK, MessageBoxImage.Error);

						return;
					}

					try
					{
						psPaintSurface.Deserialize(design!, _stampCollections);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Couldn't load the mug design. The Paint Surface encountered an error processing the design.\n\n" + ex.GetType().Name + ": " + ex.Message, "Error Loading Mug Design", MessageBoxButton.OK, MessageBoxImage.Error);

						return;
					}

					UpdateMugPreview();
				}
			}
		}
	}
}
