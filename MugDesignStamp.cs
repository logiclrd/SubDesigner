namespace SubDesigner
{
	public class MugDesignStamp : MugDesignElement
	{
		public string Descriptor;

		public MugDesignStamp()
		{
			Descriptor = "";
		}

		public MugDesignStamp(string stampDescriptor)
		{
			Descriptor = stampDescriptor;
		}
	}
}
