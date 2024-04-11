using System.Windows.Media;

namespace SubDesigner
{
	public class MugDesignText : MugDesignElement
	{
		public string? String;
		public bool Bold;
		public bool Italic;
		public bool Underline;
		public string? FillColour;
		public string? OutlineColour;
		public TextCurve? Curve;

		public MugDesignText()
		{
		}

		public MugDesignText(Text text)
		{
			String = text.String ?? "";
			Bold = text.Bold;
			Italic = text.Italic;
			Underline = text.Underline;
			FillColour = MakeHexString(text.Fill);
			OutlineColour = MakeHexString(text.Outline);
			Curve = text.Curve;
		}

		static string MakeHexString(Color colour)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}{3:X3}",
				colour.A,
				colour.R,
				colour.G,
				colour.B);
		}
	}
}