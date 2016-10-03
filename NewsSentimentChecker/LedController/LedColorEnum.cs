using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsSentimentChecker.LedController
{
    [Flags]
    public enum LedColorEnum
    {
        None = 0,
        Red = 1,
        Green = 2,
        Blue = 4,
        Yelow = Red | Green,
        Cyan = Green | Blue,
        Magenta = Red | Blue,
        White = Red | Green | Blue
    }
}
