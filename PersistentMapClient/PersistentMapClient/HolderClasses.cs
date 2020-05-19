﻿using BattleTech;
using BattleTech.Framework;
using System;
using System.Collections.Generic;

namespace PersistentMapClient {

    public class Settings {
        public string ServerURL = "http://localhost:8000/";
        public bool debug = false;
        public float priorityContactPayPercentage = 2f;
        public int priorityContractsPerAlly = 2;
        public string ClientID = "";
        public float activePlayerMarkerSize = 3.5f;
        public string Season = "Debug01";

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

        public static List<string> excludedFactions = new List<string>() { "AuriganMercenaries", "Betrayers", "MagistracyCentrella",
            "MajestyMetals", "MercenaryReviewBoard", "Nautilus", "NoFaction", "FlakJackals", "LocalsBrockwayRefugees",
            "SelfEmployed", "MasonsMarauders", "SteelBeast", "KellHounds", "RazorbackMercs", "HostileMercenaries" };
        public static PersistentMapAPI.StarMap currentMap;

        public static Dictionary<Faction, List<ShopDefItem>> currentShops = new Dictionary<Faction, List<ShopDefItem>>();
        public static KeyValuePair<Faction, List<ShopDefItem>> currentShopSold =
            new KeyValuePair<Faction, List<ShopDefItem>>(Faction.INVALID_UNSET, new List<ShopDefItem>());
        public static KeyValuePair<Faction, List<string>> currentShopBought =
            new KeyValuePair<Faction, List<string>>(Faction.INVALID_UNSET, new List<string>());
        public static Dictionary<string, DateTime> LastUpdate = new Dictionary<string, DateTime>();
        public static int UpdateTimer = 15;


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