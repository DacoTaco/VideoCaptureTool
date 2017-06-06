using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            /*VideoCaptureDevice Cam1;
            FilterInfoCollection VideoCaptureDevices;

            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            Cam1 = new VideoCaptureDevice(VideoCaptureDevices[0].MonikerString);
            Cam1.DisplayPropertyPage(IntPtr.Zero); //This will display a form with camera controls*/
            VideoCaptureTool.VideoPlayer test = new VideoCaptureTool.VideoPlayer();
            test.OpenDeviceProperties(0, IntPtr.Zero);
        }
    }
}
