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

namespace PersistentMapClient.shops
{
    class OnlineShop: IShopDescriptor, IListShop, IFillWidgetFromFaction, ISpriteIcon, ICustomDiscount, IDefaultPrice
    {
        private int updateAfterMinutesElapsed = 15;
        private DateTime nextUpdate = DateTime.UtcNow;
        private bool needsRefresh = false;
        public virtual FactionValue RelatedFaction => Control.State.CurrentSystem.Def.FactionShopOwnerValue;

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

                var owner = Control.State.CurrentSystem.Def.FactionShopOwnerValue;
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
                var owner = Control.State.CurrentSystem.Def.FactionShopOwnerValue;
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
            inventory = Web.GetShopForFaction(RelatedFaction);
            if (inventory == null)
            {
                inventory = new List<ShopDefItem>();
            }
        }

        public List<ShopDefItem> Items
        {
            get 
            {
                if (DateTime.UtcNow > this.nextUpdate || this.needsRefresh)
                {
                    this.RefreshShop();
                    this.nextUpdate = DateTime.UtcNow;
                    this.nextUpdate.AddMinutes(this.updateAfterMinutesElapsed);
                    this.needsRefresh = false;
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
            return item.DiscountModifier;
        }
    }
}
