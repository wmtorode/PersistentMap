using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using Harmony;
using HBS.Collections;
using Newtonsoft.Json;
using PersistentMapClient.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PersistentMapClient {

    public class SaveFields {

    }

    public class Helper {
        public const string GeneratedSettingsFile = "generatedSettings.json";

        public static Settings LoadSettings() {
            string _settingsPath = $"{ PersistentMapClient.ModDirectory}/settings.json";
            try {
                // Load the settings file
                Settings settings = null;
                using (StreamReader r = new StreamReader(_settingsPath)) {
                    string json = r.ReadToEnd();
                    settings = JsonConvert.DeserializeObject<Settings>(json);
                }
                return settings;
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        public static double GetDistanceInLY(float x1, float y1, float x2, float y2) {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        public static FactionValue getfaction(string faction) {
            return FactionEnumeration.GetFactionByName(faction);
        }

        public static bool MeetsNewReqs(StarSystem instance, TagSet reqTags, TagSet exTags, TagSet curTags) {
            try {
                if (!curTags.ContainsAny(exTags, false)) {
                    //Check exclution for time and rep
                    foreach (string item in exTags) {
                        if (item.StartsWith("time")) {
                            string[] times = item.Split('_');
                            if ((instance.Sim.DaysPassed >= int.Parse(times[1]))) {
                                return false;
                            }
                        }
                        else if (item.StartsWith("rep")) {
                            string[] reps = item.Split('_');
                            int test = instance.Sim.GetRawReputation(Helper.getfaction(reps[1]));
                            if ((test >= int.Parse(reps[2]))) {
                                return false;
                            }
                        }
                    }

                    //Check requirements for time and rep
                    foreach (string item in reqTags) {
                        if (!curTags.Contains(item)) {
                            if (item.StartsWith("time")) {
                                string[] times = item.Split('_');
                                if (!(instance.Sim.DaysPassed >= int.Parse(times[1]))) {
                                    return false;
                                }
                            }
                            else if (item.StartsWith("rep")) {
                                string[] reps = item.Split('_');
                                int test = instance.Sim.GetRawReputation(Helper.getfaction(reps[1]));
                                if (!(test >= int.Parse(reps[2]))) {
                                    return false;
                                }
                            }
                            else {
                                return false;
                            }
                        }
                    }
                    return true;
                }
                return false;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
        }

        // Read the clientID (a GUID) from a place that should persist across installs.
        public static void FetchClientID(string modDirectoryPath) {
            // Starting path should be battletech\mods\PersistMapClient
            string[] directories = modDirectoryPath.Split(Path.DirectorySeparatorChar);
            DirectoryInfo modsDir = Directory.GetParent(modDirectoryPath);
            DirectoryInfo battletechDir = modsDir.Parent;

            // We want to write to Battletech/ModSaves/PersistentMapClient directory
            DirectoryInfo modSavesDir = battletechDir.CreateSubdirectory("ModSaves");
            DirectoryInfo clientDir = modSavesDir.CreateSubdirectory("PersistentMapClient");

            // Finally see if the file exists
            FileInfo GeneratedSettingsFile = new FileInfo(Path.Combine(clientDir.FullName, Helper.GeneratedSettingsFile));
            if (GeneratedSettingsFile.Exists) {
                // Attempt to read the file                
                try {
                    GeneratedSettings generatedSettings = null;
                    using (StreamReader r = new StreamReader(GeneratedSettingsFile.FullName)) {
                        string json = r.ReadToEnd();
                        generatedSettings = JsonConvert.DeserializeObject<GeneratedSettings>(json);
                    }
                    Fields.settings.ClientID = generatedSettings.ClientID;
                    PersistentMapClient.Logger.Log($"Fetched clientID:({Fields.settings.ClientID}).");
                }
                catch (Exception e) {
                    PersistentMapClient.Logger.Log($"Failed to read clientID from {GeneratedSettingsFile}, will overwrite!");
                    PersistentMapClient.Logger.LogError(e);
                }
            }
            else {
                PersistentMapClient.Logger.Log($"GeneratedSettings file at path:{GeneratedSettingsFile.FullName} does not exist, will be created.");
            }

            // If the clientID hasn't been written at this point, something went wrong. Generate a new one.
            if (Fields.settings.ClientID == null || Fields.settings.ClientID.Equals("")) {
                Guid clientID = Guid.NewGuid();
                try {
                    GeneratedSettings newSettings = new GeneratedSettings {
                        ClientID = clientID.ToString()
                    };
                    using (StreamWriter writer = new StreamWriter(GeneratedSettingsFile.FullName, false)) {
                        string json = JsonConvert.SerializeObject(newSettings);
                        writer.Write(json);
                    }
                    Fields.settings.ClientID = clientID.ToString();
                    PersistentMapClient.Logger.Log($"Wrote new clientID ({Fields.settings.ClientID}) to generatedSettings at:{GeneratedSettingsFile.FullName}.");
                }
                catch (Exception e) {
                    PersistentMapClient.Logger.Log("FATAL ERROR: Failed to write clientID, cannot continue!");
                    PersistentMapClient.Logger.LogError(e);
                    // TODO: Figure out a failure strategy...
                }
            }
        }

        public static string GetFactionTag(FactionValue faction) {
            try {
                return "planet_faction_" + faction.Name.ToLower();
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return null;
            }
        }

        public static bool IsBorder(StarSystem system, SimGameState Sim) {
            try {
                bool result = false;
                if (Sim.Starmap != null) {
                    if (system.OwnerValue != FactionEnumeration.GetNoFactionValue()) {
                        foreach (StarSystem neigbourSystem in Sim.Starmap.GetAvailableNeighborSystem(system)) {
                            if (system.OwnerValue != neigbourSystem.OwnerValue && neigbourSystem.OwnerValue != FactionEnumeration.GetNoFactionValue()) {
                                result = true;
                                break;
                            }
                        }
                    }
                }
                if (!result && capitalsBySystemName.Contains(system.Name) && !IsCapital(system, system.OwnerValue.Name)) {
                    result = true;
                }
                return result;
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return false;
            }
        }

        public static bool IsWarBorder(StarSystem system, SimGameState Sim)
        {
            try
            {
                bool result = false;
                if (Sim.Starmap != null)
                {
                    if (system.OwnerValue.Name != FactionEnumeration.GetNoFactionValue().Name)
                    {
                        foreach (StarSystem neigbourSystem in Sim.Starmap.GetAvailableNeighborSystem(system))
                        {
                            if (system.OwnerDef.Enemies.Contains(neigbourSystem.OwnerValue.Name) && neigbourSystem.OwnerValue.Name != FactionEnumeration.GetNoFactionValue().Name)
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                PersistentMapClient.Logger.LogError(ex);
                return false;
            }
        }

        public static bool IsRandomTravelBorder(StarSystem system, SimGameState Sim)
        {
            try
            {
                bool result = false;
                if (Sim.Starmap != null)
                {
                    foreach (StarSystem neigbourSystem in Sim.Starmap.GetAvailableNeighborSystem(system))
                    {
                        if (system.OwnerDef.ID != neigbourSystem.OwnerDef.ID)
                        {
                            result = true;
                            break;
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                PersistentMapClient.Logger.LogError(ex);
                return false;
            }
        }

        public static StarSystem ChangeWarDescription(StarSystem system, SimGameState Sim, Objects.System warsystem) {
            try {
                //if (IsBorder(system, Sim)) {
                    List<string> factionList = new List<string>();
                    if (!Fields.FluffDescriptions.ContainsKey(system.Name))
                    {
                        Fields.FluffDescriptions.Add(system.Name, system.Def.Description.Details);
                    }
                    if (warsystem.isInsurrect())
                    {
                    factionList.Add("<b><color=#de0202>System is Insurrect</color></b>\n");
                    }

                    if (warsystem.hasOnlineEvent())
                    {
                        factionList.Add("<b><color=#8904B1>Online event target</color></b>\n");
                    }
                    if (warsystem.generatesItems)
                    {
                        factionList.Add("<b><color=#088A08>Generates Items:</color></b>");
                        foreach (string item in warsystem.itemsGenerated)
                        {
                            factionList.Add($"<b><color=#088A08>{item}</color></b>");
                        }

                        factionList.Add("\n");
                    }
                factionList.Add(Fields.FluffDescriptions[system.Name]);
                    factionList.Add("\nCurrent Control:");
                    foreach (FactionControl fc in warsystem.factions) {
                        if (fc.control != 0) {
                            factionList.Add(GetFactionName(fc.Name) + $": Companies: {fc.ActivePlayers}, Control: {fc.control}%");
                        }
                    }
                    
                    AccessTools.Method(typeof(DescriptionDef), "set_Details").Invoke(system.Def.Description, new object[] { string.Join("\n", factionList.ToArray()) });
                //}
                //else if (Fields.FluffDescriptions.ContainsKey(system.Name)) {
                //    AccessTools.Method(typeof(DescriptionDef), "set_Details").Invoke(system.Def.Description, new object[] { Fields.FluffDescriptions[system.Name] });
                //    Fields.FluffDescriptions.Remove(system.Name);
                //}
                return system;
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        public static string GetFactionName(string faction) {
            try {
                return FactionEnumeration.GetFactionByName(faction).FactionDef.Name.Replace("the ", "").Replace("The ", "");
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        public static double GetDistanceInLY(StarSystem currPosition, Objects.System target, List<StarSystem> allSystems) {
            try {
                StarSystem targetSystem = allSystems.FirstOrDefault(x => x.Name.Equals(target.name));
                return Math.Sqrt(Math.Pow(targetSystem.Position.x - currPosition.Position.x, 2) + Math.Pow(targetSystem.Position.y - currPosition.Position.y, 2));
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return 0;
            }
        }

        public static double GetDistanceInLY(StarSystem currPosition, StarSystem targetSystem) {
            try {
                return Math.Sqrt(Math.Pow(targetSystem.Position.x - currPosition.Position.x, 2) + Math.Pow(targetSystem.Position.y - currPosition.Position.y, 2));
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return 0;
            }
        }

        public static List<string> GetEmployees(StarSystem system, SimGameState Sim) {
            try {
                List<string> employees = new List<string>();
                if (Sim.Starmap != null) {
                    // If a faction owns the planet, add the owning faction and local government
                    if (system.OwnerValue != FactionEnumeration.GetNoFactionValue()) {
                        employees.Add(FactionEnumeration.GetFactionByName("Locals").Name);
                        if (system.OwnerValue != FactionEnumeration.GetFactionByName("Locals")) {
                            employees.Add(system.OwnerValue.Name);
                        }
                    }

                    // Look across neighboring systems, and add employees of factions that border this system
                    List<FactionValue> distinctNeighbors = Sim.Starmap.GetAvailableNeighborSystem(system)
                        .Select(s => s.OwnerValue)
                        .Where(f => f != FactionEnumeration.GetNoFactionValue() && f != system.OwnerValue && f != FactionEnumeration.GetFactionByName("Locals"))
                        .Distinct()
                        .ToList();
                    foreach (FactionValue neighbour in distinctNeighbors)
                        {
                        employees.Add(neighbour.Name);
                        }

                    // If a capital is occupied, add the faction that originally owned the capital to the employer list
                    if (Helper.capitalsBySystemName.Contains(system.Name)) {
                        string originalCapitalFaction = Helper.capitalsBySystemName[system.Name].First();
                        if (!employees.Contains(FactionEnumeration.GetFactionByName(originalCapitalFaction).Name)) {
                            employees.Add(FactionEnumeration.GetFactionByName(originalCapitalFaction).Name);
                        }
                    }
                }
                return employees;
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        public static List<string> GetTargets(StarSystem system, SimGameState Sim) {
            try {
                List<string> targets = new List<string>();
                if (Sim.Starmap != null) {
                    targets.Add(FactionEnumeration.GetAuriganPiratesFactionValue().Name);
                    if (system.OwnerValue != FactionEnumeration.GetNoFactionValue()) {
                        if (system.OwnerValue != FactionEnumeration.GetFactionByName("Locals")) {
                            targets.Add(system.OwnerValue.Name);
                        }
                        targets.Add(FactionEnumeration.GetFactionByName("Locals").Name);
                    }
                    foreach (StarSystem neigbourSystem in Sim.Starmap.GetAvailableNeighborSystem(system)) {
                        if (system.OwnerValue != neigbourSystem.OwnerValue && !targets.Contains(neigbourSystem.OwnerValue.Name) && neigbourSystem.OwnerValue != FactionEnumeration.GetNoFactionValue()) {
                            targets.Add(neigbourSystem.OwnerValue.Name);
                        }
                    }

                }
                else {
                    foreach (FactionValue faction in FactionEnumeration.FactionList) {
                        targets.Add(faction.Name);
                    }
                }
                return targets;
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        // Capitals by faction
        private static Dictionary<string, string> capitalsByFaction = new Dictionary<string, string> {
            { "Kurita", "Luthien" },
            { "Davion", "New Avalon" },
            { "Liao", "Sian" },
            { "Marik", "Atreus (FWL)" },
            { "Rasalhague", "Rasalhague" },
            { "Ives", "St. Ives" },
            { "Oberon", "Oberon" },
            { "TaurianConcordat", "Taurus" },
            { "MagistracyOfCanopus", "Canopus" },
            { "Outworld", "Alpheratz" },
            { "Circinus", "Circinus" },
            { "Marian", "Alphard (MH)" },
            { "Lothian", "Lothario" },
            { "AuriganRestoration", "Coromodir" },
            { "Steiner", "Tharkad" },
            { "ComStar", "Terra" },
            { "Castile", "Asturias" },
            { "Chainelane", "Far Reach" },
            { "ClanBurrock", "Albion (Clan)" },
            { "ClanCloudCobra", "Zara (Homer 2850+)" },
            { "ClanCoyote", "Tamaron" },
            { "ClanDiamondShark", "Strato Domingo" },
            { "ClanFireMandrill", "Shadow" },
            { "ClanGhostBear", "Arcadia (Clan)" },
            { "ClanGoliathScorpion", "Dagda (Clan)" },
            { "ClanHellsHorses", "Kirin" },
            { "ClanIceHellion", "Hector" },
            { "ClanJadeFalcon", "Ironhold" },
            { "ClanNovaCat", "Barcella" },
            { "ClansGeneric", "Strana Mechty" },
            { "ClanSmokeJaguar", "Huntress" },
            { "ClanSnowRaven", "Lum" },
            { "ClanStarAdder", "Sheridan (Clan)" },
            { "ClanSteelViper", "New Kent" },
            { "ClanWolf", "Tiber (Clan)" },
            { "Delphi", "New Delphi" },
            { "Elysia", "Blackbone (Nyserta 3025+)" },
            { "Hanse", "Bremen (HL)" },
            { "JarnFolk", "Trondheim (JF)" },
            { "Tortuga", "Tortuga Prime" },
            { "Valkyrate", "Gotterdammerung" },
            { "Axumite", "Thala" },
            { "WordOfBlake", "EC3040-B42A" },
            {"Illyrian", "Illyria" }
        };

        private static ILookup<string, string> capitalsBySystemName = capitalsByFaction.ToLookup(pair => pair.Value, pair => pair.Key);
        public static bool IsCapital(StarSystem system, string faction) {
            bool isCapital = false;
            try {
                if (capitalsBySystemName.Contains(system.Name)) {
                    string systemFaction = capitalsBySystemName[system.Name].First();
                    isCapital = (systemFaction == faction);
                }
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
            }
            return isCapital;
        }

        public static int CalculatePlanetSupport(SimGameState Sim, StarSystem attackSystem, FactionValue attacker, FactionValue defender) {
            int support = 0;
            PersistentMapClient.Logger.Log("Calculating planet support");
            List<StarSystem> neighbours = new List<StarSystem>();
            foreach (StarSystem possibleSystem in Sim.StarSystems) {
                if (GetDistanceInLY(attackSystem, possibleSystem) <= Sim.Constants.Travel.MaxJumpDistance && !possibleSystem.Name.Equals(attackSystem.Name)) {
                    neighbours.Add(possibleSystem);
                }
            }
            if (attackSystem.OwnerValue == attacker) {
                if (IsCapital(attackSystem, attacker.Name)) {
                    support += 10;
                }
                else {
                    support++;
                }
            }
            else if (attackSystem.OwnerValue == defender) {
                if (IsCapital(attackSystem, defender.Name)) {
                    support -= 10;
                }
                else {
                    support--;
                }
            }

            foreach (StarSystem neigbourSystem in neighbours) {
                if (neigbourSystem.OwnerValue == attacker) {
                    if (IsCapital(neigbourSystem, attacker.Name)) {
                        support += 10;
                    }
                    else {
                        support++;
                    }
                }
                else if (neigbourSystem.OwnerValue == defender) {
                    if (IsCapital(neigbourSystem, defender.Name)) {
                        support -= 10;
                    }
                    else {
                        support--;
                    }
                }
            }
            return support;
        }

        private static readonly ContractType[] prioTypes = new ContractType[]
        {
            ContractType.AmbushConvoy,
            ContractType.Assassinate,
            ContractType.CaptureBase,
            ContractType.CaptureEscort,
            ContractType.DefendBase,
            ContractType.DestroyBase,
            ContractType.Rescue,
            ContractType.SimpleBattle,
            ContractType.FireMission,
            ContractType.AttackDefend,
            ContractType.ThreeWayBattle
        };

        public static float GetCareerModifier(SimGameDifficulty simGameDifficulty)
        {
            float ret = 0.0f;
            foreach(SimGameDifficulty.DifficultySetting difficultySetting in simGameDifficulty.GetSettings())
            {
                if (difficultySetting.Enabled)
                {
                    int idx = simGameDifficulty.GetCurSettingIndex(difficultySetting.ID);
                    ret += difficultySetting.Options[idx].CareerScoreModifier;
                }
            }

            PersistentMapClient.Logger.Log($"Unclamped Career Modifier is: {ret}");
            ret = Mathf.Clamp(ret, 0.01f, 1.5f);
            PersistentMapClient.Logger.Log($"Clamped Career Modifier is: {ret}");
            return ret;
        }

        public static Contract GetNewWarContract(SimGameState Sim, int Difficulty, FactionValue emp, FactionValue targ, FactionValue third, StarSystem system) {
            Fields.prioGen = true;
            Fields.prioEmployer = emp;
            Fields.prioTarget = targ;
            Fields.prioThird = third;

            if (Difficulty <= 1) {
                Difficulty = 2;
            }
            else if (Difficulty > 9) {
                Difficulty = 9;
            }

            var difficultyRange = AccessTools.Method(typeof(SimGameState), "GetContractRangeDifficultyRange").Invoke(Sim, new object[] { system, Sim.SimGameMode, Sim.GlobalDifficulty });
            Dictionary<int, List<ContractOverride>> potentialContracts = (Dictionary<int, List<ContractOverride>>)AccessTools.Method(typeof(SimGameState), "GetContractOverrides").Invoke(Sim, new object[] { difficultyRange, prioTypes });
            WeightedList<MapAndEncounters> playableMaps =
                //MetadataDatabase.Instance.GetReleasedMapsAndEncountersByContractTypeAndTagsAndOwnership(potentialContracts.Keys.ToArray<ContractType>(), 
                //      system.Def.MapRequiredTags, system.Def.MapExcludedTags, system.Def.SupportedBiomes).ToWeightedList(WeightedListType.SimpleRandom);
                // TODO: MORPH - please review!
                MetadataDatabase.Instance.GetReleasedMapsAndEncountersBySinglePlayerProceduralContractTypeAndTags(
                    system.Def.MapRequiredTags, system.Def.MapExcludedTags, system.Def.SupportedBiomes, true)
                    .ToWeightedList(WeightedListType.SimpleRandom);
            var validParticipants = AccessTools.Method(typeof(SimGameState), "GetValidParticipants").Invoke(Sim, new object[] { system });
            if (!(bool)AccessTools.Method(typeof(SimGameState), "HasValidMaps").Invoke(Sim, new object[] { system, playableMaps })
                || !(bool)AccessTools.Method(typeof(SimGameState), "HasValidContracts").Invoke(Sim, new object[] { difficultyRange, potentialContracts })
                || !(bool)AccessTools.Method(typeof(SimGameState), "HasValidParticipants").Invoke(Sim, new object[] { system, validParticipants })) {
                return null;
            }
            AccessTools.Method(typeof(SimGameState), "ClearUsedBiomeFromDiscardPile").Invoke(Sim, new object[] { playableMaps });
            IEnumerable<int> mapWeights = from map in playableMaps
                                          select map.Map.Weight;
            WeightedList<MapAndEncounters> activeMaps = new WeightedList<MapAndEncounters>(WeightedListType.WeightedRandom, playableMaps.ToList(), mapWeights.ToList<int>(), 0);
            AccessTools.Method(typeof(SimGameState), "FilterActiveMaps").Invoke(Sim, new object[] { activeMaps, Sim.GlobalContracts });
            activeMaps.Reset(false);
            MapAndEncounters level = activeMaps.GetNext(false);
            var MapEncounterContractData = AccessTools.Method(typeof(SimGameState), "FillMapEncounterContractData").Invoke(Sim, new object[] { system, difficultyRange, potentialContracts, validParticipants, level });
            bool HasContracts = Traverse.Create(MapEncounterContractData).Property("HasContracts").GetValue<bool>();
            while (!HasContracts && activeMaps.ActiveListCount > 0) {
                level = activeMaps.GetNext(false);
                MapEncounterContractData = AccessTools.Method(typeof(SimGameState), "FillMapEncounterContractData").Invoke(Sim, new object[] { system, difficultyRange, potentialContracts, validParticipants, level });
            }
            system.SetCurrentContractFactions(FactionEnumeration.GetInvalidUnsetFactionValue(), FactionEnumeration.GetInvalidUnsetFactionValue());
            HashSet<int> Contracts = Traverse.Create(MapEncounterContractData).Field("Contracts").GetValue<HashSet<int>>();

            if (MapEncounterContractData == null || Contracts.Count == 0) {
                List<string> mapDiscardPile = Traverse.Create(Sim).Field("mapDiscardPile").GetValue<List<string>>();
                if (mapDiscardPile.Count > 0) {
                    mapDiscardPile.Clear();
                }
                else {
                    PersistentMapClient.Logger.Log(string.Format("[CONTRACT] Unable to find any valid contracts for available map pool. Alert designers.", new object[0]));
                }
            }
            GameContext gameContext = new GameContext(Sim.Context);
            gameContext.SetObject(GameContextObjectTagEnum.TargetStarSystem, system);


            Contract contract = (Contract)AccessTools.Method(typeof(SimGameState), "CreateProceduralContract").Invoke(Sim, new object[] { system, true, level, MapEncounterContractData, gameContext });
            Fields.prioGen = false;
            return contract;
        }
    }
}

