
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Devices.Gpio;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Search;
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
        private static int lefthead = 13, righthead = 12, leftturn = 19, rightturn = 16, topstrip = 26, license = 6, fogmachine = 20, extra = 21;


        private GpioPin[] lp = new GpioPin[8];
        private static int[] gp = new int[8] { lefthead, righthead, leftturn, rightturn, topstrip, license, fogmachine, extra };
        private static int lh = 0, rh = 1, lt = 2, rt = 3, ts = 4, ls = 5, fog = 6, e2 = 7;
        private GpioPinValue _value;

        private DispatcherTimer horntimer, songtimer, schedule, fogtimer;

        //Define variables for horn
        private static int HornFlashIntMS = 200, HornLengthSec = 8, HornLoops = HornLengthSec * 1000 / HornFlashIntMS;

        static MediaSource h = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/horn.mp3"));
        private MediaSource horn;


        //define variables for songs
        private int SongFlashIntMS = 500;
        private static int ScheduleIntervalMin = 20;
        private int _loop = 0, _sloop = 0, randint, songloops = 20;//default value to start with, will check song to adjust this.
        private TimeSpan songlength;
        public int songscount = 12;

        //fog
        private int FogIntMS = 1500;

        public MediaSource[] sources;

        
        private MediaPlayer _mediaplayer;

        public MainPage()
        {
            this.InitializeComponent();

            schedule = new DispatcherTimer();
            schedule.Interval = TimeSpan.FromMinutes(ScheduleIntervalMin);
            schedule.Tick += Schedule;

            fogtimer = new DispatcherTimer();
            fogtimer.Interval = TimeSpan.FromMilliseconds(FogIntMS);
            fogtimer.Tick += FogMachine;

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

            schedule.Start(); //Timers do not execute before they wait
            fogtimer.Start(); //Start fog machine timer so that it turns off after delay
            Off(lp[fog]);   //Turn on fog machine only the first time manually
            horntimer.Start();  //Start Horn timer to flash lights
            _mediaplayer.Play(); //Start to play music first time manually
        }


        private void Schedule(object o, object e)
        {
            Off(lp[fog]); //Turn on fog machinwe
            fogtimer.Start(); //Start timer to turn off fog machine
            horntimer.Start(); //Start timer to flash lights
            _mediaplayer.Source = horn; //Set media source back to horn
            _mediaplayer.Play(); //Play the horn track
        }
        private void FogMachine(object o, object e)
        {
            fogtimer.Stop(); //Stop the timer
            On(lp[fog]);//Turn off fog machine
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
                songloops = (int) (songlength.TotalMilliseconds / SongFlashIntMS)-1;
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
            Windows.Storage.StorageFolder fsongs = Windows.Storage.KnownFolders.MusicLibrary;
            Windows.Storage.StorageFolder fhorn = await fsongs.GetFolderAsync("Horn");
            
            horn = MediaSource.CreateFromStorageFile(await fhorn.GetFileAsync("horn.mp3"));

            

            var songs = await fsongs.GetFilesAsync();

            songscount = songs.Count;
            sources = new MediaSource[songscount];
            int i = 0;
            foreach (var song in songs)
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
