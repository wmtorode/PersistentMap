using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;

namespace PersistentMapClient.Objects
{
    class FactionRewardPopup : SimGameInterruptManager.Entry
    {
        public int choices;
        public FactionRewardPopup(ItemCollectionDef itemCollection)
        {
            this.type = SimGameInterruptManager.InterruptType.RewardsPopup;
            this.parameters.Add((object)itemCollection);
            this.choices = itemCollection.Entries.Count();
        }

        public override void Render()
        {
            List<object> parameters = this.parameters;
            this.manager.Sim.StopPlayMode();
            ItemCollectionDef def = parameters[0] as ItemCollectionDef;
            RewardsPopup.Show(this.manager.Sim, def, this.choices, new Action(this.Complete));
        }

        public override bool IsUnique()
        {
            return true;
        }

        public override bool NeedsFader()
        {
            return true;
        }

        public override bool IsVisible()
        {
            return true;
        }

        public void Complete()
        {
            this.Close();
        }
    }
}
