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
using System.Windows.Threading;

namespace DeNES_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DeNES deNES;
        DispatcherTimer timer;
        WriteableBitmap bitmap;
        const int sc_Width = 256;
        const int sc_Height = 240;

        public MainWindow()
        {
            InitializeComponent();
            deNES = new DeNES();

            //TIMER 60 FPS:
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMicroseconds(16);
            timer.Tick += nesTick;

            //SCREEN:
            bitmap = new WriteableBitmap
            (
                sc_Width,
                sc_Height,
                96,96,
                PixelFormats.Bgr32,
                null
            );

            gameWindow.Source = bitmap;
        }

        private void nesTick(object sender, EventArgs e)
        {
            deNES.Tick();
            
            cycleBox.Text = deNES.Cycle.ToString();

            //Screen:
            byte[] framebuffer = new byte[sc_Width * sc_Height * 4];
            for (int i = 0; i < framebuffer.Length; i+=4)
            {
                framebuffer[i] = 0; //Blue
                framebuffer[i + 1] = 0; //Green
                framebuffer[i+2] = 0; // Red
                framebuffer[i + 3] = 255; //Alpha (Ignored)
            }
            bitmap.WritePixels(new Int32Rect(0, 0, sc_Width, sc_Height), framebuffer, sc_Width * 4, 0);
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
                    timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load ROM:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}