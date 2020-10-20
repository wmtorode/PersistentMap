using Harmony;
using Newtonsoft.Json;
using System;
using System.Reflection;
using BattleTech;
using CustomShops;
using PersistentMapClient.shops;

namespace PersistentMapClient {

    public class PersistentMapClient {

        public static readonly string CLIENT_ID_STAT = "Pm_ClientId";
        public static readonly string CAREER_ID_STAT = "Pm_CareerId";
        public static readonly string MISSION_COUNT_STAT = "Pm_SuccessfulPostCount";
        public static readonly string SEASON_STAT = "Pm_PlaySeasonNumber";
        public const string ClientVersion = "4.0.0-2";

        internal static Logger Logger;
        internal static string ModDirectory;
        internal static StatCollection companyStats;
        internal static OnlineShop shop;

        public static void Init(string directory, string settingsJSON) {
            ModDirectory = directory;

            Exception settingsE = null;
            try {
                Fields.settings = Helper.LoadSettings();

            } catch (Exception e) {
                settingsE = e;
                Fields.settings = new Settings();
            }

            // Add a hook to dispose of logging on shutdown
            Logger = new Logger(directory, "persistent_map_client", Fields.settings.debug);
            System.AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Logger.Close();

            if (settingsE != null) {
                Logger.Log($"Using default settings due to exception reading settings: {settingsE.Message}");
            }

            
            Logger.LogIfDebug($"Settings are:({Fields.settings.ToString()})");

            /* Read the ClientID from a location that is persistent across installs. 
               Everything under /mods is wiped out during RT installs. Instead we write 
               to Battletech/ModSaves/PersistentMapClient to allow it to persist across installs */
            if (Fields.settings.ClientID == null || Fields.settings.ClientID.Equals("")) {
                Helper.FetchClientID(directory);
            } else {
                // We were passed an ID by the test harness. Do nothing.
                Logger.Log("Test harness passed a clientID, skipping.");
            }
            
            var harmony = HarmonyInstance.Create("de.morphyum.PersistentMapClient");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            shop = new OnlineShop();
            Control.RegisterShop(shop, new string[] {"systemchange", "ContractComplete", "OwnerChange" });
        }

        // Used for Unit Tests only
        public static void Dispose() {
            Logger.Close();
        }

        public static void setCompanyStats(StatCollection stats)
        {
            companyStats = stats;


            if (!companyStats.ContainsStatistic(CLIENT_ID_STAT)) { companyStats.AddStatistic(CLIENT_ID_STAT, Fields.settings.ClientID); };
            if (!companyStats.ContainsStatistic(CAREER_ID_STAT)) 
            {
                Guid careerId = Guid.NewGuid();
                companyStats.AddStatistic(CAREER_ID_STAT, careerId.ToString()); 
            }
            if (!companyStats.ContainsStatistic(SEASON_STAT)) { companyStats.AddStatistic(SEASON_STAT, Fields.settings.Season); };
            if (!companyStats.ContainsStatistic(MISSION_COUNT_STAT)) { companyStats.AddStatistic(MISSION_COUNT_STAT, 0); };

            Logger.Log($"Career ID Loaded: {companyStats.GetValue<string>(CAREER_ID_STAT)}");
        }

        public static string getClientPostId()
        {
            if (companyStats == null)
            {
                Logger.Log("null company Stats, cannot send data to server!");
                return "";
            }
            string clientPostId = $"{companyStats.GetValue<string>(CLIENT_ID_STAT)}{companyStats.GetValue<string>(CAREER_ID_STAT)}{companyStats.GetValue<string>(SEASON_STAT)}";
            return clientPostId;
        }

        public static int getMissionCount()
        {
            if (companyStats == null)
            {
                return 0;
            }
            return companyStats.GetValue<int>(MISSION_COUNT_STAT);
        }

        public static void incrementMissionCount()
        {
            if (companyStats != null)
            {
                int currentValue = companyStats.GetValue<int>(MISSION_COUNT_STAT);
                currentValue++;
                companyStats.Set<int>(MISSION_COUNT_STAT, currentValue);
            }
        }
    }
}
