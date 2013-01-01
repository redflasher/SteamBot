using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SteamTrade
{
    public class AssetPrices
    {
        public static int[] ValidAppIDs = { 440, 520, 570, 620, 816, 205790 };

        public static Dictionary<int, AssetPrices> FetchAllAssetPrices(string apiKey, string language = "")
        {
            Dictionary<int, AssetPrices> assetPriceses = new Dictionary<int, AssetPrices>();
            foreach (int id in ValidAppIDs)
            {
                assetPriceses.Add(id, FetchAssetPrices(id, apiKey, language));
            }
            return assetPriceses;
        }
        public static AssetPrices FetchAssetPrices(int appid, string apiKey, string language = "")
        {
            if (!ValidAppIDs.Contains(appid))
                throw new ArgumentOutOfRangeException("see http://wiki.teamfortress.com/wiki/WebAPI#appids for list of valid ids");
            if (language != null)
            {
                language = "&language=" + language;
            }
            string url = String.Format("http://api.steampowered.com/ISteamEconomy/GetAssetPrices/v0001/?key={0}&appid={1}{2}", apiKey, appid, language);
            Console.WriteLine("Fetching AssetPrices for appid:" + appid + " from " + url);

            try
            {
                string response = SteamWeb.Fetch(url, "GET", null, null, true);
                System.IO.File.WriteAllText("assetprices_" + appid + ".prices", response);
                AssetPrices ass = JsonConvert.DeserializeObject<AssetPrices>(response);
                System.IO.File.WriteAllText("assetprices2_" + appid + ".prices", JsonConvert.SerializeObject(ass, Formatting.Indented));
                return JsonConvert.DeserializeObject<AssetPrices>(response);
            }
            catch (Exception)
            {
                //return JsonConvert.DeserializeObject ("{\"success\":\"false\"}");
            }
            return null;
        }
        [JsonProperty("result")]
        public ResponseResult Result { get; set; }
        public class ResponseResult
        {
            [JsonProperty("success")]
            public bool Success { get; set; }
            [JsonProperty("assets")]
            public List<Asset> Assets { get; set; }
            [JsonProperty("tags")]
            public dynamic Tags { get; set; }
            [JsonProperty("tag_ids")]
            public dynamic TagIds { get; set; }
        }
            
        public class Asset
        {
            [JsonProperty("prices")]
            public Currencies Prices { get; set; }
            [JsonProperty("original_prices")]
            public Currencies OriginalPrices { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("date")]
            public string Date { get; set; }
            [JsonProperty("class")]
            public List<AssetClass> Class { get; set; }
            [JsonProperty("classid")]
            public string ClassId { get; set; }
            [JsonProperty("tags")]
            public List<string> Tags { get; set; }
            //appid 816 differs from the rest!
            [JsonProperty("tag_ids")]
            public dynamic[] TagIds { get; set; }
            
        }
        public class Currencies
        {
            [JsonProperty("USD")]
            public int USD { get; set; }
            [JsonProperty("GBP")]
            public int GBP { get; set; }
            [JsonProperty("EUR")]
            public int EUR { get; set; }
            [JsonProperty("RUB")]
            public int RUB { get; set; }
            [JsonProperty("BRL")]
            public int BRL { get; set; }
        }

        public class AssetClass
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("value")]
            public string Value { get; set; }
        }
    }
}
