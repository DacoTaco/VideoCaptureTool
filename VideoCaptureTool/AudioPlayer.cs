using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace VideoCaptureTool
{
    public class AudioDevice
    {
        public string DeviceName { get; set; }
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public int BitRate { get; set; }

        public override string ToString()
        {
 	         return DeviceName;
        }
    }


    public class AudioPlayer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private WaveIn input = null;
        private WaveOut output = null;
        public ObservableCollection<AudioDevice> AudioDevices;
        private BufferedWaveProvider WaveProvider;
        VolumeWaveProvider16 volumeHandler;

        /// <summary>
        /// Get or Set the Volume of the player. 1 = 100% , 0 = 0%
        /// </summary>
        private float volume = 1;
        public float Volume
        {
            get
            {
                if (volumeHandler == null)
                    return volume * 100;
                return (volumeHandler.Volume >= 1) ? 100 : volumeHandler.Volume * 100;
            }
            set
            {
                float number = value / 100;

                if (number > 1)
                    number = 1;
                else if (number < 0)
                    number = 0;

                volume = number;

                if (volumeHandler != null)
                {            
                    volumeHandler.Volume = number;
                }
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
            }
        }
        public bool DeviceClosed
        {
            get
            {
                return (deviceOpen == false);
            }
        }

        private void input_DataAvailable(object sender, WaveInEventArgs e)
        {
            int nextTotal = WaveProvider.BufferedBytes + e.BytesRecorded;
            if (WaveProvider == null || nextTotal > WaveProvider.BufferLength)
            {
                //MessageBox.Show("Application tried adding audio data to buffer that is already full!");
                return;
            }

            WaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        public AudioDevice GetBestCapability(WaveInCapabilities device)
        {
            int sampleRate = 44100;
            int bitrate = 16;

            if (device.Channels == 1)
            {
                //mono
                if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_96M16))
                {
                    bitrate = 16;
                    sampleRate = 96000;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_96M08))
                {
                    bitrate = 8;
                    sampleRate = 96000;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_48M16))
                {
                    bitrate = 16;
                    sampleRate = 48000;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_48M08))
                {
                    bitrate = 8;
                    sampleRate = 48000;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_4M16))
                {
                    bitrate = 16;
                    sampleRate = 44100;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_4M08))
                {
                    bitrate = 8;
                    sampleRate = 44100;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_2M16))
                {
                    bitrate = 16;
                    sampleRate = 22050;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_2M08))
                {
                    bitrate = 8;
                    sampleRate = 22050;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_1M16))
                {
                    bitrate = 16;
                    sampleRate = 11025;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_1M08))
                {
                    bitrate = 8;
                    sampleRate = 11025;
                }
                else
                {
                    bitrate = 0;
                    sampleRate = 0;
                }
            }
            else
            {
                //stereo
                if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_96S16))
                {
                    bitrate = 16;
                    sampleRate = 96000;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_96S08))
                {
                    bitrate = 8;
                    sampleRate = 96000;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_48S16))
                {
                    bitrate = 16;
                    sampleRate = 48000;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_48S08))
                {
                    bitrate = 8;
                    sampleRate = 48000;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_4S16))
                {
                    bitrate = 16;
                    sampleRate = 44100;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_4S08))
                {
                    bitrate = 8;
                    sampleRate = 44100;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_2S16))
                {
                    bitrate = 16;
                    sampleRate = 22050;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_2S08))
                {
                    bitrate = 8;
                    sampleRate = 22050;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_1S16))
                {
                    bitrate = 16;
                    sampleRate = 11025;
                }
                else if (device.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_1S08))
                {
                    bitrate = 8;
                    sampleRate = 11025;
                }
                else
                {
                    bitrate = 0;
                    sampleRate = 0;
                }
            }

            return new AudioDevice() { BitRate = bitrate, SampleRate = sampleRate, Channels = device.Channels, DeviceName = device.ProductName };

        }

        public AudioPlayer()
        {

            DetectDevices();

            output = new WaveOut();
            input = new WaveIn();
        }

        public void DetectDevices()
        {
            AudioDevices = new ObservableCollection<AudioDevice>();
            List<NAudio.Wave.WaveInCapabilities> devices = new List<NAudio.Wave.WaveInCapabilities>();

            //gets the input (wavein) devices and adds them to the list
            for (short i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                devices.Add(NAudio.Wave.WaveIn.GetCapabilities(i));
            }

            //each device gets inserted into the devices list, which is linked to the listdevices listview module
            foreach (var device in devices)
            {
                AudioDevice newDevice = GetBestCapability(device);
                if (newDevice != null)
                {
                    AudioDevices.Add(newDevice);
                }
            }
        }

        public void Start(int AudioIndex, AudioDevice device)
        {
            if (AudioIndex != -1)
            {
                input.DataAvailable += new EventHandler<WaveInEventArgs>(input_DataAvailable);
                input.DeviceNumber = AudioIndex;

                WaveFormat format = null;

                if (device.BitRate != 0 && device.SampleRate != 0)
                {
                    format = new WaveFormat(device.SampleRate, device.BitRate, device.Channels);
                    input.WaveFormat = format;
                }
                
                WaveProvider = new BufferedWaveProvider(input.WaveFormat);
                
                //WaveProvider.DiscardOnBufferOverflow = true;
                //make max buffer twice as long so it doesn't crash instantly and all that
                //WaveProvider.BufferLength = (int)(WaveProvider.BufferLength /5);
                WaveProvider.BufferDuration = TimeSpan.FromMilliseconds(400);
                //WaveProvider.ReadFully = false;

                volumeHandler = new VolumeWaveProvider16(WaveProvider);
                volumeHandler.Volume = volume;

                //output.DesiredLatency = 0;
                output.Init(volumeHandler);
                output.Play();
                input.StartRecording();
                DeviceOpen = true;

            }
        }
        public void Stop()
        {
            if (input != null)
            {
                input.StopRecording();
                input.Dispose();
                input = new WaveIn();
            }
            if (output != null && output.PlaybackState != PlaybackState.Stopped)
            {
                output.Stop();
                output.Dispose();
                volumeHandler.ToSampleProvider().Skip(TimeSpan.FromSeconds(5));
                WaveProvider.ClearBuffer();
                WaveProvider = null;
                output = new WaveOut();

            }
            DeviceOpen = false;
        }
        /// <summary>
        /// Retrieve's the audioplayer's waveformat.
        /// </summary>
        /// <param name="inputFormat"></param>
        /// <returns></returns>
        public WaveFormat GetWaveFormat()
        {
            return WaveProvider.WaveFormat;
        }

        public void OpenDeviceProperties(int index, IntPtr ParentHandle)
        {

        }
    }
}
