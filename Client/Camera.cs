using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Video.DirectShow;

namespace WindowsFormsApp2
{
    class Camera
    {
        private VideoCaptureDevice videoDevice;
        private FilterInfoCollection videoDevices;
        public void capture()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            videoDevice = new VideoCaptureDevice(videoDevices[0].MonikerString);
        }

    }

}
