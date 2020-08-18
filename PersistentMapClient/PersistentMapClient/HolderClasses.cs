using BattleTech;
using BattleTech.Framework;
using System;
using System.Collections.Generic;
using PersistentMapAPI;

namespace PersistentMapClient {

    public class Settings {
        public string ServerURL = "http://localhost:8000/";
        public bool allowSelfSignedCert = false;
        public bool debug = false;
        public float priorityContactPayPercentage = 2f;
        public int priorityContractsPerAlly = 2;
        public string ClientID = "";
        public float activePlayerMarkerSize = 3.5f;
        public string Season = "Debug01";
        public bool SortByRealDifficulty = false;
        public float percentageOfTravelOnBorder = 0.5f;
        public bool warBorders = false;
        public List<string> immuneSystemEnemies = new List<string> {"Locals", "AuriganPirates" };
        public List<string> cannotBeTarget = new List<string>() { "Solaris7" };

        public override string ToString() {
            return $"ServerURL:({ServerURL}) ClientID:({ClientID}) Debug:({debug}) PriorityContractPay%:({priorityContactPayPercentage}) PriorityContractsPerAlly:{priorityContractsPerAlly} ActivePlayerMarkerSize:({activePlayerMarkerSize})";
        }
    }

    public class GeneratedSettings {
        public string ClientID = "";
    }

    public static class Fields {
        public static Settings settings;
        public static Dictionary<string, string> FluffDescriptions = new Dictionary<string, string>();
        public static bool cheater = false;
        public static bool skipmission = false;
        public static bool firstpass = true;
        public static bool warmission = false;
        public static string ShopFileTag = "rt_economy";
        public static bool canPostSalvage = false;
        public static int currBorderCons = 0;
        public static bool needsToReloadEnemies = false;
        public static List<string> immuneFromWar = new List<string>();
        public static List<string> enemyHolder = new List<string>();
        public static List<string> excludedFactions = new List<string>() { "AuriganMercenaries", "Betrayers", "MagistracyCentrella",
            "MajestyMetals", "MercenaryReviewBoard", "Nautilus", "NoFaction", "FlakJackals", "LocalsBrockwayRefugees",
            "SelfEmployed", "MasonsMarauders", "SteelBeast", "KellHounds", "RazorbackMercs", "HostileMercenaries" };
        public static PersistentMapAPI.StarMap currentMap;

        public static FactionValue currentShopOwner;
        public static Dictionary<string, PurchasedItem> shopItemsSold = new Dictionary<string, PurchasedItem>();
        public static Dictionary<string, ShopDefItem> shopItemsPosted = new Dictionary<string, ShopDefItem>();


        //prioFields
        public static bool prioGen = false;
        public static FactionValue prioEmployer = FactionEnumeration.GetInvalidUnsetFactionValue();
        public static FactionValue prioTarget = FactionEnumeration.GetInvalidUnsetFactionValue();
        public static FactionValue prioThird = FactionEnumeration.GetInvalidUnsetFactionValue();

        public struct PotentialContract {
            public ContractOverride contractOverride;
            public Faction employer;
            public Faction target;
            public int difficulty;
        }
    }

}