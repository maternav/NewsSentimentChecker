using NewsSentimentChecker.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Json;
using Windows.Web.Http;

namespace NewsSentimentChecker.SentimentProvider
{
    public class MicrosoftCognitiveApiSentimentProvider : ISentimentProvider
    {
        private readonly ILog logger;

        private List<News> NewsCache = new List<News>();
        private const int NewsCacheSize = 10;
        private const string AccountKey = "cfb70fd48e7641a995135e1e378d1327";

        public MicrosoftCognitiveApiSentimentProvider(ILog log)
        {
            logger = log;
        }

        async public Task<double?> GetSentimentAsync()
        {
            logger.Info($"Getting sentiment");
            await UpdateOverallSentiment();
            var finalResult = GetOverallSentiment();

            logger.Info("Sentiment resolved");
            return finalResult;
        }


        async private Task UpdateOverallSentiment()
        {
            var newNews = await GetNewNews(null);
            if (newNews.Count == 0)
                return;

            NewsCache.AddRange(newNews);
            NewsCache = NewsCache.OrderByDescending(c => c.DatePublished).Take(NewsCacheSize).ToList();

            await UpdateSentimentValuesForNewNews(NewsCache.Where(c => !c.Sentiment.HasValue).ToList());

        }


        async private Task UpdateSentimentValuesForNewNews(List<News> news)
        {
            try
            {
                var input = CreateJsonInput(news);
                var uri = new Uri("https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment");

                using (var client = new HttpClient())
                {

                    using (var content = new Windows.Web.Http.HttpStringContent(input, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"))
                    {
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AccountKey);
                        client.DefaultRequestHeaders.Accept.Add(new Windows.Web.Http.Headers.HttpMediaTypeWithQualityHeaderValue("application/json"));

                        var result = await client.PostAsync(uri, content);

                        var resStr = await result.Content.ReadAsStringAsync();
                        MergeUpdatedSentimentsWithCacheValues(resStr);

                    }

                }
            }
            catch(Exception e)
            {
                logger.Error($"Cognitive service call failed: {e.Message}");
            }


        }

        private void MergeUpdatedSentimentsWithCacheValues(string resultJson)
        {
            JsonObject jsonObj = JsonObject.Parse(resultJson);

            var documentsArray = jsonObj.GetNamedArray("documents");
            var processedObjects = documentsArray.Select(a =>
            {
                var objNew = a.GetObject();
                var objParsednew = new TextAnalyticsResponse
                {
                    Id = long.Parse(objNew.GetNamedString("id")),
                    Sentiment = objNew.GetNamedNumber("score")
                };
                return objParsednew;
            }).ToList();

            processedObjects.ForEach(update =>
            {
                var toBeUpdated = NewsCache.Where(c => c.DatePublished.Ticks == update.Id).ToList();
                toBeUpdated.ForEach(oldValue => oldValue.Sentiment = update.Sentiment);
            });

        }

        private double? GetOverallSentiment()
        {
            var withValues = NewsCache.Where(v => v.Sentiment.HasValue).ToList();
            if (withValues.Count == 0)
                return null;

            double totalSum = 0;
            withValues.ForEach(v => totalSum += v.Sentiment.Value);

            return totalSum / withValues.Count;
        }

        private string CreateJsonInput(List<News> news)
        {
            var inputs = news.Select(n =>
            {
                JsonObject obj = new JsonObject();
                obj.SetNamedValue("language", JsonValue.CreateStringValue("en"));
                obj.SetNamedValue("text", JsonValue.CreateStringValue(n.Title));
                obj.SetNamedValue("id", JsonValue.CreateStringValue(n.DatePublished.Ticks.ToString()));
                return obj.Stringify();
            });

            JsonObject finalObject = new JsonObject();
            var jsonInternalArray = String.Join(",", inputs);
            var finalString = @"{ ""documents"": [ " + jsonInternalArray + @"] }";

            return finalString;
        }

        async private Task<List<News>> GetNewNews(DateTime? fromDate)
        {
            try
            {
                var client = new HttpClient();
                var uri = new Uri("http://feeds.reuters.com/reuters/topNews");
                string html = await client.GetStringAsync(uri);

                XElement htmlElement = XElement.Parse(html);
                var newsItems = htmlElement.Descendants("item");

                var newNews = newsItems.Select(i => new News
                {
                    DatePublished = DateTime.Parse(i.Descendants("pubDate").First().Value),
                    Title = i.Descendants("title").First().Value
                })
                .Where(r => !fromDate.HasValue || r.DatePublished > fromDate)
                .OrderByDescending(r => r.DatePublished)
                .ToList();

                return newNews;
            }
            catch(Exception e)
            {
                logger.Error(e.Message);
                return new List<News>();
            }
        }
    }
}
