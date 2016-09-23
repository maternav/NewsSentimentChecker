﻿using NewsSentimentChecker.Logging;
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
            await UpdateOverallSentiment();
            var finalResult = GetOverallSentiment();
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
            var input = CreateJsonInput(news);
            //byte[] byteData = System.Text.Encoding.UTF8.GetBytes(input);
            var uri = new Uri("https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment");

            using (var client = new HttpClient())
            {

                using (var content = new Windows.Web.Http.HttpStringContent(input, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"))
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AccountKey);
                    client.DefaultRequestHeaders.Accept.Add(new Windows.Web.Http.Headers.HttpMediaTypeWithQualityHeaderValue("application/json"));

                    var result = await client.PostAsync(uri, content);

                    var resStr = await result.Content.ReadAsStringAsync();
                    //var resStr = @"{""documents"":[{""score"":0.7213746,""id"":""636099996330000000""},{""score"":0.381423,""id"":""636099993100000000""},{""score"":0.05435185,""id"":""636099968060000000""},{""score"":0.7505028,""id"":""636099960290000000""},{""score"":0.6527786,""id"":""636099946280000000""},{""score"":0.4073813,""id"":""636099914040000000""},{""score"":0.384699,""id"":""636099901550000000""},{""score"":0.7070935,""id"":""636099818230000000""},{""score"":0.6415039,""id"":""636099810610000000""},{""score"":0.4527278,""id"":""636099519160000000""}],""errors"":[]}";

                    MergeUpdatedSentimentsWithCacheValues(resStr);

                }

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
    }
}
