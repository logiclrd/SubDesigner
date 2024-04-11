using System.Windows.Controls;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for TextHost.xaml
	/// </summary>
	public partial class TextHost : UserControl
	{
		public TextHost()
		{
			InitializeComponent();
		}

		public Text? Text
		{
			get { return ccContent.Content as Text; }
			set { ccContent.Content = value; }
		}
	}
}
