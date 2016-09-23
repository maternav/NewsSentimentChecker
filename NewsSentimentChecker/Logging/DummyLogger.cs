using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsSentimentChecker.Logging
{
    public class DummyLogger : ILog
    {
        public void Debug(string message)
        {
        }

        public void Error(string message)
        {
        }

        public void Fatal(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warning(string message)
        {
        }
    }
}
