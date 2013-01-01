using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using SteamTrade;
using HtmlAgilityPack;

namespace SteamTrade
{
    /// <summary>
    /// This class handles the web-based interaction for Steam trades.
    /// </summary>
    public partial class Trade
    {
        static string SteamCommunityDomain = "steamcommunity.com";
        static string SteamTradeUrl = "http://steamcommunity.com/trade/{0}/";

        string sessionId;
        string sessionIdEsc;
        string baseTradeURL;
        string steamLogin;
        CookieContainer cookies;
        

        internal int LogPos { get; set; }

        internal int Version { get; set; }

        StatusObj GetStatus ()
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);
            data.Add ("logpos", "" + LogPos);
            data.Add ("version", "" + Version);
            
            string response = Fetch (baseTradeURL + "tradestatus", "POST", data);
            return JsonConvert.DeserializeObject<StatusObj> (response);
        }

        #region Trade Web command methods

        /// <summary>
        /// Sends a message to the user over the trade chat.
        /// </summary>
        bool SendMessageWebCmd (string msg)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", sessionIdEsc);
            data.Add ("message", msg);
            data.Add ("logpos", "" + LogPos);
            data.Add ("version", "" + Version);

            string result = Fetch (baseTradeURL + "chat", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Adds a specified item by its itemid.  Since each itemid is
        /// unique to each item, you'd first have to find the item, or
        /// use AddItemByDefindex instead.
        /// </summary>
        /// <returns>
        /// Returns false if the item doesn't exist in the Bot's inventory,
        /// and returns true if it appears the item was added.
        /// </returns>
        bool AddItemWebCmd (ulong itemid, int slot)
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);

            string result = Fetch(baseTradeURL + "additem", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Removes an item by its itemid.  Read AddItem about itemids.
        /// Returns false if the item isn't in the offered items, or
        /// true if it appears it succeeded.
        /// </summary>
        bool RemoveItemWebCmd (ulong itemid, int slot)
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", "" + itemid);
            data.Add ("slot", "" + slot);

            string result = Fetch (baseTradeURL + "removeitem", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
        }

        public SteamInventory.AppInventory GetForeignInventory(ulong steamid, int appid, int contextid)
        {
            //var sessionIdEsc = Uri.UnescapeDataString(sessionId);
            //var cookies = new CookieContainer();
            //cookies.Add(new Cookie("sessionid", sessionId, String.Empty, SteamCommunityDomain));
            //cookies.Add(new Cookie("steamLogin", token, String.Empty, SteamCommunityDomain));
            var data = new NameValueCollection();

            data.Add("sessionid", sessionId);
            data.Add("steamid", steamid.ToString());
            data.Add("appid", appid.ToString());
            data.Add("contextid", contextid.ToString());

            string result = Fetch(baseTradeURL + "foreigninventory", "POST", data);
            //string result = Fetch(baseTradeURL + "foreigninventory", "POST", data);

            SteamInventory.AppContext inventoryResult = JsonConvert.DeserializeObject<SteamInventory.AppContext>(result);
            System.IO.File.WriteAllText("foreign.json", JsonConvert.SerializeObject(inventoryResult, Formatting.Indented));
            System.IO.File.WriteAllText("foreign.inventory", result);
            SteamInventory.AppInventory appInventory = new SteamInventory.AppInventory();
            appInventory.AppId = appid;
            appInventory.AppContexts.Add(contextid.ToString(), inventoryResult);
            return appInventory;
        }
        public static string CheckIfUserHasVanityURL(ulong steamid)
        {
            var url = "http://steamcommunity.com/";
            HttpWebResponse response = SteamWeb.Request(url + "profiles/" + steamid, "GET");
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                url = url + "id/";
                return response.GetResponseHeader("Location").Remove(0,url.Length).TrimEnd('/');
            }
            response.Close();
            return "false";
        }
        public AppContextData GetTrade()
        {
            string result = Fetch(baseTradeURL, "GET");
            System.IO.File.WriteAllText("trade.start", result);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(result);
            string scripts = "";
            AppContextData appContextData = new AppContextData();
            foreach (HtmlNode script in doc.DocumentNode.SelectNodes("//script"))
            {
                //scripts += scripts + script.InnerText;
                string[] vars = script.InnerText.Split(';');
                //File.WriteAllText("scripts.scripts", scripts);
                //File.WriteAllLines("scripts2.scripts", vars);
                string contSearch = "var g_rgAppContextData = ";
                string probSearch = "var g_bTradePartnerProbation = ";
                string invSearch = "var g_strInventoryLoadURL = '";
                foreach (string v in vars)
                {
                    if (v.Contains(contSearch))
                    {
                        string appcontext = v.Substring(v.IndexOf(contSearch) + contSearch.Length);
                        //File.WriteAllText("context.json", appcontext);
                        appContextData.Apps = JsonConvert.DeserializeObject<Dictionary<int, App>>(appcontext);
                        //File.WriteAllText("contexts.json", JsonConvert.SerializeObject(appContextData.Apps, Formatting.Indented));
                    }
                    if (v.Contains(probSearch))
                    {
                        string probation = v.Substring(v.IndexOf(probSearch) + probSearch.Length);
                        appContextData.IsTradePartnerOnProbation = Convert.ToBoolean(probation);
                    }
                    if (v.Contains(invSearch))
                    {
                        string invurl = v.Substring(v.IndexOf(invSearch) + invSearch.Length, v.LastIndexOf("'") - invSearch.Length - 2);
                        appContextData.InventoryLoadUrl = invurl;
                    }
                }
            }
            
            return appContextData;
        }

        SteamInventory.AppInventory GetInventory(ulong steamid, int appid, string contextid)
        {
            string vanity = CheckIfUserHasVanityURL(steamid);
            if (vanity == "false")
                vanity = steamid.ToString();
            string url = String.Format(
                "http://steamcommunity.com/id/{0}/inventory/json/{1}/{2}/?trading=1",
                vanity, appid, contextid
            );
            Console.WriteLine(url);
            try
            {
                //Console.WriteLine(cookies.Count);
                //foreach (Cookie c in cookies.GetCookies(new Uri("http://steamcommunity.com/")))
                //{
                //    Console.WriteLine(c.ToString());
                //}
                string response = SteamWeb.Fetch(url, "GET", null, cookies, true);
                System.IO.File.WriteAllText("inventory_" + appid + "_" + contextid + ".json", response);
                SteamInventory.AppContext appContext = JsonConvert.DeserializeObject<SteamInventory.AppContext>(response);
                SteamInventory.AppInventory appInventory = new SteamInventory.AppInventory();
                appInventory.AppId = appid;
                appInventory.AppContexts.Add(contextid, appContext);
                return appInventory;
            }
            catch (Exception)
            {
                //return JsonConvert.DeserializeObject("{\"success\":\"false\"}");
            }
            return null;
        }
        /// <summary>
        /// Sets the bot to a ready status.
        /// </summary>
        bool SetReadyWebCmd (bool ready)
        {
            var data = new NameValueCollection ();
            data.Add ("sessionid", sessionIdEsc);
            data.Add ("ready", ready ? "true" : "false");
            data.Add ("version", "" + Version);
            
            string result = Fetch (baseTradeURL + "toggleready", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Accepts the trade from the user.  Returns a deserialized
        /// JSON object.
        /// </summary>
        bool AcceptTradeWebCmd ()
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);
            data.Add ("version", "" + Version);

            string response = Fetch (baseTradeURL + "confirm", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(response);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Cancel the trade.  This calls the OnClose handler, as well.
        /// </summary>
        bool CancelTradeWebCmd ()
        {
            var data = new NameValueCollection ();

            data.Add ("sessionid", sessionIdEsc);

            string result = Fetch (baseTradeURL + "cancel", "POST", data);

            dynamic json = JsonConvert.DeserializeObject(result);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
        }

        #endregion Trade Web command methods
        
        string Fetch (string url, string method, NameValueCollection data = null)
        {
            return SteamWeb.Fetch (url, method, data, cookies);
        }

        void Init()
        {
            sessionIdEsc = Uri.UnescapeDataString(sessionId);

            Version = 1;

            cookies = new CookieContainer();
            cookies.Add (new Cookie ("sessionid", sessionId, String.Empty, SteamCommunityDomain));
            cookies.Add (new Cookie ("steamLogin", steamLogin, String.Empty, SteamCommunityDomain));
            cookies.Add (new Cookie ("bCompletedTradeTutorial", "true", String.Empty, SteamCommunityDomain));

            baseTradeURL = String.Format (SteamTradeUrl, OtherSID.ConvertToUInt64 ());
        }

        public class StatusObj
        {
            public string error { get; set; }
            
            public bool newversion { get; set; }
            
            public bool success { get; set; }
            
            public long trade_status { get; set; }
            
            public int version { get; set; }
            
            public int logpos { get; set; }
            
            public TradeUserObj me { get; set; }
            
            public TradeUserObj them { get; set; }
            
            public TradeEvent[] events { get; set; }
        }

        public class TradeEvent
        {
            public string steamid { get; set; }
            
            public int action { get; set; }
            
            public ulong timestamp { get; set; }
            
            public int appid { get; set; }
            
            public string text { get; set; }
            
            public int contextid { get; set; }
            
            public ulong assetid { get; set; }
        }
        
        public class TradeUserObj
        {
            public int ready { get; set; }
            
            public int confirmed { get; set; }
            
            public int sec_since_touch { get; set; }
        }
    }


}

