using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS.Collections;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PersistentMapClient {
    
    [HarmonyPatch(typeof(Contract), "FinalizeSalvage")]
    public static class Contract_FinalizeSalvage_Patch {
        static void Postfix(Contract __instance, List<SalvageDef> ___finalPotentialSalvage) {
            try {
                SimGameState simulation = __instance.BattleTechGame.Simulation;
                if (simulation.IsFactionAlly(__instance.Override.employerTeam.FactionValue, null) && Fields.canPostSalvage) {
                    Web.PostUnusedSalvage(___finalPotentialSalvage, __instance.Override.employerTeam.FactionValue);
                }
            }
            catch (Exception e) {
               PersistentMapClient.Logger.LogError(e);
            }
        }
    }    

    [HarmonyPatch(typeof(SG_Shop_Screen), "OnCompleted")]
    public static class SG_Shop_Screen_OnCompleted_Patch {
        static void Postfix() {
            try {
                if (Fields.currentShopOwner != FactionEnumeration.GetInvalidUnsetFactionValue() && PersistentMapClient.shop.Exists) {
                    Web.PostBuyItems(Fields.shopItemsSold, Fields.currentShopOwner);
                    if(Fields.shopItemsPosted.Count() > 0)
                    {
                        Web.PostSoldItems(Fields.shopItemsPosted, Fields.currentShopOwner);
                        Fields.shopItemsPosted = new Dictionary<string, ShopDefItem>();
                        PersistentMapClient.shop.needsRefresh = true;
                    }
                    if (Fields.shopItemsSold.Count() > 0)
                    {
                        PersistentMapClient.shop.needsRefresh = true;
                    }
                    Fields.shopItemsSold = new Dictionary<string, PersistentMapAPI.PurchasedItem>();
                    
                }
            }
            catch (Exception e) {
              PersistentMapClient.Logger.LogError(e);
            }
        }
    }

}