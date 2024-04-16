using System;
using System.Windows;
using System.Windows.Controls;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for PrintQueued.xaml
	/// </summary>
	public partial class PrintQueued : UserControl
	{
		public PrintQueued()
		{
			InitializeComponent();
		}

		public event EventHandler? Close;

		protected virtual void OnClose()
		{
			Close?.Invoke(this, EventArgs.Empty);
		}

		private void cmdOK_Click(object sender, RoutedEventArgs e)
		{
			OnClose();
		}
	}
}
