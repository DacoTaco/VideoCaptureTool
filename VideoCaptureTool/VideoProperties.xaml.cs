using Accord.Video.DirectShow;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace VideoCaptureTool
{
    /// <summary>
    /// Interaction logic for VideoProperties.xaml
    /// </summary>
    public partial class VideoProperties : Window
    {
        protected VideoPlayer VideoPlayer { get; set; }

        public VideoProperties(VideoPlayer player) => _Initialise(player);  

        private void _Initialise(VideoPlayer player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            VideoPlayer = player;

            InitializeComponent();
            cbVideoModes.ItemsSource = VideoPlayer.VideoCapabilities;
            cbVideoModes.SelectedItem = VideoPlayer.VideoMode ?? null;
        }

        private void cbVideoModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoPlayer.VideoMode = (VideoCapabilities)cbVideoModes.SelectedItem;
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void bDeviceProperties_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.OpenDeviceProperties(new WindowInteropHelper(this).Handle);
        }
    }
}
