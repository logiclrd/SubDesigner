using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for PaintSurface.xaml
	/// </summary>
	public partial class PaintSurface : UserControl
	{
		static PaintSurface()
		{
			HasSelectionProperty = DependencyProperty.Register(nameof(HasSelection), typeof(bool), typeof(PaintSurface));
		}

		public readonly static DependencyProperty HasSelectionProperty;

		public PaintSurface()
		{
			InitializeComponent();

			MouseDown +=
				(_, e) =>
				{
					if ((_manipulator != null) && !e.Handled)
						ClearSelection();
				};
		}

		public bool HasSelection
		{
			get => (bool)GetValue(HasSelectionProperty);
			set => SetValue(HasSelectionProperty, value);
		}

		bool _pauseChangeEvents = false;

		public event EventHandler<UIElement>? Open;
		public event EventHandler<UIElement?>? ChangeMade;

		protected virtual void OnOpen(UIElement selectedItem)
		{
			Open?.Invoke(this, selectedItem);
		}

		protected virtual void OnChangeMade(UIElement? changedItem)
		{
			if (!_pauseChangeEvents)
				ChangeMade?.Invoke(this, changedItem);
		}

		ElementManipulator? _manipulator;

		public void ClearSelection()
		{
			if (_manipulator != null)
			{
				_manipulator.LostFocus -= manipulator_LostFocus;
				cnvContents.Children.Remove(_manipulator);
			}

			HasSelection = false;
		}

		private void manipulator_Open(object? sender, UIElement selectedItem)
		{
			OnOpen(selectedItem);
		}

		private void manipulator_ChangeMade(object? sender, UIElement changedItem)
		{
			OnChangeMade(changedItem);
		}

		private void manipulator_Delete(object? sender, EventArgs e)
		{
			if (_manipulator != null)
				DeleteItem(_manipulator.Wrapped!);
		}

		private void manipulator_LostFocus(object sender, RoutedEventArgs e)
		{
			ClearSelection();
		}

		public void AddText(Text text, double angle = 0)
		{
			text.FitContent();

			Point centre = new Point(ActualWidth * 0.5, ActualHeight * 0.5);

			Vector size = (Vector)new Size(text.Width, text.Height);

			AddText(centre - size * 0.5, text, fitted: true, angle);
		}

		public TextHost AddText(Point location, Text text)
		{
			return AddText(location, text, fitted: false, angle: 0);
		}

		private TextHost AddText(Point location, Text text, bool fitted, double angle)
		{
			if (!fitted)
				text.FitContent();

			var textHost = new TextHost() { Text = text };

			AddText(location, textHost, new Size(text.Width, text.Height), angle);

			return textHost;
		}

		private void AddText(Point location, TextHost text, Size size, double angle = 0)
		{
			text.Width = size.Width;
			text.Height = size.Height;

			Canvas.SetLeft(text, location.X);
			Canvas.SetTop(text, location.Y);

			text.RenderTransform =
				new RotateTransform()
				{
					Angle = angle,
					CenterX = size.Width * 0.5,
					CenterY = size.Height * 0.5
				};

			cnvContents.Children.Add(text);

			text.MouseDown += element_MouseDown;
		}

		public void AddStamp(Point location, ImageSource stampBitmap, string stampDescriptor, Size initialSize, double angle = 0)
		{
			var stamp = new Image();

			stamp.Tag = stampDescriptor;

			stamp.Width = initialSize.Width;
			stamp.Height = initialSize.Height;
			stamp.Source = stampBitmap;

			Canvas.SetLeft(stamp, location.X);
			Canvas.SetTop(stamp, location.Y);

			stamp.RenderTransform =
				new RotateTransform()
				{
					Angle = angle,
					CenterX = initialSize.Width * 0.5,
					CenterY = initialSize.Height * 0.5
				};

			cnvContents.Children.Add(stamp);

			stamp.MouseDown += element_MouseDown;
		}

		public void ClearItems()
		{
			ClearSelection();
			cnvContents.Children.Clear();
			OnChangeMade(null);
		}

		public void DeleteItem(UIElement element)
		{
			ClearSelection();
			cnvContents.Children.Remove(element);
			OnChangeMade(element);
		}

		private void element_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (_manipulator != null)
				ClearSelection();

			var element = (FrameworkElement)sender;

			_manipulator = new ElementManipulator();

			_manipulator.Wrap(element);

			cnvContents.Children.Add(_manipulator);
			_manipulator.Focus();

			HasSelection = true;

			e.Handled = true;

			_manipulator.Open += manipulator_Open;
			_manipulator.ChangeMade += manipulator_ChangeMade;
			_manipulator.Delete += manipulator_Delete;
			_manipulator.LostFocus += manipulator_LostFocus;
		}

		public MugDesign Serialize()
		{
			var design = new MugDesign();

			foreach (var element in cnvContents.Children.OfType<FrameworkElement>())
			{
				MugDesignElement serializedElement;

				if ((element is Image image) && (image.Tag is string stampDescriptor))
					serializedElement = new MugDesignStamp(stampDescriptor);
				else if ((element is TextHost textHost) && (textHost.Text is Text text))
					serializedElement = new MugDesignText(text);
				else
					continue;

				serializedElement.X = Canvas.GetLeft(element);
				serializedElement.Y = Canvas.GetTop(element);

				serializedElement.Width = element.Width;
				serializedElement.Height = element.Height;

				if (element.RenderTransform is RotateTransform rotateTransform)
					serializedElement.Angle = rotateTransform.Angle;

				design.Elements.Add(serializedElement);
			}

			return design;
		}

		public void Deserialize(MugDesign design, IEnumerable<StampCollection> loadedStamps)
		{
			var stampByDescriptor = loadedStamps
				.SelectMany(collection => collection.Stamps)
				.ToDictionary(stamp => stamp.Descriptor!, stamp => stamp);

			_pauseChangeEvents = true;

			try
			{
				cnvContents.Children.Clear();

				foreach (var element in design.Elements)
				{
					if (element is MugDesignStamp stamp)
					{
						if (!stampByDescriptor.TryGetValue(stamp.Descriptor, out var loadedStamp))
						{
							BitmapSource bitmap;

							if (!stamp.Descriptor.Contains("::"))
							{
								bitmap = new BitmapImage(new Uri(stamp.Descriptor));

							}
							else
							{
								string[] parts = stamp.Descriptor.Split("::");

								if (parts.Length != 2)
									continue;

								string[] crop = parts[0].Split(":");
								string fileName = parts[1];

								if (crop.Length != 4)
									continue;

								BitmapSource fullBitmap = new BitmapImage(new Uri(fileName));

								if (!int.TryParse(crop[0], out int x))
									continue;
								if (!int.TryParse(crop[1], out int y))
									continue;
								if (!int.TryParse(crop[2], out int w))
									continue;
								if (!int.TryParse(crop[3], out int h))
									continue;

								bitmap = new CroppedBitmap(
									fullBitmap,
									new Int32Rect(x, y, w, h));
							}

							loadedStamp = new Stamp();

							loadedStamp.BitmapSource = bitmap;
							loadedStamp.Descriptor = stamp.Descriptor;
						}

						AddStamp(
							new Point(element.X, element.Y),
							loadedStamp.BitmapSource!,
							loadedStamp.Descriptor!,
							new Size(element.Width, element.Height),
							element.Angle);
					}

					if (element is MugDesignText serializedText)
					{
						var text = serializedText.ToText();

						text.FitContent();

						var textHost = new TextHost();

						textHost.Text = text;

						AddText(
							new Point(element.X, element.Y),
							textHost,
							new Size(element.Width, element.Height),
							element.Angle);
					}
				}
			}
			finally
			{
				_pauseChangeEvents = false;
			}
		}
	}
}
