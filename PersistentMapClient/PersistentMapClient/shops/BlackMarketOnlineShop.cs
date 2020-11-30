using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using CustomShops;
using PersistentMapClient;
using BattleTech.UI;
using HBS;
using UnityEngine;
using PersistentMapClient.Objects;

namespace PersistentMapClient.shops
{
    class BlackMarketOnlineShop: OnlineShop
    {
        public new bool isBlackMarket = true;

        public override string Name => "Online BlackMarket";
        public override string TabText => RelatedFaction == null ? "ERROR_FACTION" : RelatedFaction.Name + " Blackmarket";

        public Sprite internalSprite;
        public override bool Exists
        {
            get
            {
                if (Control.State.CurrentSystem == null || RelatedFaction == null)
                {
                    return false;
                }
                if (!Control.State.Sim.IsFactionAlly(RelatedFaction))
                {
                    return Web.BlackMarketAvailable(Control.State.Sim);
                }
                return false;
            }
        }

        public override string ShopPanelImage
        {
            get
            {
                return SG_Stores_StoreImagePanel.BLACK_MARKET_ILLUSTRATION;
            }
        }

        public override void RefreshShop()
        {
            if (Control.State.Sim.IsFactionAlly(RelatedFaction))
            {
                inventory = new List<ShopDefItem>();
            }
            else
            {
                inventory = Web.GetShopForFaction(RelatedFaction, isBlackMarket);
                if (inventory == null)
                {
                    inventory = new List<ShopDefItem>();
                }
            }
            this.nextUpdate = DateTime.UtcNow.AddMinutes(this.updateAfterMinutesElapsed);
            this.needsRefresh = false;
        }

        public override bool Purshase(ShopDefItem item, int quantity)
        {
            bool ret = true;
            Fields.currentShopOwner = RelatedFaction;
            if (!Fields.shopItemsSold.ContainsKey(item.ID))
            {
                PurchasedItem pItem = new PurchasedItem();
                pItem.ID = item.ID;
                pItem.Count = quantity;
                pItem.Cost = UIControler.GetPrice(item) * quantity;
                Fields.shopItemsSold.Add(item.ID, pItem);
            }
            else
            {
                Fields.shopItemsSold[item.ID].Count += quantity;
                Fields.shopItemsSold[item.ID].Cost += UIControler.GetPrice(item) * quantity;
            }
            try
            {
                Web.PostBuyItems(Fields.shopItemsSold, RelatedFaction, isBlackMarket);
                UIControler.DefaultPurshase(this, item, quantity);
            }
            catch (Exception e)
            {
                PersistentMapClient.Logger.LogError(e);
                ret = false;
            }
            Fields.shopItemsSold = new Dictionary<string, Objects.PurchasedItem>();
            needsRefresh = true;
            return ret;
        }
    }
}
