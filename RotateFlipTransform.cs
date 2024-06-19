using System;
using System.Windows;
using System.Windows.Media;

namespace SubDesigner
{
	public class RotateFlipTransform
	{
		public double Angle;
		public double CentreX, CentreY;
		public bool IsFlipped;

		public Transform Build()
		{
			var result = new TransformGroup();

			result.Children.Add(new ScaleTransform(scaleX: IsFlipped ? -1 : 1, scaleY: 1, CentreX, CentreY));
			result.Children.Add(new RotateTransform(Angle, CentreX, CentreY));

			return result;
		}

		public void ApplyTo(UIElement element)
		{
			if (element.RenderTransform == null)
				element.RenderTransform = Build();
			else
				ApplyTo(element.RenderTransform);
		}

		public static RotateFlipTransform Parse(Transform transform)
		{
			var ret = new RotateFlipTransform();

			ret.BuildFrom(transform);

			return ret;
		}

		void BuildFrom(Transform transform)
		{
			if (transform is TransformGroup group)
				BuildFrom(group);
			if (transform is RotateTransform rotate)
				BuildFrom(rotate);
			if (transform is ScaleTransform scale)
				BuildFrom(scale);
		}

		void BuildFrom(TransformGroup group)
		{
			foreach (var child in group.Children)
				BuildFrom(child);
		}

		void BuildFrom(RotateTransform rotate)
		{
			Angle = rotate.Angle;
			CentreX = rotate.CenterX;
			CentreY = rotate.CenterY;
		}

		void BuildFrom(ScaleTransform scale)
		{
			IsFlipped = (scale.ScaleX < 0);
		}

		void ApplyTo(Transform transform)
		{
			if (transform is TransformGroup group)
				ApplyTo(group);
			if (transform is RotateTransform rotate)
				ApplyTo(rotate);
			if (transform is ScaleTransform scale)
				ApplyTo(scale);
		}

		void ApplyTo(TransformGroup group)
		{
			foreach (var child in group.Children)
				ApplyTo(child);
		}

		void ApplyTo(RotateTransform rotate)
		{
			rotate.Angle = Angle;
			rotate.CenterX = CentreX;
			rotate.CenterY = CentreY;
		}

		void ApplyTo(ScaleTransform scale)
		{
			scale.ScaleX = (IsFlipped ? -1 : 1);
		}
	}
}
