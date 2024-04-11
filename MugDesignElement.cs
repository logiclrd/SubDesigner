using System.Xml.Serialization;

namespace SubDesigner
{
	[XmlInclude(typeof(MugDesignStamp))]
	[XmlInclude(typeof(MugDesignText))]
	public class MugDesignElement
	{
	}
}