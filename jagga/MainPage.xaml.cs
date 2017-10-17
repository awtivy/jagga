
using System;
using Windows.Devices.Gpio;
using Windows.Media.Core;
using Windows.Media.Playback;
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
        private GpioPin _leftHead;
        private GpioPin _rightHead;
        private GpioPin _leftTurn;
        private GpioPin _rightTurn;
        private GpioPinValue _value;


        private DispatcherTimer _timer, _schedule;
        private int FlashIntervalSec = 1;
        private int ScheduleIntervalMin = 60;
        private int _loop = 0;
        
        private MediaPlayer truckSound;

        public MainPage()
        {
            this.InitializeComponent();
            _schedule = new DispatcherTimer();
            _schedule.Interval = TimeSpan.FromMinutes(ScheduleIntervalMin);
            _schedule.Tick += Schedule;


            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(FlashIntervalSec);
            _timer.Tick += BlinkLights;

            truckSound = new MediaPlayer();
            Uri truckSource = new Uri("ms-appx:///Assets/trucksound.wav");
            truckSound.Source = MediaSource.CreateFromUri(truckSource);
            truckSound.AutoPlay = false;
            SetupGPIO();

            _schedule.Start();
            _timer.Start();
            truckSound.Play();
        }
        private void SetupGPIO()
        {
            GpioController gpio = GpioController.GetDefault();
            if (gpio == null)
                return; // GPIO not available on this system

            _leftHead = gpio.OpenPin(6);
            _leftHead.SetDriveMode(GpioPinDriveMode.Output);

            _rightHead = gpio.OpenPin(13);
            _rightHead.SetDriveMode(GpioPinDriveMode.Output);

            _leftTurn = gpio.OpenPin(19);
            _leftTurn.SetDriveMode(GpioPinDriveMode.Output);

            _rightTurn = gpio.OpenPin(26);
            _rightTurn.SetDriveMode(GpioPinDriveMode.Output);
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

        private void Schedule(object o, object e)
        {
            _timer.Start();
            truckSound.Play();
        }

        private void BlinkLights(object o, object e)
        {
            if(_loop == 0)
            {
                Off(_rightHead);
                Off(_leftTurn);
            }
            if (_loop < 20)
            {
                Toggle(_leftHead);
                Toggle(_rightTurn);
                Toggle(_rightHead);
                Toggle(_leftTurn);
                _loop++;
            }


            if (_loop >=20)
            {
                _timer.Stop();
                _loop = 0;
                On(_leftHead);
                On(_rightTurn);
                On(_rightHead);
                On(_leftTurn);
            }
        }
    }
}
