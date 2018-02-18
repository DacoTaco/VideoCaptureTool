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
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

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
                BitmapImage newImage = null;

                if(value != null)
                {
                    if (ResizeFrame)
                    {
                        try
                        {
                            //TODO : check if we actually get better image, and else just remove this
                            //if we DO get better image... find a faster method!
                            float height = (float)value.Height * 1.5F;
                            float width = (float)value.Width * 1.5F;

                            Bitmap oldImage = BitmapImage2Bitmap(value);
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

                            newImage = Bitmap2BitmapImage(bmp);
                        }
                        catch(Exception ex)
                        {
                            newImage = value;
                        }
                    }
                    else
                    {
                        newImage = value;
                    }
                }
                image = newImage;
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

        VideoFileWriter VideoWriter = new VideoFileWriter();

        FilterInfoCollection videoDevices = null;

        public ObservableCollection<FilterInfo> VideoDevices;

        private ImageFormat GetImageFormat(Image img)
        {        
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
                return System.Drawing.Imaging.ImageFormat.Jpeg;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Bmp))
                return System.Drawing.Imaging.ImageFormat.Bmp;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png))
                return System.Drawing.Imaging.ImageFormat.Png;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Emf))
                return System.Drawing.Imaging.ImageFormat.Emf;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Exif))
                return System.Drawing.Imaging.ImageFormat.Exif;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                return System.Drawing.Imaging.ImageFormat.Gif;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Icon))
                return System.Drawing.Imaging.ImageFormat.Icon;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.MemoryBmp))
                return System.Drawing.Imaging.ImageFormat.MemoryBmp;
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Tiff))
                return System.Drawing.Imaging.ImageFormat.Tiff;
            else
                return System.Drawing.Imaging.ImageFormat.Wmf;            
        }
        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
        private BitmapImage Bitmap2BitmapImage(Bitmap image)
        {
            if (image == null)
                return null;
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memory = new MemoryStream())
            {
                //image.Save(memory, ImageFormat.Jpeg);
                image.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage;
        }
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
            if(Cam1.AvailableCrossbarVideoInputs.Length > 0)
                Cam1.DisplayCrossbarPropertyPage(ParentHandle);
            
        }
    }
}
