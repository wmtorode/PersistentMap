﻿using System;
using BattleTech;
using Harmony;
using BattleTech.UI;
using BattleTech.Framework;

namespace PersistentMapClient
{
    [HarmonyPatch(typeof(SGContractsWidget), "GetContractComparePriority")]
    public static class SGContractsWidget_GetContractComparePriority_Patch
    {
        static bool Prefix(SGContractsWidget __instance, ref int __result, Contract contract)
        {
            try
            {
                int difficulty = contract.Override.GetUIDifficulty();
                if (Fields.settings.SortByRealDifficulty)
                {
                    difficulty = contract.Difficulty;
                }
                int result = 100;
                SimGameState Sim = (SimGameState)ReflectionHelper.InvokePrivateMethode(__instance, "get_Sim", null);
                if (Sim.ContractUserMeetsReputation(contract))
                {
                    if (contract.Override.contractDisplayStyle == ContractDisplayStyle.BaseCampaignRestoration)
                    {
                        result = 0;
                    }
                    else if (contract.Override.contractDisplayStyle == ContractDisplayStyle.BaseCampaignStory)
                    {
                        result = 1;
                    }
                    else if (contract.TargetSystem.Replace("starsystemdef_", "").Equals(Sim.CurSystem.Name))
                    {
                        result = difficulty + 1;
                    }
                    else
                    {
                        result = difficulty + 11;
                    }
                }
                else
                {
                    result = difficulty + 21;
                }
                __result = result;
                return false;
            }
            catch (Exception e)
            {
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
        }
    }
}
