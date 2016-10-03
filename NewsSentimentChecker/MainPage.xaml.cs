using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Json;
using NewsSentimentChecker.SentimentProvider;
using NewsSentimentChecker.Logging;
using Windows.System.Threading;
using NewsSentimentChecker.LedController;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NewsSentimentChecker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ISentimentProvider sentimentProvider;
        private readonly RgbLedController ledController;

        public MainPage()
        {
            this.InitializeComponent();
            if (sentimentProvider == null)
            {
                sentimentProvider = new MicrosoftCognitiveApiSentimentProvider(new DummyLogger());
            }
            if(ledController == null)
            {
                ledController = new RgbLedController();
            }

            int period = 3;

            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(StartSentimentLoop,
                                                    TimeSpan.FromHours(period));

            StartSentimentLoop(null);

        }


        private async void StartSentimentLoop(ThreadPoolTimer timer)
        {
            try
            {
                var overallSentiment = await sentimentProvider.GetSentimentAsync();

                if (overallSentiment < .33d)
                {
                    ledController.TurnRedOn();
                }
                else if (overallSentiment < .66d)
                {
                    ledController.TurnYelowOn();
                }
                else
                {
                    ledController.TurnGreenOn();
                }
            }
            catch(Exception e)
            {
                ledController.TurnLedOff();
            }
        }




        
    }
}
