using System.Windows.Controls;
using System.Windows.Input;

namespace SubDesigner
{
	public class KeepOpenComboBoxItem : ComboBoxItem
	{
		ComboBox _parent;

		public KeepOpenComboBoxItem(ComboBox parent)
		{
			_parent = parent;
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			e.Handled = true;

			var item = _parent.ItemContainerGenerator.ItemFromContainer(this);

			if (item != null)
				_parent.SelectedItem = item;
		}
	}
}
