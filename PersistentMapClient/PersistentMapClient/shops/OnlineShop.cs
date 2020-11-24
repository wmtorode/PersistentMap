﻿using System;
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
    class OnlineShop: IShopDescriptor, IListShop, IFillWidgetFromFaction, ISpriteIcon, ICustomDiscount, IDefaultPrice, ICustomPurshase, ISellShop, IForceRefresh
    {
        private int updateAfterMinutesElapsed = 15;
        private DateTime nextUpdate = DateTime.UtcNow;
        public bool needsRefresh = false;
        public int SellPriority => 1;

        public virtual FactionValue RelatedFaction => Control.State.CurrentSystem.OwnerValue;

        public int SortOrder => Control.Settings.FactionShopPriority;
        public bool RefreshOnSystemChange => true;
        public bool RefreshOnMonthChange => false;
        public bool RefreshOnOwnerChange => true;
        public bool Exists
        {
            get
            {
                if (Control.State.CurrentSystem == null || RelatedFaction == null)
                {
                    return false;
                }
                if (Control.State.Sim.IsFactionAlly(RelatedFaction))
                {
                    return true;
                }
                return false;
            }
        }

        public bool CanUse => Exists;

        public string Name => "Online";
        public string TabText => RelatedFaction == null ? "ERROR_FACTION" : RelatedFaction.Name + " Online";

        private List<ShopDefItem> inventory = new List<ShopDefItem>();

        public string ShopPanelImage
        {
            get
            {
                if (RelatedFaction == null)
                    return SG_Stores_StoreImagePanel.BLACK_MARKET_ILLUSTRATION;

                return string.IsNullOrEmpty(RelatedFaction.FactionDef.storePanelImage) ?
                    SG_Stores_StoreImagePanel.BLACK_MARKET_ILLUSTRATION :
                    RelatedFaction.FactionDef.storePanelImage;
            }
        }

        public virtual Sprite Sprite
        {
            get
            {
                if (!Exists)
                    return null;

                var owner = RelatedFaction;
                if (owner == null)
                    return null;

                return owner.FactionDef.GetSprite();
            }
        }

        public Color IconColor
        {
            get
            {
                if (!Exists)
                    return Color.white;
                var owner = RelatedFaction;
                if (owner == null)
                    return Color.white; // LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FactionStoreColor.color;
                return Color.white; //owner.FactionDef.GetFactionStoreColor(out var color) ? color : LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FactionStoreColor.color;
            }
        }
        public Color ShopColor
        {
            get
            {
                if (!Exists)
                    return LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FactionStoreColor.color;
                var owner = Control.State.CurrentSystem.Def.FactionShopOwnerValue;
                if (owner == null)
                    return LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FactionStoreColor.color;
                return owner.FactionDef.GetFactionStoreColor(out var color) ? color : LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FactionStoreColor.color;
            }
        }

        public void RefreshShop()
        {
            if (!Control.State.Sim.IsFactionAlly(RelatedFaction))
            {
                inventory = new List<ShopDefItem>();
            }
            else
            {
                inventory = Web.GetShopForFaction(RelatedFaction);
                if (inventory == null)
                {
                    inventory = new List<ShopDefItem>();
                }
            }
            this.nextUpdate = DateTime.UtcNow.AddMinutes(this.updateAfterMinutesElapsed);
            this.needsRefresh = false;
        }

        public List<ShopDefItem> Items
        {
            get 
            {
                if ((DateTime.UtcNow.Ticks > this.nextUpdate.Ticks) || this.needsRefresh)
                {
                    PersistentMapClient.Logger.Log($"Time has elapsed, refreshing shop, {DateTime.UtcNow.Ticks}, {this.nextUpdate.Ticks}, {this.needsRefresh}");

                    this.RefreshShop();
                }
                return this.inventory;
            }
        }

        /*public int GetPrice(TypedShopDefItem item)
        {
            return (int)(item.Description.Cost * item.DiscountModifier);
        }*/

        public float GetDiscount(TypedShopDefItem item)
        {
            if (item == null)
            {
                PersistentMapClient.Logger.LogIfDebug("Null discount item recvd");
                return 1.0f;
            }
            return item.DiscountModifier;
        }

        public bool Purshase(ShopDefItem item, int quantity)
        {
            bool ret = true;
            Fields.currentShopOwner = RelatedFaction;
            if (!Fields.shopItemsSold.ContainsKey(item.ID))
            {
                PurchasedItem pItem = new PurchasedItem();
                pItem.ID = item.ID;
                pItem.Count = quantity;
                Fields.shopItemsSold.Add(item.ID, pItem);
            }
            else
            {
                Fields.shopItemsSold[item.ID].Count += quantity;
            }
            try
            {
                Web.PostBuyItems(Fields.shopItemsSold, RelatedFaction);
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

        public bool OnSellItem(ShopDefItem item, int num)
        {
            if (Exists && Web.CanPostSoldItems())
            {
                Fields.currentShopOwner = RelatedFaction;
                if (!Fields.shopItemsPosted.ContainsKey(item.ID))
                {
                    ShopDefItem pItem = new ShopDefItem(item);
                    pItem.Count = num;
                    Fields.shopItemsPosted.Add(item.ID, pItem);
                }
                else
                {
                    Fields.shopItemsPosted[item.ID].Count += num;
                }
                return true;
            }
            return false;
        }
    }
}
