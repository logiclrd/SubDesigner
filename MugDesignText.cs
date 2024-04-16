using System.Windows.Media;

namespace SubDesigner
{
	public class MugDesignText : MugDesignElement
	{
		public double ContentWidth;
		public double ContentHeight;
		public string? FontFamily;
		public double FontSize;
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
			ContentWidth = text.Width;
			ContentHeight = text.Height;
			FontFamily = text.FontFamily.Source;
			FontSize = text.FontSize;
			String = text.String ?? "";
			Bold = text.Bold;
			Italic = text.Italic;
			Underline = text.Underline;
			FillColour = MakeHexString(text.Fill);
			OutlineColour = MakeHexString(text.Outline);
			Curve = text.Curve;
		}

		public Text ToText()
		{
			var text =
				new Text()
				{
					FontFamily = new FontFamily(this.FontFamily),
					FontSize = this.FontSize,
					Width = this.Width,
					Height = this.Height,
					String = this.String,
					Bold = this.Bold,
					Italic = this.Italic,
					Underline = this.Underline,
					Fill = ParseHexString(this.FillColour),
					Outline = ParseHexString(this.OutlineColour),
					Curve = this.Curve,
				};

			text.RestoreSize(this.ContentWidth, this.ContentHeight);

			return text;
		}

		static string MakeHexString(Color colour)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}",
				colour.A,
				colour.R,
				colour.G,
				colour.B);
		}

		static Color ParseHexString(string? colour)
		{
			if (colour == null)
				return Colors.Transparent;

			if ((colour[0] != '#') || (colour.Length != 9))
				return Colors.Black;

			const string HexChars = "0123456789ABCDEF";

			int a = (HexChars.IndexOf(colour[1]) << 4) | HexChars.IndexOf(colour[2]);
			int r = (HexChars.IndexOf(colour[3]) << 4) | HexChars.IndexOf(colour[4]);
			int g = (HexChars.IndexOf(colour[5]) << 4) | HexChars.IndexOf(colour[6]);
			int b = (HexChars.IndexOf(colour[7]) << 4) | HexChars.IndexOf(colour[8]);

			unchecked
			{
				return Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
			}
		}
	}
}