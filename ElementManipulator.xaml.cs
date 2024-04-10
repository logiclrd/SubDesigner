using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for ElementManipulator.xaml
	/// </summary>
	public partial class ElementManipulator : UserControl
	{
		public ElementManipulator()
		{
			InitializeComponent();

			var leftDescriptor = DependencyPropertyDescriptor.FromProperty(Canvas.LeftProperty, typeof(ElementManipulator));
			var topDescriptor = DependencyPropertyDescriptor.FromProperty(Canvas.TopProperty, typeof(ElementManipulator));

			leftDescriptor.AddValueChanged(this, manipulator_PositionChanged);
			topDescriptor.AddValueChanged(this, manipulator_PositionChanged);

			SizeChanged += manipulator_SizeChanged;
		}

		public event EventHandler<UIElement> ChangeMade;

		protected virtual void OnChangeMade(UIElement changedItem)
		{
			ChangeMade?.Invoke(this, changedItem);
		}

		private void manipulator_PositionChanged(object? sender, EventArgs e)
		{
			if (Wrapped != null)
			{
				Canvas.SetLeft(Wrapped, Canvas.GetLeft(this) + BorderSize);
				Canvas.SetTop(Wrapped, Canvas.GetTop(this) + BorderSize);
			}
		}

		private void manipulator_SizeChanged(object? sender, EventArgs e)
		{
			if (Wrapped != null)
			{
				Wrapped.Width = this.ActualWidth - 2 * BorderSize;
				Wrapped.Height = this.ActualHeight - 2 * BorderSize;
			}
		}

		public FrameworkElement? Wrapped;

		const double BorderSize = 6;
		const double CornerSize = 26;

		public void Wrap(FrameworkElement toWrap)
		{
			Wrapped = null;

			Canvas.SetLeft(this, Canvas.GetLeft(toWrap) - BorderSize);
			Canvas.SetTop(this, Canvas.GetTop(toWrap) - BorderSize);

			this.Width = toWrap.ActualWidth + 2 * BorderSize;
			this.Height = toWrap.ActualHeight + 2 * BorderSize;

			Wrapped = toWrap;
		}

		bool _resizing;
		bool _resizingTop;
		bool _resizingLeft;
		Point _resizeDragStart;

		private void StartResize(object sender, MouseButtonEventArgs e)
		{
			if ((sender is UIElement resizeHandle) && (e.ChangedButton == MouseButton.Left))
			{
				resizeHandle.CaptureMouse();

				_resizingTop = Grid.GetRow(resizeHandle) < 2;
				_resizingLeft = Grid.GetColumn(resizeHandle) < 2;

				_resizing = true;

				_resizeDragStart = e.GetPosition(resizeHandle);

				e.Handled = true;
			}
		}

		private void DoResize(object sender, MouseEventArgs e)
		{
			if ((sender is UIElement resizeHandle) && _resizing)
			{
				var mousePosition = e.GetPosition(resizeHandle);

				double dx = mousePosition.X - _resizeDragStart.X;
				double dy = mousePosition.Y - _resizeDragStart.Y;

				if (_resizingLeft)
					dx = -dx;
				if (_resizingTop)
					dy = -dy;

				var newSizeByWidth = new Vector(
					this.Width + dx,
					this.Height + dx * this.Height / this.Width);

				var newSizeByHeight = new Vector(
					this.Width + dy * this.Width / this.Height,
					this.Height + dy);

				if ((newSizeByWidth.X < 0) || (newSizeByWidth.Y < 0))
					newSizeByWidth.X = newSizeByWidth.Y = double.MaxValue;
				if ((newSizeByHeight.X < 0) || (newSizeByHeight.Y < 0))
					newSizeByHeight.X = newSizeByHeight.Y = double.MaxValue;

				var newSize =
					(newSizeByWidth.X < newSizeByHeight.X)
					? newSizeByWidth
					: newSizeByHeight;

				if ((newSize.X < 2 * CornerSize) || (newSize.Y < 2 * CornerSize))
				{
					if (this.Width < this.Height)
					{
						newSize.X = 2 * CornerSize;
						newSize.Y = newSize.X * this.Height / this.Width;
					}
					else
					{
						newSize.Y = 2 * CornerSize;
						newSize.X = newSize.Y * this.Width / this.Height;
					}
				}

				if ((newSize.X < 100000) && (newSize.Y < 100000))
				{
					if (_resizingLeft)
						Canvas.SetLeft(this, Canvas.GetLeft(this) + this.Width - newSize.X);
					if (_resizingTop)
						Canvas.SetTop(this, Canvas.GetTop(this) + this.Height - newSize.Y);

					this.Width = newSize.X;
					this.Height = newSize.Y;

					OnChangeMade(Wrapped);
				}
			}
		}

		private void EndResize(object sender, MouseButtonEventArgs e)
		{
			if (_resizing && (sender is UIElement resizeHandle) && (e.ChangedButton == MouseButton.Left))
			{
				resizeHandle.ReleaseMouseCapture();
				_resizing = false;
			}
		}

		bool _moving;
		Point _moveDragStart;

		private void StartMove(object sender, MouseButtonEventArgs e)
		{
			if ((e.ChangedButton == MouseButton.Left) && (sender == ElementArea))
			{
				ElementArea.CaptureMouse();

				_moving = true;

				e.Handled = true;

				_moveDragStart = e.GetPosition(ElementArea);
			}
		}

		private void DoMove(object sender, MouseEventArgs e)
		{
			if (_moving)
			{
				var mousePosition = e.GetPosition(ElementArea);

				double dx = mousePosition.X - _moveDragStart.X;
				double dy = mousePosition.Y - _moveDragStart.Y;

				Canvas.SetLeft(this, Canvas.GetLeft(this) + dx);
				Canvas.SetTop(this, Canvas.GetTop(this) + dy);

				OnChangeMade(Wrapped);
			}
		}

		private void EndMove(object sender, MouseButtonEventArgs e)
		{
			if (_moving && (e.ChangedButton == MouseButton.Left))
			{
				ElementArea.ReleaseMouseCapture();
				_moving = false;
			}
		}
	}
}
