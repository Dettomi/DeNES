using DeNES_ClassLibrary;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeNES_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DeNES deNES;
        public MainWindow()
        {
            InitializeComponent();
            deNES = new DeNES();
        }

        private void File_open(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open NES ROM",
                Filter = "NES ROM files (*.nes)|*.nes|All files (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };
            bool? result = dialog.ShowDialog();

            if (result == true) {
                try
                {
                    string filePath = dialog.FileName;
                    deNES.Load(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load ROM:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}