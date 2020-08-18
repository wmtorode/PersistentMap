using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace PersistentMapClient
{
    public class ContractGenCallback
    {
        public StarSystem lastSystem;

        public void callbackAction()
        {
            Action action = (Action)Delegate.CreateDelegate(typeof(Action), lastSystem, "OnInitialContractFetched");
            List<StarSystem> travels = lastSystem.Sim.StarSystems;
            travels.Shuffle<StarSystem>();
            lastSystem.Sim.GeneratePotentialContracts(false, action, travels[0], true);
        }

    }
}
