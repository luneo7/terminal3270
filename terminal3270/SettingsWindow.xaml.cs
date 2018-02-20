using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace terminal3270
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : ModernWindow
	{
		public SettingsWindow()
		{
			InitializeComponent();
            this.Activated += SettingsWindow_Activated;
		}

        void SettingsWindow_Activated(object sender, EventArgs e)
        {
            txtHost.Focus();
        }
        

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			(this.mainGrid.DataContext as TerminalSettings).SaveToSettings(Properties.Settings.Default);
			this.Close();
		}
	}
}
