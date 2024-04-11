using System;
using System.Windows;
using System.Windows.Controls;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for StampCollectionPreview.xaml
	/// </summary>
	public partial class StampCollectionPreview : UserControl
	{
		public StampCollectionPreview()
		{
			InitializeComponent();
		}

		public StampCollection? Collection { get; private set; }

		public void LoadCollection(StampCollection collection)
		{
			if (spItems.Children.Count > 1)
				spItems.Children.RemoveRange(1, spItems.Children.Count - 1);

			lblName.Content = collection.Name;

			for (int i = 0; i < 20; i++)
				spItems.Children.Add(new Image() { Source = collection.Stamps[i].BitmapSource, Margin = new Thickness(15) });

			this.Collection = collection;
		}
	}
}
