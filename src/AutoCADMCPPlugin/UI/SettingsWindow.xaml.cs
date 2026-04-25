using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace autocad_mcp_plugin.UI
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            var chrome = new WindowChrome
            {
                CornerRadius = new CornerRadius(10),
                CaptionHeight = 44,
                ResizeBorderThickness = new Thickness(6)
            };
            WindowChrome.SetWindowChrome(this, chrome);
            ContentFrame.Navigate(new CommandSetSettingsPage());
            Loaded += OnLoaded;
            RootBorder.SizeChanged += (s, _) => UpdateClip();
        }

        private void UpdateClip()
        {
            if (RootBorder.ActualWidth <= 0 || RootBorder.ActualHeight <= 0) return;
            RootBorder.Clip = new System.Windows.Media.RectangleGeometry(
                new Rect(0, 0, RootBorder.ActualWidth, RootBorder.ActualHeight), 10, 10);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateClip();
            try
            {
                // deepbim-logo-56 for title bar icon and window taskbar icon
                var logoUri = new System.Uri("pack://application:,,,/AutoCADMCPPlugin;component/Resources/deepbim-logo-56.png", System.UriKind.Absolute);
                var logoFrame = BitmapFrame.Create(logoUri);
                TitleBarIcon.Source = logoFrame;
                Icon = logoFrame;
            }
            catch { /* icon optional */ }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
