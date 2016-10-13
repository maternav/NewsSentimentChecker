using NewsSentimentChecker.LcdDisplayController;
using NewsSentimentChecker.LedController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace NewsSentimentChecker.Logging
{
    public class LcdDisplayLogger : ILog
    {
        #region LCD
        //Setup address

        private const string I2C_CONTROLLER_NAME = "I2C1"; //use for RPI2

        private const byte DEVICE_I2C_ADDRESS = 0x27; // 7-bit I2C address of the port expander
        
        //Setup pins

        private const byte EN = 0x02;

        private const byte RW = 0x01;

        private const byte RS = 0x00;

        private const byte D4 = 0x04;

        private const byte D5 = 0x05;

        private const byte D6 = 0x06;

        private const byte D7 = 0x07;

        private const byte BL = 0x03;
        #endregion

        SortedDictionary<long, string> messagesCache = new SortedDictionary<long, string>();
        private const int maxDisplayedMessages = 10;
        private const int messageRefreshInterval = 5;
        private readonly DisplayI2C lcdDisplay;
        object lockingObj = new object();

        private KeyValuePair<long, string> displayedMessage = new KeyValuePair<long, string>();

        public LcdDisplayLogger()
        {
            lcdDisplay = new DisplayI2C(DEVICE_I2C_ADDRESS, I2C_CONTROLLER_NAME, RS, RW, EN, D4, D5, D6, D7, BL);
            lcdDisplay.init();

            var periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(DisplayMessage, TimeSpan.FromSeconds(messageRefreshInterval));
        }

        private void StoreMessageToCache(string message)
        {
            lock (lockingObj)
            {
                var newMax = messagesCache.Keys.Any() ? messagesCache.Keys.Max() + 1 : 1;
                DequeueExcessiveMessages();
                messagesCache.Add(newMax, message);
            }
        }

        public void Debug(string message)
        {
            StoreMessageToCache(message);
        }

        public void Error(string message)
        {
            StoreMessageToCache(message);
        }

        public void Fatal(string message)
        {
            StoreMessageToCache(message);
        }

        public void Info(string message)
        {
            StoreMessageToCache(message);
        }

        public void Warning(string message)
        {
            StoreMessageToCache(message);
        }

        private void DequeueExcessiveMessages()
        {
            var msgToRemove = messagesCache.Count - maxDisplayedMessages;
            if (msgToRemove < 1)
                return;


            var toRemove = messagesCache.Take(msgToRemove).ToList();
            toRemove.ForEach(r => messagesCache.Remove(r.Key));
        }

        private void DisplayMessage(ThreadPoolTimer timer)
        {
            KeyValuePair<long, string> newMsgToDisplay;
            lock (lockingObj)
            {
                if (messagesCache.Count == 0)
                {
                    DisplayMessage(String.Empty);
                    return;
                }
                if (messagesCache.ContainsKey(displayedMessage.Key) && messagesCache.Count > 1)
                {
                    //newMsgToDisplay = messagesCache.SkipWhile(pair => pair.Key <= displayedMessage.Key).First();
                    if (messagesCache.Any(c => c.Key > displayedMessage.Key))
                        newMsgToDisplay = messagesCache.Where(c => c.Key > displayedMessage.Key).First();
                    else
                        newMsgToDisplay = messagesCache.First();


                }
                else
                {
                    newMsgToDisplay = messagesCache.First();
                }
            }
            string text = FormatMsgToDisplay(newMsgToDisplay);
            DisplayMessage(text);
            displayedMessage = newMsgToDisplay;
        }

        private string FormatMsgToDisplay(KeyValuePair<long, string> messagePair)
        {
            return messagePair.Value;
        }

        private void DisplayMessage(string message)
        {
            lcdDisplay.clrscr();
            if (!String.IsNullOrWhiteSpace(message))
            {
                if (message.Length < 17)
                    lcdDisplay.prints(message);
                else
                {
                    var msgPart = message.Substring(0, 16);//new String(message.Take(17).ToArray());
                    lcdDisplay.prints(msgPart);

                    msgPart = message.Substring(16);
                    lcdDisplay.gotoSecondLine();
                    lcdDisplay.prints(msgPart);
                }
            }
        }


    }
}
