using System.Windows;
using System.Windows.Controls;

namespace SubDesigner
{
	public class KeepOpenComboBox : ComboBox
	{
		protected override DependencyObject GetContainerForItemOverride()
		{
			return new KeepOpenComboBoxItem(this);
		}
	}
}
