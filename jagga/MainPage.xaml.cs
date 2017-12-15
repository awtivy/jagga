
using System;
using Windows.Devices.Gpio;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Security.Cryptography;
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
        private static int lefthead = 13, righthead = 12, leftturn = 19, rightturn = 16, topstrip = 26, license = 6, extra1 = 21, extra2 = 20;


        private GpioPin[] lp = new GpioPin[8];
        private static int[] gp = new int[8] { lefthead, righthead, leftturn, rightturn, topstrip, license, extra1, extra2 };
        private static int lh = 0, rh = 1, lt = 2, rt = 3, ts = 4, ls = 5, e1 = 6, e2 = 6;
        private GpioPinValue _value;

        private DispatcherTimer horntimer, songtimer, schedule;

        //Define variables for horn
        private static int HornFlashIntMS = 250, HornLengthSec = 3, HornLoops = HornLengthSec * 1000 / HornFlashIntMS;
        static MediaSource h = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/horn.mp3"));


        //define variables for songs
        private int SongFlashIntMS = 500;
        private static int ScheduleIntervalMin = 30;
        private int _loop = 0, _sloop =0, randint, songloops = 20;//default value to start with, will check song to adjust this.
        private TimeSpan songlength;
        public int songscount = 12;


        public MediaSource[] sources;

        
        private MediaPlayer _mediaplayer;

        public MainPage()
        {
            this.InitializeComponent();

            schedule = new DispatcherTimer();
            schedule.Interval = TimeSpan.FromMinutes(ScheduleIntervalMin);
            schedule.Tick += Schedule;

            horntimer = new DispatcherTimer();
            horntimer.Interval = TimeSpan.FromMilliseconds(HornFlashIntMS);
            horntimer.Tick += HornLights;

            songtimer = new DispatcherTimer();
            songtimer.Interval = TimeSpan.FromMilliseconds(SongFlashIntMS);
            songtimer.Tick += SongLights;

            _mediaplayer = new MediaPlayer();

            _mediaplayer.Source = h;
            _mediaplayer.AutoPlay = false;

            SetupGPIO();
            GetSongs();

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
                Off(lp[rh]);
                Off(lp[lt]);
            }
            if (_loop < HornLoops)
            {
                Toggle(lp[lh]);
                Toggle(lp[rt]);
                Toggle(lp[rh]);
                Toggle(lp[lt]);
                Toggle(lp[ts]);
                Toggle(lp[ls]);
                _loop++;
            }


            if (_loop >= HornLoops)
            {
                horntimer.Stop();
                
                
                _loop = 0;
                On(lp[lh]);
                On(lp[rt]);
                On(lp[rh]);
                On(lp[lt]);

                randint = (int) (CryptographicBuffer.GenerateRandomNumber() % (songscount-1));
               
                _mediaplayer.Source = sources[randint];

                _mediaplayer.AutoPlay = false;
                _mediaplayer.Play();

                songlength = sources[randint].Duration.Value;
                songloops = (int) songlength.TotalMilliseconds / SongFlashIntMS;
                songtimer.Start();
            }
        }
        private void SongLights(object o, object e)
        {
            if (_sloop == 0)
            {
                Off(lp[rh]);
                Off(lp[lt]);
            }
            if (_sloop < songloops)
            {
                Toggle(lp[lh]);
                Toggle(lp[rt]);
                Toggle(lp[rh]);
                Toggle(lp[lt]);
                Toggle(lp[ts]);
                Toggle(lp[ls]);
                _sloop++;
            }


            if (_sloop >= songloops)
            {
                songtimer.Stop();
                _sloop = 0;
                On(lp[lh]);
                On(lp[rt]);
                On(lp[rh]);
                On(lp[lt]);
                On(lp[ls]);
                On(lp[ts]);

                songloops = 20;
            }
        }
        async void GetSongs()
        {
            Windows.Storage.StorageFolder folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            Windows.Storage.StorageFolder fsongs = await folder.GetFolderAsync("Assets\\Songs");
            var songs = await fsongs.GetFilesAsync();
            songscount = songs.Count;
            sources = new MediaSource[songs.Count];
            int i = 0;
            foreach (IStorageFile song in songs)
            {
                sources[i] = MediaSource.CreateFromStorageFile(song);
                await sources[i++].OpenAsync();
                
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
            pin.Write(GpioPinValue.High);
        }

        private void Off(GpioPin pin)
        {
            pin.Write(GpioPinValue.Low);
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
