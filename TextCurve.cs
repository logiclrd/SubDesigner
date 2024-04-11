using System;
using System.Windows;
using System.Windows.Media;

namespace SubDesigner
{
	public class TextCurve
	{
		public Point StartPoint;
		public Point EndPoint;
		public Point? BendPoint;
		public Point? BendPoint2;

		// Modes:
		// - Straight line: BendPoint == BendPoint2 == null
		// - Simple curve: BendPoint != null, BendPoint2 == null
		//   Single control point that it bends toward at both ends
		// - Complex curve: BendPoint != null, BendPoint2 != null
		//   StartPoint and EndPoint each have their own control point setting the direction of the line at that part of the curve

		public static TextCurve FlatLine(double textLength)
		{
			return
				new TextCurve()
				{
					StartPoint = new Point(0, 50),
					EndPoint = new Point(textLength, 50),
				};
		}

		public PathFigure ToPathFigure()
		{
			var figure = new PathFigure();

			figure.StartPoint = StartPoint;

			if (!(BendPoint is Point bendPoint))
				figure.Segments.Add(new LineSegment(EndPoint, isStroked: true));
			else
			{
				figure.Segments.Add(new BezierSegment(
					point1: bendPoint,
					point2: BendPoint2 ?? bendPoint,
					point3: EndPoint,
					isStroked: true));
			}

			return figure;
		}
	}
}
