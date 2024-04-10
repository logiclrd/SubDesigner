using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace SubDesigner
{
	public class StampCollection
	{
		public StampCollection(string name)
		{
			Name = name;
		}
			
		public string Name;
		public List<BitmapSource> Stamps = new List<BitmapSource>();
	}
}
