using System.Windows;

namespace Deployer
{
    /// <summary>
    /// Interaction logic for ConfigurationItemSettings.xaml
    /// </summary>
    public partial class ConfigurationItemSettings : Window
    {
        public ConfigurationItemSettings()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
