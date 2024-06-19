using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

		public event EventHandler<UIElement>? Open;
		public event EventHandler<UIElement>? ChangeMade;
		public event EventHandler? Raise;
		public event EventHandler? Lower;
		public event EventHandler? Delete;

		protected virtual void OnOpen(UIElement? selectedItem)
		{
			if (selectedItem != null)
				Open?.Invoke(this, selectedItem);
		}

		protected virtual void OnChangeMade(UIElement? changedItem)
		{
			ChangeMade?.Invoke(this, changedItem!);
		}

		protected virtual void OnRaise()
		{
			Raise?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnLower()
		{
			Lower?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnDelete()
		{
			Delete?.Invoke(this, EventArgs.Empty);
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

				if (Wrapped.RenderTransform is RotateTransform rotateTransform)
				{
					rotateTransform.CenterX = Wrapped.Width * 0.5;
					rotateTransform.CenterY = Wrapped.Height * 0.5;
				}
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

			this.RenderTransform = toWrap.RenderTransform;

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
		Point _movePositionAtStart;
		DateTime _lastMoveStarted;

		static readonly TimeSpan MinimumMoveTime = TimeSpan.FromSeconds(0.25);
		static readonly TimeSpan DoubleClickThresholdTime = TimeSpan.FromSeconds(0.3);

		private void StartMove(object sender, MouseButtonEventArgs e)
		{
			if ((e.ChangedButton == MouseButton.Left) && (sender == ElementArea) && (Parent is UIElement parentElement))
			{
				var timeElapsed = DateTime.UtcNow - _lastMoveStarted;

				if (timeElapsed < DoubleClickThresholdTime)
					OnOpen(Wrapped);
				else
				{
					ElementArea.CaptureMouse();

					_moving = true;

					_lastMoveStarted = DateTime.UtcNow;

					_moveDragStart = e.GetPosition(parentElement);
					_movePositionAtStart = new Point(
						Canvas.GetLeft(this),
						Canvas.GetTop(this));
				}

				e.Handled = true;
			}
		}

		private void DoMove(object sender, MouseEventArgs e)
		{
			if (_moving && (Parent is UIElement parentElement))
			{
				var mousePosition = e.GetPosition(parentElement);

				double dx = mousePosition.X - _moveDragStart.X;
				double dy = mousePosition.Y - _moveDragStart.Y;

				Canvas.SetLeft(this, _movePositionAtStart.X + dx);
				Canvas.SetTop(this, _movePositionAtStart.Y + dy);

				OnChangeMade(Wrapped);
			}
		}

		private void EndMove(object sender, MouseButtonEventArgs e)
		{
			if (_moving && (e.ChangedButton == MouseButton.Left))
			{
				var timeElapsed = DateTime.UtcNow - _lastMoveStarted;

				if (timeElapsed < MinimumMoveTime)
				{
					// Undo accidental moves
					Canvas.SetLeft(this, _movePositionAtStart.X);
					Canvas.SetTop(this, _movePositionAtStart.Y);
				}

				ElementArea.ReleaseMouseCapture();
				_moving = false;
			}
		}

		bool _rotating;
		double _rotateStartAngle;
		double _rotateStartX;

		private void StartRotate(object sender, MouseButtonEventArgs e)
		{
			if ((e.ChangedButton == MouseButton.Left) && (sender == pRotateWidget) && (Parent is UIElement parentElement))
			{
				pRotateWidget.CaptureMouse();

				_rotating = true;

				e.Handled = true;

				var clickPoint = Mouse.GetPosition(parentElement);

				_rotateStartX = clickPoint.X;

				if (this.RenderTransform is RotateTransform rotateTransform)
					_rotateStartAngle = rotateTransform.Angle;
			}
		}

		private void DoRotate(object sender, MouseEventArgs e)
		{
			if (_rotating && (Parent is UIElement parentElement))
			{
				var mousePosition = Mouse.GetPosition(parentElement);

				double da = mousePosition.X - _rotateStartX;

				if (this.RenderTransform is RotateTransform rotateTransform)
					rotateTransform.Angle = _rotateStartAngle + da;

				OnChangeMade(Wrapped);
			}
		}

		private void EndRotate(object sender, MouseButtonEventArgs e)
		{
			if (_rotating && (e.ChangedButton == MouseButton.Left))
			{
				pRotateWidget.ReleaseMouseCapture();
				_rotating = false;
			}
		}

		private void cmdRaise_Click(object sender, RoutedEventArgs e)
		{
			OnRaise();
		}

		private void cmdLower_Click(object sender, RoutedEventArgs e)
		{
			OnLower();
		}

		private void cmdDelete_Click(object sender, RoutedEventArgs e)
		{
			OnDelete();
		}
	}
}
