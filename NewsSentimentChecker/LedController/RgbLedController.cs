using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace NewsSentimentChecker.LedController
{
    public class RgbLedController
    {
        private const int RedLedPinNo = 17;
        private const int GreenLedPinNo = 27;
        private const int BlueLedPinNo = 22;

        private GpioPin redLedPin;
        private GpioPin greenLedPin;
        private GpioPin blueLedPin;

        public RgbLedController()
        {
            InitGPIO();
        }

        public LedColorEnum CurrentLedColor { get; set; }

        //private

        public void TurnRedOn()
        {
            SetMultipleColorsOn(true, false, false);
            CurrentLedColor = LedColorEnum.Red;
        }

        public void TurnGreenOn()
        {
            SetMultipleColorsOn(false, true, false);
            CurrentLedColor = LedColorEnum.Green;
        }

        public void TurnBlueOn()
        {
            SetMultipleColorsOn(false, false, true);
            CurrentLedColor = LedColorEnum.Blue;
        }

        public void TurnYelowOn()
        {
            SetMultipleColorsOn(true, true, false);
            CurrentLedColor = LedColorEnum.Yelow;
        }

        public void TurnCyanOn()
        {
            SetMultipleColorsOn(false, true, true);
            CurrentLedColor = LedColorEnum.Cyan;
        }

        public void TurnMagentaOn()
        {
            SetMultipleColorsOn(true, false, true);
            CurrentLedColor = LedColorEnum.Magenta;
        }

        public void TurnWhiteOn()
        {
            SetMultipleColorsOn(true, true, true);
            CurrentLedColor = LedColorEnum.White;
        }

        public void SetMultipleColorsOn(bool isRedOn, bool isGreenOn, bool isBlueOn)
        {
            TurnLedOff();
            if (isRedOn)
            {
                redLedPin.Write(GpioPinValue.High);
                redLedPin.SetDriveMode(GpioPinDriveMode.Output);
            }
            if (isGreenOn)
            {
                greenLedPin.Write(GpioPinValue.High);
                greenLedPin.SetDriveMode(GpioPinDriveMode.Output);
            }
            if (isBlueOn)
            {
                blueLedPin.Write(GpioPinValue.High);
                blueLedPin.SetDriveMode(GpioPinDriveMode.Output);
            }
        }

        public void TurnLedOff()
        {
            redLedPin.Write(GpioPinValue.Low);
            redLedPin.SetDriveMode(GpioPinDriveMode.Output);

            greenLedPin.Write(GpioPinValue.Low);
            greenLedPin.SetDriveMode(GpioPinDriveMode.Output);

            blueLedPin.Write(GpioPinValue.Low);
            blueLedPin.SetDriveMode(GpioPinDriveMode.Output);

            CurrentLedColor = LedColorEnum.None;
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                throw new InvalidOperationException("Unable to initialize GPIO");
            }

            redLedPin = gpio.OpenPin(RedLedPinNo);
            greenLedPin = gpio.OpenPin(GreenLedPinNo);
            blueLedPin = gpio.OpenPin(BlueLedPinNo);
        }
    }
}
