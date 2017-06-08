using System;
using System.Windows;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using NAudio.Wave;

namespace VideoCaptureTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool DevicesOpen 
        {
            get
            {
                if(
                    (videoPlayer != null && videoPlayer.DeviceOpen == true) &&
                    (audioPlayer != null && audioPlayer.DeviceOpen == true)
                    )
                {
                    return true;
                }
                return false;
            }
        }
        public bool DevicesClosed
        {
            get
            {
                if (
                    (videoPlayer != null && videoPlayer.DeviceOpen == false) &&
                    (audioPlayer != null && audioPlayer.DeviceOpen == false)
                    )
                {
                    return true;
                }
                return false;
            }
        }
        public bool EnableControls
        {
            get
            {
                if (ListDevices.Items.Count > 0)
                    return true;
                else
                    return false;
            }
        }

        public bool KeepAspectRatio
        {
            get
            {
                if (frameWindow.Stretch != System.Windows.Media.Stretch.None && frameWindow.Stretch != System.Windows.Media.Stretch.Uniform)
                    return false;
                return true;
            }
            set
            {
                if (value == true)
                {
                    frameWindow.Stretch = System.Windows.Media.Stretch.Uniform;
                }
                else
                    frameWindow.Stretch = System.Windows.Media.Stretch.Fill;
            }
        }

        int framesDrawn = 0;
        public string Frames
        {
            get
            {
                return framesDrawn.ToString();
            }
        }

        public string FrameResolution
        {
            get
            {
                if (videoPlayer.VideoFrame == null)
                    return "";
                return String.Format("{0}x{1}", Math.Round(videoPlayer.VideoFrame.Width), Math.Round(videoPlayer.VideoFrame.Height));
            }
        }
        public string AudioFormat
        {
            get
            {
                if (audioPlayer == null || audioPlayer.DeviceOpen == false)
                {
                    return "--";
                }

                string ret = "unknown";
                WaveFormat format = audioPlayer.GetWaveFormat();

                if (format != null)
                {
                    ret = String.Format("{0}hz @ {1} - {2} channels", format.SampleRate, format.BitsPerSample, format.Channels);
                }

                return ret;
            }
        }

        DispatcherTimer fpsTimer = new DispatcherTimer();
        VideoPlayer videoPlayer = new VideoPlayer();
        AudioPlayer audioPlayer = new AudioPlayer();

        public MainWindow()
        {
            InitializeComponent();

            ListDevices.ItemsSource = videoPlayer.VideoDevices;
            if (ListDevices.SelectedIndex == -1 && ListDevices.Items.Count > 0)
                ListDevices.SelectedIndex = 0;

            ListAudioDevices.ItemsSource = audioPlayer.AudioDevices;
            if (ListAudioDevices.SelectedIndex == -1 && ListAudioDevices.Items.Count > 0)
                ListAudioDevices.SelectedIndex = 0;

            mainGrid.DataContext = videoPlayer;
            dockpanel.DataContext = this;
            grdControls.DataContext = this;
            Controls.DataContext = videoPlayer;
            slVolume.DataContext = audioPlayer;
            Menubar.DataContext = this;


            fpsTimer.Tick += fpsTimer_Tick;
            fpsTimer.Interval = new TimeSpan(0, 0, 1);

            videoPlayer.PropertyChanged += videoPlayer_PropertyChanged;
        }

        private void OpenDevices()
        {
            audioPlayer.Start(ListAudioDevices.SelectedIndex, (AudioDevice)ListAudioDevices.SelectedItem);
            videoPlayer.Start(ListDevices.SelectedIndex);
            fpsTimer.Start();
            NotifyPropertyChanged("AudioFormat");
        }
        private void CloseDevices()
        {
            audioPlayer.Stop();
            videoPlayer.Stop();
            fpsTimer.Stop();
            NotifyPropertyChanged("AudioFormat");
        }
        private void ExitApplication()
        {
            CloseDevices();
            Application.Current.Shutdown(0);
        }

        void videoPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VideoFrame")
            {
                framesDrawn++;
                NotifyPropertyChanged("FrameResolution");
            }
            if (e.PropertyName == "DeviceOpen" || e.PropertyName == "DeviceClosed")
            {
                NotifyPropertyChanged("DevicesOpen");
                NotifyPropertyChanged("DevicesClosed");
            }
        }

        void fpsTimer_Tick(object sender, EventArgs e)
        {
            NotifyPropertyChanged("Frames");
            framesDrawn = 0;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            fpsTimer.Stop();
            if(videoPlayer.DeviceOpen)
                videoPlayer.Stop();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Bitmap frame = (Bitmap)System.Drawing.Image.FromFile(@"./images/lawl.bmp",true);

            videoPlayer.SetFrame(BitmapTools.BitmapToImageSource(frame));

            if (videoPlayer.VideoFrame == null)
                MessageBox.Show("failed loading image");
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            videoPlayer.OpenDeviceProperties(ListDevices.SelectedIndex, new WindowInteropHelper(this).Handle);
        }
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (videoPlayer.DeviceClosed)
                return;
            try
            {
                string filename = String.Format("image_{0}.bmp", DateTime.Now.ToString("dd-MM-yyyy-mm-ss"));
                videoPlayer.SaveFrame(filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failure saving Frame : " + ex.Message);
            }
        }

        private void btnCloseDevice_Click(object sender, RoutedEventArgs e)
        {
            CloseDevices();
        }
        private void btnOpenDevice_Click(object sender, RoutedEventArgs e)
        {
            OpenDevices();
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as ToggleButton).IsChecked == true)
            {
                string filename = String.Format("video_{0}.mkv", DateTime.Now.ToString("dd-MM-yyyy-mm-ss"));
                videoPlayer.StartRecording(filename);
            }
            else
            {
                videoPlayer.StopRecording();
            }
        }

        private void OpenDevice_Click(object sender, RoutedEventArgs e)
        {
            OpenDevices();
        }
        private void CloseDevice_Click(object sender, RoutedEventArgs e)
        {
            CloseDevices();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }
    }
}
