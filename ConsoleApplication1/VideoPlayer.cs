using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
/*using Accord;
using Accord.Video.DirectShow;
using Accord.Video;*/

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

        VideoCaptureDevice VideoSource = null;
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

        public void OpenDeviceProperties(int index, IntPtr ParentHandle)
        {
            //the only reason we have Aforge. its properties window calling seems to be more complete!
            VideoCaptureDevice Cam1;

            if (ParentHandle == null)
                ParentHandle = IntPtr.Zero;

            if(VideoSource == null)
                Cam1 = new VideoCaptureDevice(videoDevices[index].MonikerString);
            else
                Cam1 = VideoSource;//new VideoCaptureDevice(videoDevices[index].MonikerString);

            //Cam1.DisplayCrossbarPropertyPage(ParentHandle);
            Cam1.DisplayPropertyPage(ParentHandle);// IntPtr.Zero); //This will display a form with camera controls
        }
    }
}
