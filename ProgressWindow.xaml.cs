using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace SubDesigner
{
	/// <summary>
	/// Interaction logic for ProgressWindow.xaml
	/// </summary>
	public partial class ProgressWindow : Window
	{
		public static ProgressWindow RunOnSeparateThread()
		{
			var threadOutput = new ThreadOutput();

			var thread = new Thread(UIThreadProc);

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start(threadOutput);

			return threadOutput.ReceiveWindowReference();
		}

		class ThreadOutput
		{
			object _sync = new object();
			ProgressWindow? _progressWindow;

			public void PassWindowReference(ProgressWindow window)
			{
				lock (_sync)
				{
					_progressWindow = window;
					Monitor.PulseAll(_sync);
				}
			}

			public ProgressWindow ReceiveWindowReference()
			{
				lock (_sync)
				{
					while (_progressWindow == null)
						Monitor.Wait(_sync);

					return _progressWindow;
				}
			}
		}

		static void UIThreadProc(object? state)
		{
			var threadOutput = (ThreadOutput)state!;

			var window = new ProgressWindow();

			window.Show();

			threadOutput.PassWindowReference(window);

			System.Windows.Threading.Dispatcher.Run();
		}

		public ProgressWindow()
		{
			InitializeComponent();
		}

		bool _closing;

		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = !_closing;

			base.OnClosing(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			Dispatcher.InvokeShutdown();
		}

		int _currentIndex;
		int _maximumIndex;

		public void UpdateProgress(int currentIndex, int maximumIndex)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => UpdateProgress(currentIndex, maximumIndex));
				return;
			}

			if (maximumIndex != _maximumIndex)
			{
				_maximumIndex = maximumIndex;
				rMaximumIndex.Text = _maximumIndex.ToString();
				pbProgress.Maximum = _maximumIndex;
			}

			if (currentIndex != _currentIndex)
			{
				_currentIndex = currentIndex;
				rCurrentIndex.Text = _currentIndex.ToString();
				pbProgress.Value = _currentIndex;
			}

			tiiProgressInTaskBar.ProgressValue = pbProgress.Value / pbProgress.Maximum;
		}

		public void CloseWindow()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => CloseWindow());
				return;
			}

			_closing = true;
			Close();
		}
	}
}
