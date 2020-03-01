using BattleTech;

namespace PersistentMapAPI {
    public class MissionResult {
        public FactionValue employer;
        public FactionValue target;
        public BattleTech.MissionResult result;
        public string systemName;
        public int difficulty;
        public int awardedRep;
        public int planetSupport;

        public MissionResult(FactionValue employer, FactionValue target, BattleTech.MissionResult result, string systemName, int difficulty, int awardedRep, int planetSupport) {
            this.awardedRep = awardedRep;
            this.difficulty = difficulty;
            this.employer = employer;
            this.result = result;
            this.systemName = systemName;
            this.target = target;
            this.planetSupport = planetSupport;
        }

        public MissionResult() {
        }
    }
}
