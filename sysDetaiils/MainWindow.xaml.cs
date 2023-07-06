using System.Windows;
using System.Windows.Controls;

namespace sysDetails
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                if (tabControl.SelectedIndex == 0)
                {
                    contentFrame.Navigate(new SystemDetails());
                }
                else if (tabControl.SelectedIndex == 1)
                {
                    adContentFrame.Navigate(new ADReport());
                }
            }
        }
    }
}