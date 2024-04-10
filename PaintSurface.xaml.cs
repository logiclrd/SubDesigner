using System;
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
		public event EventHandler<UIElement> ItemModified;

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

		public event EventHandler<UIElement> ChangeMade;

		protected virtual void OnChangeMade(UIElement changedItem)
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

		private void manipulator_ChangeMade(object? sender, UIElement changedItem)
		{
			OnChangeMade(changedItem);
		}

		private void manipulator_LostFocus(object sender, RoutedEventArgs e)
		{
			ClearSelection();
		}

		public void AddStamp(Point location, ImageSource stampBitmap, Size initialSize)
		{
			var stamp = new Image();

			stamp.Width = initialSize.Width;
			stamp.Height = initialSize.Height;
			stamp.Source = stampBitmap;

			Canvas.SetLeft(stamp, location.X);
			Canvas.SetTop(stamp, location.Y);

			cnvContents.Children.Add(stamp);

			stamp.MouseDown += element_MouseDown;
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

			_manipulator.ChangeMade += manipulator_ChangeMade;
			_manipulator.LostFocus += manipulator_LostFocus;
		}
	}
}
