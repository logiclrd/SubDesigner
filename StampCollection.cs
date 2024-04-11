using System.Collections.Generic;

namespace SubDesigner
{
	public class StampCollection
	{
		public StampCollection(string name)
		{
			Name = name;
		}

		public string Name;
		public List<Stamp> Stamps = new List<Stamp>();
	}
}
