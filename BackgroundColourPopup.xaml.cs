using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for BackgroundColourPopup.xaml
	/// </summary>
	public partial class BackgroundColourPopup : UserControl
	{
		public BackgroundColourPopup()
		{
			InitializeComponent();
		}

		public Color SelectedColour;
		public event EventHandler? SelectedColourChanged;
		public event EventHandler? Close;

		public void SetSelectedColour(Color selectedColour)
		{
			SelectedColour = selectedColour;
			cpColour.HighlightColour(selectedColour);
		}

		private void cpColour_ColourSelected(object sender, EventArgs e)
		{
			cpColour.HighlightColour(cpColour.SelectedColour);
		}

		private void cmdSet_Click(object sender, RoutedEventArgs e)
		{
			SelectedColour = cpColour.SelectedColour;
			SelectedColourChanged?.Invoke(this, EventArgs.Empty);
			Close?.Invoke(this, EventArgs.Empty);
		}

		private void cmdCancel_Click(object sender, RoutedEventArgs e)
		{
			Close?.Invoke(this, EventArgs.Empty);
		}
	}
}
