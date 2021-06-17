using BattleTech;
using Newtonsoft.Json;
using PersistentMapClient.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace PersistentMapClient {
    public static class Web {



        private enum WarService {
            GetFactionShop,
            GetStarMap,
            PostBuyItems,
            PostMissionResult,
            PostSalvage,
            PostSoldItems,
            GetServerSettings,
            GetBlackMarket,
            PostBuyBlackMarketItem,
            GetHpgMail,
            PostNonWarResult
        }

        private static ServerSettings serverSettings = new ServerSettings();
        private static DateTime nextRefresh = DateTime.UtcNow;

        public static string postUrl = "api/roguewarservices/postmissionresult";
        public static string nonWarUrl = "api/roguewarservices/warmissionupdate";

        // unused for now but might be useful for the future
        public static uint iState = 0;

        public static void setIstateBits(uint bits)
        {
            iState = iState | bits;
        }

        public static bool CanPostSoldItems()
        {
            RefreshServerSettings();
            PersistentMapClient.Logger.Log($"Can Post Sold {serverSettings.CanPostSoldItems}");
            return serverSettings.CanPostSoldItems;
        }

        public static bool canBypassSupport(FactionValue faction)
        {
            RefreshServerSettings();
            return serverSettings.SupportBypass.Contains(faction.Name);

        }

        public static bool BlackMarketAvailable(SimGameState sim)
        {
            RefreshServerSettings();
            bool isAllied = false;
            foreach (FactionValue faction in FactionEnumeration.FactionList)
            {
                if (sim.IsFactionAlly(faction))
                {
                    isAllied = true;
                    break;
                }
            }
            if(!isAllied)
            {
                return false;
            }
            return serverSettings.BlackMarketAvailable;
        }

        public static bool canBypassSupport(string opfor, SimGameState sim)
        {
            RefreshServerSettings();
            bool isAllied = false;
            foreach(FactionValue faction in FactionEnumeration.FactionList)
            {
                if(sim.IsFactionAlly(faction))
                {
                    isAllied = true;
                    break;
                }
            }
            if (!isAllied)
            {
                PersistentMapClient.Logger.Log($"No Allies, cannot bypass gain!");
                return false;
            }
            bool ret = serverSettings.SupportBypass.Contains(opfor);
            PersistentMapClient.Logger.Log($"Able to bypass gain reqs against opfor:{opfor} : {ret}");
            return ret;
        }

        public static void forceRefreshServerSettings()
        {
            RefreshServerSettings(true);
        }

        private static void RefreshServerSettings(bool force=false)
        {
            if (DateTime.UtcNow > nextRefresh || force)
            {
                try
                {

                    HttpWebRequest request = new RequestBuilder(WarService.GetServerSettings).Build();
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream);
                        string itemsstring = reader.ReadToEnd();
                        serverSettings = JsonConvert.DeserializeObject<ServerSettings>(itemsstring);
                        PersistentMapClient.Logger.Log($"----Refreshing client settings from server-------");
                        PersistentMapClient.Logger.Log($"Can Post Sold: {serverSettings.CanPostSoldItems}");
                        PersistentMapClient.Logger.Log($"Online BlackMarkets Available: {serverSettings.BlackMarketAvailable}");
                        PersistentMapClient.Logger.Log($"Bypass list:");
                        foreach (String fac in serverSettings.SupportBypass)
                        {
                            PersistentMapClient.Logger.Log($"Bypass is active for: {fac}");
                        }
                        PersistentMapClient.Logger.Log($"Capitals:");
                        foreach (String fac in serverSettings.Capitals.Keys)
                        {
                            PersistentMapClient.Logger.Log($"Faction: {fac}, Capital: {serverSettings.Capitals[fac]}");
                        }
                        Helper.updateCaptials(serverSettings.Capitals);
                    }
                    nextRefresh = DateTime.UtcNow.AddMinutes(15);
                }
                catch (Exception e)
                {
                    PersistentMapClient.Logger.LogError(e);
                }
            }
        }

        // Pulls down messages from your faction, 
        public static HpgMail GetHpgMail()
        {
            try
            {

                HttpWebRequest request = new RequestBuilder(WarService.GetHpgMail).Build();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                HpgMail mail = new HpgMail();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream);
                    string jdata = reader.ReadToEnd();
                    mail = JsonConvert.DeserializeObject<HpgMail>(jdata);
                }
                return mail;
            }
            catch (Exception e)
            {
                PersistentMapClient.Logger.LogError(e);
                return new HpgMail();
            }
        }


        // Pulls the inventory for the specified faction
        public static List<ShopDefItem> GetShopForFaction(FactionValue faction, bool blackMarket) {
            try {

                WarService market = WarService.GetFactionShop;
                if (blackMarket)
                {
                    market = WarService.GetBlackMarket;
                }
                HttpWebRequest request = new RequestBuilder(market).Faction(faction).Build();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                List<ShopDefItem> items;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string itemsstring = reader.ReadToEnd();
                    items = JsonConvert.DeserializeObject<List<ShopDefItem>>(itemsstring);
                }
                return items;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return null;
            }
        }

        // Send any salvage the user didn't want to the faction inventory
        public static bool PostUnusedSalvage(List<SalvageDef> ___finalPotentialSalvage, FactionValue faction) {
            List<ShopItem> items = new List<ShopItem>();
            foreach (SalvageDef salvage in ___finalPotentialSalvage) {
                ShopItem item = new ShopItem();
                item.ID = salvage.Description.Id;
                item.UiName = salvage.Description.UIName;
                switch (salvage.ComponentType) {
                    case ComponentType.AmmunitionBox: {
                            item.Type = ShopItemType.AmmunitionBox;
                            break;
                        }
                    case ComponentType.HeatSink: {
                            item.Type = ShopItemType.HeatSink;
                            break;
                        }
                    case ComponentType.JumpJet: {
                            item.Type = ShopItemType.JumpJet;
                            break;
                        }
                    case ComponentType.MechPart: {
                            item.Type = ShopItemType.MechPart;
                            break;
                        }
                    case ComponentType.Upgrade: {
                            item.Type = ShopItemType.Upgrade;
                            break;
                        }
                    case ComponentType.Weapon: {
                            item.Type = ShopItemType.Weapon;
                            break;
                        }
                }
                item.Count = 1;
                items.Add(item);
            }
            if (items.Count > 0) {
                string testjson = JsonConvert.SerializeObject(items);
                HttpWebRequest request = new RequestBuilder(WarService.PostSalvage).Faction(faction).PostData(testjson).Build();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                }
            }
            return true;
        }

        // Anything the user sells goes into faction inventory as well.
        public static bool PostSoldItems(Dictionary<string, ShopDefItem> soldItems, FactionValue faction) {
            List<ShopItem> items = new List<ShopItem>();
            foreach (ShopDefItem soldItem in soldItems.Values.ToList<ShopDefItem>())
            {
                SalvageDef salvage = new SalvageDef();
                soldItem.ToSalvageDef(ref salvage);
                if (salvage != null)
                {
                    ShopItem item = new ShopItem();
                    item.ID = salvage.Description.Id;
                    item.UiName = salvage.Description.UIName;
                    switch (salvage.ComponentType)
                    {
                        case ComponentType.AmmunitionBox:
                            {
                                item.Type = ShopItemType.AmmunitionBox;
                                break;
                            }
                        case ComponentType.HeatSink:
                            {
                                item.Type = ShopItemType.HeatSink;
                                break;
                            }
                        case ComponentType.JumpJet:
                            {
                                item.Type = ShopItemType.JumpJet;
                                break;
                            }
                        case ComponentType.MechPart:
                            {
                                item.Type = ShopItemType.MechPart;
                                break;
                            }
                        case ComponentType.Upgrade:
                            {
                                item.Type = ShopItemType.Upgrade;
                                break;
                            }
                        case ComponentType.Weapon:
                            {
                                item.Type = ShopItemType.Weapon;
                                break;
                            }
                    }
                    item.Count = 1;
                    items.Add(item);
                }
            }
            string testjson = JsonConvert.SerializeObject(items);
            HttpWebRequest request = new RequestBuilder(WarService.PostSoldItems).Faction(faction).PostData(testjson).Build();
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream);
                string mapstring = reader.ReadToEnd();
                return true;
            }
        }

        // Send a list of items to purchase from the faction store
        public static bool PostBuyItems(Dictionary<string, PurchasedItem> sold, FactionValue owner, bool blackMarket) {
            try {
                if (sold == null || owner == null)
                {
                    PersistentMapClient.Logger.LogIfDebug("null owner or dictionary");
                    return false;
                }
                if (sold.Count() > 0)
                {
                    WarService market = WarService.PostBuyItems;
                    if (blackMarket)
                    {
                        market = WarService.PostBuyBlackMarketItem;
                    }
                    string testjson = JsonConvert.SerializeObject(sold.Values.ToList<PurchasedItem>());
                    HttpWebRequest request = new RequestBuilder(market).Faction(owner).PostData(testjson).Build();
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream);
                        string pitemString = reader.ReadToEnd();
                        if (blackMarket)
                        {
                            PurchasedItem pItem;
                            pItem = JsonConvert.DeserializeObject<PurchasedItem>(pitemString);
                            PersistentMapClient.updateBMarketId(pItem.TransactionId);

                        }

                        return true;
                    }
                }
                else
                {
                    PersistentMapClient.Logger.Log("No online items purchased, nothing to do");
                    return true;
                }
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
        }

        // Fetch the current state of the starmap
        public static StarMap GetStarMap() {
            RefreshServerSettings(true);
            try {
                HttpWebRequest request = new RequestBuilder(WarService.GetStarMap).Build();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                watch.Stop();
                PersistentMapClient.Logger.LogIfDebug($"GetStarMap took: {watch.ElapsedMilliseconds}ms.");
                StarMap map;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                    map = JsonConvert.DeserializeObject<StarMap>(mapstring);
                }
                return map;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return null;
            }
        }

        // Send the results of a mission to the server
        public static bool PostMissionResult(Objects.MissionResult mresult, string companyName, bool warmission, out string errorText) {
            errorText = "No Error";
            try {
                WarService post = WarService.PostNonWarResult;
                if (warmission)
                {
                    post = WarService.PostMissionResult;
                    PersistentMapClient.Logger.Log($"Post as war mission");
                }
                else
                {
                    PersistentMapClient.Logger.Log($"Post as non-war mission");
                }
                string testjson = JsonConvert.SerializeObject(mresult);           
                HttpWebRequest request = new RequestBuilder(post).CompanyName(companyName).PostData(testjson).Build();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                watch.Stop();                
                PersistentMapClient.Logger.LogIfDebug($"PostMissionResult took: {watch.ElapsedMilliseconds}ms.");
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    PersistentMapClient.Logger.Log($"PostMissionResult Failed Code: {response.StatusCode}");
                    errorText = "Unknown Error";
                    return false;
                }
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                }
                return true;
            }
            catch (WebException e)
            {
                switch(((HttpWebResponse)e.Response).StatusCode)
                {
                    case HttpStatusCode.InternalServerError:
                        errorText = "Internal Server, please make a discord ticket!";
                        break;
                    case HttpStatusCode.NotFound:
                        errorText = "Connection Error, Try again later";
                        break;
                    case HttpStatusCode.BadRequest:
                        errorText = "Improper client code, please make a discord ticket and provide this save";
                        break;
                    case HttpStatusCode.Conflict:
                        errorText = "This career is from a not current season career and cannot particpate on the war map";
                        break;
                    case HttpStatusCode.Forbidden:
                        errorText = "This career has been banned from particpating on the war map, contact a roguewar moderator or admin for additional details, If you believe this is in error you can appeal your ban by opening a Support Ticket on the main RogueTech Discord";
                        break;
                    case HttpStatusCode.ExpectationFailed:
                        errorText = "Cooldown is in affect, results not posted";
                        break;
                    case HttpStatusCode.PreconditionFailed:
                        HttpWebResponse response = (HttpWebResponse)e.Response;
                        string cooldown = "";
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(responseStream);
                            string errDat = reader.ReadToEnd();
                            Dictionary<string, string> errDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(errDat);
                            cooldown = errDict["Lockout"];
                        }
                        errorText = $"Running multiple saves on a single career is not allowed, if you are rolling back a save (because of a crash or the like) you will need to wait {cooldown} for cooldown to expire before you can continue posting results";
                        break;
                    case HttpStatusCode.RequestedRangeNotSatisfiable:
                        errorText = "Client transmission error, could not determine star system";
                        break;
                    case HttpStatusCode.NotAcceptable:
                        errorText = "Mission results looked suspicious, results not counted, if you believe this is in error, please make a ticket on discord";
                        break;
                    case (HttpStatusCode)418:
                        errorText = "Contract type has not be assigned a value, please contact roguewar admins";
                        break;
                    case (HttpStatusCode)444:
                        errorText = "System is surrounded, no control change possible";
                        break;
                    case (HttpStatusCode)472:
                        errorText = "Client is out of date, you must update RogueTech to continue participating on the map";
                        break;
                    case (HttpStatusCode)473:
                        errorText = "Critical mission data not available, if you are Mac or Linux please file a support ticket on discord. If on Windows restart RogueTech";
                        break;
                    case (HttpStatusCode)474:
                        errorText = "Unknown Client version, please make sure RogueTech is up to date and make a ticket if this still occurs";
                        break;
                    case (HttpStatusCode)475:
                        errorText = "Mission results could not be posted because a seasonal break is in affect, check the roguewar discord for announcements of when this break will end and mission posts can resume";
                        break;
                    default:
                        errorText = "Unknown Error, your install may be out of date, if not make a discord ticket";
                        break;
                }
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
            catch (Exception e) {
                
                PersistentMapClient.Logger.LogError(e);
                errorText = "Client Error, consider making a ticket!";
                return false;
            }
        }

        // Builder class that encapsulates all the common functions of making a request to the REST API
        private class RequestBuilder {

            private string _requestUrl;
            private string _requestMethod;
            private string _faction;
            private string _companyName;
            private string _postJSON;
            private readonly WarService _service;

            public RequestBuilder(WarService service) {
                this._service = service;
            }

            public RequestBuilder Faction(FactionValue faction) {
                _faction = faction.Name;
                return this;
            }

            public RequestBuilder CompanyName(string companyName) {
                _companyName = companyName;
                return this;
            }

            public RequestBuilder PostData(string postJSON) {
                _postJSON = postJSON;
                return this;
            }

            public HttpWebRequest Build() {
                switch (_service) {
                    case WarService.PostBuyItems:
                        _requestUrl = $"{Fields.settings.ServerURL}api/rogueshopservices/purchasefromshop/{_faction}";
                        _requestMethod = "POST";
                        break;
                    case WarService.PostMissionResult:
                        _requestUrl = $"{Fields.settings.ServerURL}{Web.postUrl}";
                        _requestMethod = "POST";
                        break;
                    case WarService.PostNonWarResult:
                        _requestUrl = $"{Fields.settings.ServerURL}{Web.nonWarUrl}";
                        _requestMethod = "POST";
                        break;
                    case WarService.PostSalvage:
                        _requestUrl = $"{Fields.settings.ServerURL}api/rogueshopservices/postsalvage/{_faction}";
                        _requestMethod = "POST";
                        break;
                    case WarService.PostSoldItems:
                        _requestUrl = $"{Fields.settings.ServerURL}api/rogueshopservices/postsolditems/{_faction}";
                        _requestMethod = "POST";
                        break;
                    case WarService.GetFactionShop:
                        _requestUrl = $"{Fields.settings.ServerURL}api/rogueshopservices/getshop/{_faction}";
                        _requestMethod = "GET";
                        break;
                    case WarService.GetServerSettings:
                        _requestUrl = $"{Fields.settings.ServerURL}api/roguesettingservice/getsettings";
                        _requestMethod = "GET";
                        break;
                    case WarService.GetBlackMarket:
                        _requestUrl = $"{Fields.settings.ServerURL}api/rogueshopservices/getblackmarketshop/{_faction}";
                        _requestMethod = "GET";
                        break;
                    case WarService.PostBuyBlackMarketItem:
                        _requestUrl = $"{Fields.settings.ServerURL}api/rogueshopservices/purchasefromblackmarketshop/{_faction}";
                        _requestMethod = "POST";
                        break;
                    case WarService.GetHpgMail:
                        _requestUrl = $"{Fields.settings.ServerURL}api/rogueevents/hpgmessages";
                        _requestMethod = "GET";
                        break;
                    case WarService.GetStarMap:
                    default:
                        _requestUrl = $"{Fields.settings.ServerURL}api/roguewarservices/getmap";
                        _requestMethod = "GET";
                        break;
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_requestUrl);
                
                request.AutomaticDecompression = DecompressionMethods.GZip;
                if (Fields.settings.allowSelfSignedCert)
                {
                    request.ServerCertificateValidationCallback = (sender, certificate, chain, policyErrors) => { return true; };
                }
                request.AllowAutoRedirect = true;
                request.Method = _requestMethod;
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 30000; // 30s connection timeout
                string clientId = PersistentMapClient.getClientPostId();
                request.Headers["X-RT-CLIENT"] =  clientId;
                request.Headers["X-RT-CLIENT-VERSION"] = PersistentMapClient.ClientVersion;
                request.Headers["X-RT-ISTATE"] = iState.ToString();

                if (_postJSON != null) {
                    // TODO: Why are we ASCII encoding instead of UTF-8?
                    byte[] testarray = Encoding.UTF8.GetBytes(_postJSON);
                    request.ContentLength = testarray.Length;

                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(testarray, 0, testarray.Length);
                    dataStream.Close();
                }

                

                return request;
            }
        }
    }
}
