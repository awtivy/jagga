
using System;
using Windows.Devices.Gpio;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace jagga
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {


        //Define GPIO pins for each relay
        private static int lefthead = 6, righthead = 6, leftturn = 6, rightturn = 6, topstrip = 6, license = 6, extra1 = 8, extra2 = 10;


        private GpioPin[] lp = new GpioPin[8];
        private static int[] gp = new int[8] { lefthead, righthead, leftturn, rightturn, topstrip, license, extra1, extra2 };
        private GpioPinValue _value;

        private DispatcherTimer horntimer, songtimer, schedule;

        //Define variables for horn
        private static int HornFlashIntMS = 250, HornLengthSec = 5, HornLoops = HornLengthSec * 1000 / HornLengthSec;
        static MediaSource h = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/horn.mp3"));


        //define variables for songs
        private static int SongFlashIntMS = 500, SongLengthSec = 30, SongLoops = SongLengthSec * 1000 / SongFlashIntMS;
        private static int ScheduleIntervalMin = 60;
        private int _loop = 0;

        private MediaSource[] sources;

        
        private MediaPlayer _mediaplayer;

        public MainPage()
        {
            this.InitializeComponent();

            schedule = new DispatcherTimer();
            schedule.Interval = TimeSpan.FromMinutes(ScheduleIntervalMin);
            schedule.Tick += Schedule;

            horntimer = new DispatcherTimer();
            horntimer.Interval = TimeSpan.FromSeconds(HornFlashIntMS);
            horntimer.Tick += HornLights;

            songtimer = new DispatcherTimer();
            songtimer.Interval = TimeSpan.FromSeconds(SongFlashIntMS);
            songtimer.Tick += HornLights;

            _mediaplayer = new MediaPlayer();

            _mediaplayer.Source = h;
            _mediaplayer.AutoPlay = false;
            SetupGPIO();

            schedule.Start();
            horntimer.Start();
            _mediaplayer.Play();
        }


        private void Schedule(object o, object e)
        {
            horntimer.Start();
            _mediaplayer.Source = h;
            _mediaplayer.Play();
        }

        private void HornLights(object o, object e)
        {
            if(_loop == 0)
            {
                Off(lp[righthead]);
                Off(lp[leftturn]);
            }
            if (_loop < HornLoops)
            {
                Toggle(lp[lefthead]);
                Toggle(lp[rightturn]);
                Toggle(lp[righthead]);
                Toggle(lp[leftturn]);
                Toggle(lp[topstrip]);
                Toggle(lp[license]);
                _loop++;
            }


            if (_loop >= HornLoops)
            {
                horntimer.Stop();
                _loop = 0;
                On(lp[lefthead]);
                On(lp[rightturn]);
                On(lp[righthead]);
                On(lp[leftturn]);
            }
        }
        private void SongLights(object o, object e)
        {
            if (_loop == 0)
            {
                Off(lp[righthead]);
                Off(lp[leftturn]);
            }
            if (_loop < SongLoops)
            {
                Toggle(lp[lefthead]);
                Toggle(lp[rightturn]);
                Toggle(lp[righthead]);
                Toggle(lp[leftturn]);
                Toggle(lp[topstrip]);
                Toggle(lp[license]);
                _loop++;
            }


            if (_loop >= SongLoops)
            {
                horntimer.Stop();
                _loop = 0;
                On(lp[lefthead]);
                On(lp[rightturn]);
                On(lp[righthead]);
                On(lp[leftturn]);
            }
        }
        async void GetSongs()
        {
            Windows.Storage.StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("\\Assets\\Songs");
            var songs = await folder.GetFilesAsync();
            int i = 0;
            foreach (var song in songs)
            {
                sources[i++] = MediaSource.CreateFromStorageFile(song);
                
            }

        }
        private void SetupGPIO()
        {
            GpioController gpio = GpioController.GetDefault();
            if (gpio == null)
                return; // GPIO not available on this system

            for (int i = 0; i < 8; i++)
            {
                lp[i] = gpio.OpenPin(gp[i]);
                lp[i].SetDriveMode(GpioPinDriveMode.Output);
            }


        }

        private void On(GpioPin pin)
        {
            pin.Write(GpioPinValue.Low);
        }

        private void Off(GpioPin pin)
        {
            pin.Write(GpioPinValue.High);
        }

        private void Toggle(GpioPin pin)
        {
            _value = pin.Read();
            if (pin.Read() == GpioPinValue.High)
                pin.Write(GpioPinValue.Low);
            else
                pin.Write(GpioPinValue.High);

        }

    }
}
