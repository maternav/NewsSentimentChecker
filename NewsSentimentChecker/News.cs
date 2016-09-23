using System;

namespace NewsSentimentChecker
{
    public class News
    {
        public DateTime DatePublished { get; set; }
        public string Title { get; set; }
        public double? Sentiment { get; set; }
    }
}
