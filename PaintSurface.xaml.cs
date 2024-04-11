using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

using HelixToolkit.Wpf;

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

		public event EventHandler<UIElement>? Open;
		public event EventHandler<UIElement?>? ChangeMade;

		protected virtual void OnOpen(UIElement selectedItem)
		{
			Open?.Invoke(this, selectedItem);
		}

		protected virtual void OnChangeMade(UIElement? changedItem)
		{
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

		public void AddText(Text text)
		{
			text.FitContent();

			Point centre = new Point(ActualWidth * 0.5, ActualHeight * 0.5);

			Vector size = (Vector)new Size(text.Width, text.Height);

			AddText(centre - size * 0.5, text, fitted: true);
		}

		public TextHost AddText(Point location, Text text)
		{
			return AddText(location, text, fitted: false);
		}

		private TextHost AddText(Point location, Text text, bool fitted)
		{
			if (!fitted)
				text.FitContent();

			var textHost = new TextHost() { Text = text };

			AddText(location, textHost, new Size(text.Width, text.Height));

			return textHost;
		}

		private void AddText(Point location, TextHost text, Size size)
		{
			text.Width = size.Width;
			text.Height = size.Height;

			Canvas.SetLeft(text, location.X);
			Canvas.SetTop(text, location.Y);

			text.RenderTransform = new RotateTransform() { CenterX = size.Width * 0.5, CenterY = size.Height * 0.5 };

			cnvContents.Children.Add(text);

			text.MouseDown += element_MouseDown;
		}

		public void AddStamp(Point location, ImageSource stampBitmap, string stampDescriptor, Size initialSize)
		{
			var stamp = new Image();

			stamp.Tag = stampDescriptor;

			stamp.Width = initialSize.Width;
			stamp.Height = initialSize.Height;
			stamp.Source = stampBitmap;

			Canvas.SetLeft(stamp, location.X);
			Canvas.SetTop(stamp, location.Y);

			stamp.RenderTransform = new RotateTransform() { CenterX = initialSize.Width * 0.5, CenterY = initialSize.Height * 0.5 };

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

			foreach (var element in cnvContents.Children)
			{
				if ((element is Image image) && (image.Tag is string stampDescriptor))
					design.Elements.Add(new MugDesignStamp(stampDescriptor));
				else if ((element is TextHost textHost) && (textHost.Text is Text text))
					design.Elements.Add(new MugDesignText(text));
			}

			return design;
		}
	}
}
