using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using Harmony;
using HBS;
using HBS.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PersistentMapClient
{
    [HarmonyPatch(typeof(SimGameState), "OnTargetSystemFound")]
    public static class SimGameState_OnTargetSystemFound
    {
        static bool Prefix(SimGameState __instance)
        {
            try
            {
                __instance.CurSystem.RefreshSystem();
                if (__instance.UXAttached)
                {
                    __instance.RoomManager.ShipRoom.RefreshData();
                }
                __instance.SetReputation(FactionEnumeration.GetOwnerFactionValue(), __instance.CurSystem.OwnerReputation, StatCollection.StatOperation.Set, null);
                Fields.currBorderCons = 0;
                return false;
            }
            catch (Exception e)
            {
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "GenerateContractParticipants")]
    public static class SimGameState_GenerateContractParticipants_Patch
    {
        public static void Prefix(FactionDef employer, StarSystemDef system)
        {

            if (!Fields.immuneFromWar.Contains(system.Description.Name))
                return;

            Fields.enemyHolder.Clear();
            var NewEnemies = system.ContractTargetIDList;
            Fields.enemyHolder = employer.Enemies.ToList();
            var NewFactionEnemies = Fields.enemyHolder;

            foreach (var faction in Fields.settings.immuneSystemEnemies)
            {
                if (!NewFactionEnemies.Contains(faction) && faction != employer.FactionValue.Name)
                {
                        NewFactionEnemies.Add(faction);
                }
            }

            Traverse.Create(employer).Property("Enemies").SetValue(NewFactionEnemies.ToArray());
            Fields.needsToReloadEnemies = true;
        }

        public static void Postfix(FactionDef employer)
        {
            if (Fields.needsToReloadEnemies)
            {
                Traverse.Create(employer).Property("Enemies").SetValue(Fields.enemyHolder.ToArray());
                Fields.needsToReloadEnemies = false;
            }
        }
    }

    [HarmonyPatch(typeof(StarSystem), "RefreshBreadcrumbs")]
    public static class StarSystem_RefreshBreadcrumbs
    {
        static bool Prefix(StarSystem __instance)
        {
            try
            {
                if (__instance.CurBreadcrumbOverride > 0)
                {
                    ReflectionHelper.InvokePrivateMethode(__instance, "set_CurMaxBreadcrumbs", new object[] { __instance.CurBreadcrumbOverride });
                }
                else
                {
                    int num = __instance.MissionsCompleted;
                    if (num < __instance.Sim.Constants.Story.MissionsForFirstBreadcrumb)
                    {
                        return false;
                    }
                    ReflectionHelper.InvokePrivateMethode(__instance, "set_CurMaxBreadcrumbs", new object[] { __instance.Sim.Constants.Story.MaxBreadcrumbsPerSystem });
                }

                return false;
            }
            catch (Exception e)
            {
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
        }
    }

        [HarmonyPatch(typeof(SimGameState), "GeneratePotentialContracts")]
    public static class SimGameState_GeneratePotentialContracts
    {

        static bool Prefix(SimGameState __instance, bool clearExistingContracts, Action onContractGenComplete, StarSystem systemOverride = null, bool useCoroutine = false)
        {
            try
            {
                if (systemOverride == null)
                {
                    PersistentMapClient.Logger.Log($"SystemOverride is null, falling back to vanilla generator: {__instance.CurSystem.Name}");
                    //LazySingletonBehavior<UnityGameInstance>.Instance.StartCoroutine(StartGeneratePotentialContractsRoutine(__instance, clearExistingContracts, onContractGenComplete, systemOverride, useCoroutine));
                    //return false;
                    return true;
                }
                //else
                //if(Fields.immuneFromWar.Contains(systemOverride.Name))
                //{
                //    PersistentMapClient.Logger.Log($"SystemOverride is immune from war, falling back to vanilla generator: {systemOverride.Name}");
                //    LazySingletonBehavior<UnityGameInstance>.Instance.StartCoroutine(StartGeneratePotentialContractsRoutine(__instance, clearExistingContracts, onContractGenComplete, systemOverride, useCoroutine));
                //    return false;
                    //return true;
                //}
                else
                {
                    PersistentMapClient.Logger.Log($"Using RTC generator: {systemOverride.Name}");
                    LazySingletonBehavior<UnityGameInstance>.Instance.StartCoroutine(StartGeneratePotentialContractsRoutine(__instance, clearExistingContracts, onContractGenComplete, systemOverride, useCoroutine));
                    return false;
                }
            }
            catch (Exception e)
            {
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
        }

        private static IEnumerator StartGeneratePotentialContractsRoutine(SimGameState __instance, bool clearExistingContracts, Action onContractGenComplete, StarSystem systemOverride, bool useWait)
        {
            int debugCount = 0;
            bool usingBreadcrumbs = systemOverride != null;
            if (useWait)
            {
                yield return new WaitForSeconds(0.2f);
            }
            StarSystem system;
            List<Contract> contractList;
            int maxContracts;
            if (usingBreadcrumbs)
            {
                system = systemOverride;
                contractList = __instance.CurSystem.SystemBreadcrumbs;
                maxContracts = __instance.CurSystem.CurMaxBreadcrumbs;
            }
            else
            {
                system = __instance.CurSystem;
                contractList = __instance.CurSystem.SystemContracts;
                maxContracts = Mathf.CeilToInt(system.CurMaxContracts);
            }
            if (clearExistingContracts)
            {
                contractList.Clear();
            }
            //SimGameState.logger.LogError($"RTC gen System: {system.Name}");
            List<StarSystem> AllSystems = new List<StarSystem>();
            foreach (StarSystem addsystem in __instance.StarSystems)
            {
                if (addsystem.OwnerValue.Name != FactionEnumeration.GetNoFactionValue().Name)
                {
                    AllSystems.Add(addsystem);
                }
            }
            while (contractList.Count < maxContracts && debugCount < 1000)
            {
                try
                {
                    if (usingBreadcrumbs)
                    {
                        List<StarSystem> listsys = AllSystems;
                        listsys.Shuffle();
                        if (Fields.currBorderCons < maxContracts * Fields.settings.percentageOfTravelOnBorder)
                        {
                            int sysNr = 0;
                            if (Fields.settings.warBorders)
                            {
                                while (!Helper.IsWarBorder(listsys[sysNr], __instance))
                                {
                                    sysNr++;
                                }
                            }
                            else
                            {
                                while (!Helper.IsRandomTravelBorder(listsys[sysNr], __instance))
                                {
                                    sysNr++;
                                }
                            }
                            system = listsys[sysNr];
                            Fields.currBorderCons++;
                        }
                        else
                        {
                            system = listsys[0];
                        }
                    }
                }
                catch (Exception e)
                {
                    PersistentMapClient.Logger.LogError(e);
                    yield break;
                }
                //SimGameState.logger.LogError($"RTC gen Real System: {system.Name}");
                var difficultyRange = AccessTools.Method(typeof(SimGameState), "GetContractRangeDifficultyRange").Invoke(__instance, new object[] { system, __instance.SimGameMode, __instance.GlobalDifficulty });
                Dictionary<int, List<ContractOverride>> potentialContracts = (Dictionary<int, List<ContractOverride>>)AccessTools.Method(typeof(SimGameState), "GetSinglePlayerProceduralContractOverrides").Invoke(__instance, new object[] { difficultyRange });
                WeightedList<MapAndEncounters> playableMaps = MetadataDatabase.Instance.GetReleasedMapsAndEncountersBySinglePlayerProceduralContractTypeAndTags(system.Def.MapRequiredTags, system.Def.MapExcludedTags, system.Def.SupportedBiomes, true).ToWeightedList(WeightedListType.SimpleRandom);
                var validParticipants = AccessTools.Method(typeof(SimGameState), "GetValidParticipants").Invoke(__instance, new object[] { system });
                if (!(bool)AccessTools.Method(typeof(SimGameState), "HasValidMaps").Invoke(__instance, new object[] { system, playableMaps })
                    || !(bool)AccessTools.Method(typeof(SimGameState), "HasValidContracts").Invoke(__instance, new object[] { difficultyRange, potentialContracts })
                    || !(bool)AccessTools.Method(typeof(SimGameState), "HasValidParticipants").Invoke(__instance, new object[] { system, validParticipants }))
                {
                    if (onContractGenComplete != null)
                    {
                        onContractGenComplete();
                    }
                    yield break;
                }
                AccessTools.Method(typeof(SimGameState), "ClearUsedBiomeFromDiscardPile").Invoke(__instance, new object[] { playableMaps });
                debugCount++;
                IEnumerable<int> mapWeights = from map in playableMaps
                                              select map.Map.Weight;
                /*SimGameState.logger.LogError($"potential contracts: {potentialContracts.Count}");
                foreach (KeyValuePair<int, List<ContractOverride>> kVcontracts in potentialContracts)
                {
                    SimGameState.logger.LogError($"Key: {kVcontracts.Key}");
                    foreach (ContractOverride contractOverride in kVcontracts.Value)
                    {
                        SimGameState.logger.LogError($"contract: {contractOverride.ID}");
                    }
                }*/
                WeightedList<MapAndEncounters> activeMaps = new WeightedList<MapAndEncounters>(WeightedListType.WeightedRandom, playableMaps.ToList(), mapWeights.ToList<int>(), 0);
               /* SimGameState.logger.LogError($"activeMaps before filter: {activeMaps.Count}");
                foreach (MapAndEncounters mae in activeMaps.ToList())
                {
                    SimGameState.logger.LogError($"map: {mae.Map.FriendlyName}");
                    SimGameState.logger.LogError($"encounters: {String.Join(", ", mae.EncounterFriendlyNames())}");
                    List<string> contractTypes = new List<string>();
                    foreach (EncounterLayer_MDD encounterLayerMDD in mae.Encounters)
                    {
                        contractTypes.Add(((int)encounterLayerMDD.ContractTypeRow.ContractTypeID).ToString());
                    }
                    SimGameState.logger.LogError($"encounter contract types: {String.Join(", ", contractTypes)}");
                }*/
                AccessTools.Method(typeof(SimGameState), "FilterActiveMaps").Invoke(__instance, new object[] { activeMaps, contractList });
                //SimGameState.logger.LogError($"activeMaps after filter: {activeMaps.Count}");
                activeMaps.Reset(false);
                MapAndEncounters level = activeMaps.GetNext(false);
                var MapEncounterContractData = AccessTools.Method(typeof(SimGameState), "FillMapEncounterContractData").Invoke(__instance, new object[] { system, difficultyRange, potentialContracts, validParticipants, level });
                bool HasContracts = Traverse.Create(MapEncounterContractData).Property("HasContracts").GetValue<bool>();
                while (!HasContracts && activeMaps.ActiveListCount > 0)
                {
                    level = activeMaps.GetNext(false);
                    MapEncounterContractData = AccessTools.Method(typeof(SimGameState), "FillMapEncounterContractData").Invoke(__instance, new object[] { system, difficultyRange, potentialContracts, validParticipants, level });
                }
                system.SetCurrentContractFactions(FactionEnumeration.GetInvalidUnsetFactionValue(), FactionEnumeration.GetInvalidUnsetFactionValue());
                HashSet<int> Contracts = Traverse.Create(MapEncounterContractData).Field("Contracts").GetValue<HashSet<int>>();

                if (MapEncounterContractData == null || Contracts.Count == 0)
                {
                    List<string> mapDiscardPile = Traverse.Create(__instance).Field("mapDiscardPile").GetValue<List<string>>();
                    if (mapDiscardPile.Count > 0)
                    {
                        mapDiscardPile.Clear();
                    }
                    else
                    {
                        debugCount = 1000;
                        SimGameState.logger.LogError(string.Format("[CONTRACT] Unable to find any valid contracts for available map pool. Alert designers.", new object[0]));
                    }
                }
                GameContext gameContext = new GameContext(__instance.Context);
                gameContext.SetObject(GameContextObjectTagEnum.TargetStarSystem, system);
                Contract con = (Contract)AccessTools.Method(typeof(SimGameState), "CreateProceduralContract").Invoke(__instance, new object[] { system, usingBreadcrumbs, level, MapEncounterContractData, gameContext });
                contractList.Add(con);
                if (useWait)
                {
                    yield return new WaitForSeconds(0.2f);
                }
            }
            if (debugCount >= 1000)
            {
                SimGameState.logger.LogError("Unable to fill contract list. Please inform AJ Immediately");
            }
            if (onContractGenComplete != null)
            {
                onContractGenComplete();
            }
            yield break;
        }
    }
}
