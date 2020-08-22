using Accord.Video.DirectShow;
using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Media.Imaging;

namespace VideoCaptureTool
{
    public class VideoPlayer : INotifyPropertyChanged
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
                image = value;
                NotifyPropertyChanged("VideoFrame");
            }
        }

        private bool deviceOpen;
        public bool ResizeFrame;
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
        public IList<VideoCapabilities> VideoCapabilities => VideoSource?.VideoCapabilities?.OfType<VideoCapabilities>().ToList() ?? new List<VideoCapabilities>();
        public VideoCapabilities VideoMode
        {
            get => VideoSource?.VideoResolution;
            set
            {

                if (VideoSource == null)
                    return;

                VideoSource.VideoResolution = value;
                if (VideoSource.IsRunning)
                {
                    VideoSource.SignalToStop();
                    VideoSource.WaitForStop();
                    VideoSource.Start();
                }
            }
        }

        VideoFileWriter VideoWriter = new VideoFileWriter();

        FilterInfoCollection videoDevices = null;

        public ObservableCollection<FilterInfo> VideoDevices;

        public VideoPlayer()
        {
            DetectDevices();
            /*Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)//opens dialog to select files
            {
                // create video source
                videoSource = new FileVideoSource(openFileDialog.FileName);
            }*/
        }

        public void DetectDevices()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            VideoDevices = new ObservableCollection<FilterInfo>();
            VideoDevices.CollectionChanged += VideoDevices_CollectionChanged;

            foreach (FilterInfo dev in videoDevices)
            {
                VideoDevices.Add(dev);
            }
        }

        void VideoDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("VideoDevices");
            NotifyPropertyChanged("EnableControls");
        }

        public void Start(int VideoIndex, VideoCapabilities videoMode = null)
        {
            VideoSource = new VideoCaptureDevice(VideoDevices[VideoIndex].MonikerString);
            if (VideoSource != null)
            {
                // set NewFrame event handler             
                VideoSource.ProvideSnapshots = false;
                VideoSource.DesiredAverageTimePerFrame = 1;
                VideoSource.NewFrame += video_NewFrame;
                VideoSource.VideoSourceError += video_VideoSourceError;

                if (videoMode == null)
                    videoMode = VideoSource.VideoCapabilities.OfType<VideoCapabilities>().FirstOrDefault();

                if (videoMode != null)
                    VideoSource.VideoResolution = videoMode;

                VideoSource.Start();
                DeviceOpen = true;
            }
        }

        private void video_VideoSourceError(object sender, Accord.Video.VideoSourceErrorEventArgs eventArgs)
        {
            //Do Nothing
            Console.WriteLine($"error : {eventArgs.Description}");
        }

        public void Stop()
        {
            if (VideoSource != null && VideoSource.IsRunning)
                VideoSource.SignalToStop();

            VideoSource.WaitForStop();

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
                Bitmap bitmap = (Bitmap)eventArgs.Frame;//.Clone();
                if (ResizeFrame)
                {
                    try
                    {
                        //TODO : check if we actually get better image, and else just remove this
                        //if we DO get better image... find a faster method!
                        float height = (float)bitmap.Height * 1.5F;
                        float width = (float)bitmap.Width * 1.5F;

                        Bitmap oldImage = bitmap;
                        var destRect = new Rectangle(0, 0, (int)width, (int)height);
                        Bitmap bmp = new Bitmap((int)width, (int)height);

                        bmp.SetResolution(oldImage.HorizontalResolution, oldImage.VerticalResolution);

                        using (var graphics = Graphics.FromImage(bmp))
                        {
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            using (var wrapMode = new ImageAttributes())
                            {
                                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                                graphics.DrawImage(oldImage, destRect, 0, 0, oldImage.Width, oldImage.Height, GraphicsUnit.Pixel, wrapMode);
                            }
                        }

                        bitmap = bmp;
                    }
                    catch (Exception ex)
                    {
                        bitmap = (Bitmap)eventArgs.Frame;
                    }
                }

                if (bitmap != null)
                {
                    VideoFrame = BitmapTools.BitmapToImageSource(bitmap);
                    if (IsRecording)
                    {
                        VideoWriter.WriteVideoFrame(bitmap);
                    }
                }
            }
            catch(Exception ex)
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
                /*VideoWriter.FrameRate = 30;
                VideoWriter.VideoCodec = VideoCodec.H264;
                VideoWriter.Height = (int)Math.Round(VideoFrame.Height);
                VideoWriter.Width = (int)Math.Round(VideoFrame.Width);
                VideoWriter.Open(filePath);*/
                VideoWriter.Open(filePath, (int)Math.Round(VideoFrame.Width), (int)Math.Round(VideoFrame.Height), 30, VideoCodec.H264);
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
        public void OpenDeviceProperties(IntPtr ParentHandle)
        {
            VideoSource.DisplayPropertyPage(ParentHandle);// IntPtr.Zero); //This will display a form with camera controls

            if (VideoSource.CheckIfCrossbarAvailable()) //if (Cam1.AvailableCrossbarVideoInputs.Length > 0)
                VideoSource.DisplayCrossbarPropertyPage(ParentHandle);
            
        }
    }
}
