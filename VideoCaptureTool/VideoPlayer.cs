using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Media.Imaging;
using Accord.Video.DirectShow;
using Accord.Video;
using Accord.Video.FFMPEG;
using Accord.DirectSound;
using Accord.Audio;
using System.Text.RegularExpressions;

namespace VideoCaptureTool
{
    class VideoPlayer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private BitmapImage image;
        public BitmapImage VideoFrame
        {
            get { return image; }
            private set
            {
                image = null;
                image = value;
                NotifyPropertyChanged("VideoFrame");
            }
        }

        private bool deviceOpen;

        public bool DeviceOpen
        {
            get { return deviceOpen; }
            set
            {
                deviceOpen = value;
                NotifyPropertyChanged("DeviceOpen");
                NotifyPropertyChanged("DeviceClosed");
            }
        }
        public bool DeviceClosed
        {
            get
            {
                return (deviceOpen == false);
            }
        }
        private bool isClosing = false;
        public bool IsRecording
        {
            get 
            {
                if (isClosing)
                    return false;
                return VideoWriter.IsOpen; 
            }
        }
        

        VideoCaptureDevice VideoSource = null;

        VideoFileWriter VideoWriter = new VideoFileWriter();

        FilterInfoCollection videoDevices = null;

        public ObservableCollection<FilterInfo> VideoDevices = new ObservableCollection<FilterInfo>();


        public VideoPlayer()
        {
            VideoDevices.CollectionChanged += VideoDevices_CollectionChanged;

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo dev in videoDevices)
            {
                VideoDevices.Add(dev);
            }

            /*Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)//opens dialog to select files
            {
                // create video source
                videoSource = new FileVideoSource(openFileDialog.FileName);
            }*/
        }

        void VideoDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("VideoDevices");
            NotifyPropertyChanged("EnableControls");
        }

        public void Start(int VideoIndex)
        {
            VideoSource = new VideoCaptureDevice(VideoDevices[VideoIndex].MonikerString);

            if (VideoSource != null)
            {
                // set NewFrame event handler
                VideoSource.NewFrame += video_NewFrame;
                VideoSource.Start();
                DeviceOpen = true;

            }
        }

        public void Stop()
        {
            if (VideoSource != null && VideoSource.IsRunning)
                VideoSource.Stop();

            StopRecording();

            VideoFrame = null;
            DeviceOpen = false;
        }

        // New frame event handler, which is invoked on each new available video frame
        private void video_NewFrame(object sender, Accord.Video.NewFrameEventArgs eventArgs)
        {
            // get new frame
            try
            {
                Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();

                if (bitmap != null)
                {
                    VideoFrame = BitmapTools.BitmapToImageSource(bitmap);
                    if (IsRecording)
                    {
                        VideoWriter.WriteVideoFrame(bitmap);
                    }
                }
            }
            catch
            {
                Stop();
                throw new Exception("error receiving frame!");
            }
        }
        public void StartRecording(string filePath)
        {
            if (IsRecording == true || String.IsNullOrEmpty(filePath) || VideoFrame == null || VideoSource == null || VideoSource.IsRunning == false)
                return;

            try
            {
                VideoWriter.Open(filePath, (int)Math.Round(VideoFrame.Width), (int)Math.Round(VideoFrame.Height),25,VideoCodec.H264);
            }
            catch
            {
                throw new Exception("Failed to start recording!");
            }
        }
        public void StopRecording()
        {
            if (IsRecording == false)
                return;

            isClosing = true;
            VideoWriter.Close();
            isClosing = false;
        }

        public void SaveFrame(string filePath)
        {
            BitmapImage frameToDump = VideoFrame;

            if (frameToDump == null)
                return;
            try
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));

                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
            catch
            {
                throw new Exception("failed saving file!");
            }
        }
        public void SetFrame(BitmapImage frame)
        {
            if (VideoSource != null && (VideoSource.IsRunning || frame == null))
                return;
            else
            {
                VideoFrame = frame;
            }
        }
        public void OpenDeviceProperties(int index, IntPtr ParentHandle)
        {
            //if the properties window isn't right, project file/.NET versioning/packages got corrupt...again!
            VideoCaptureDevice Cam1;

            if (ParentHandle == null)
                ParentHandle = IntPtr.Zero;

            if(VideoSource == null)
                Cam1 = new VideoCaptureDevice(videoDevices[index].MonikerString);
            else
                Cam1 = VideoSource;

            //Cam1.DisplayCrossbarPropertyPage(ParentHandle);
            Cam1.DisplayPropertyPage(ParentHandle);// IntPtr.Zero); //This will display a form with camera controls
        }
    }
}
