using System.Xml.Serialization;

namespace SubDesigner
{
	[XmlInclude(typeof(MugDesignStamp))]
	[XmlInclude(typeof(MugDesignText))]
	public class MugDesignElement
	{
		public double X;
		public double Y;
		public double Width;
		public double Height;
		public double Angle;
		public bool IsFlipped;
	}
}