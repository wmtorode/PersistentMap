﻿using BattleTech;

namespace PersistentMapAPI {
    public class MissionResult {
        public string employer;
        public string target;
        public BattleTech.MissionResult result;
        public string systemName;
        public int difficulty;
        public int awardedRep;
        public int planetSupport;
        public int mCount;
        public string missionType;
        public int rtState;
        public string rtKey;
        public int cbCount;

        public MissionResult(FactionValue employer, FactionValue target, BattleTech.MissionResult result, string systemName, int difficulty, int awardedRep, int planetSupport, int mCount, string missionType, int cbCount, int state, string key) {
            this.awardedRep = awardedRep;
            this.difficulty = difficulty;
            this.employer = employer.Name;
            this.result = result;
            this.systemName = systemName;
            this.target = target.Name;
            this.planetSupport = planetSupport;
            this.mCount = mCount;
            this.missionType = missionType;
            this.cbCount = cbCount;
            this.rtState = state;
            this.rtKey = key;
        }

        public MissionResult() {
        }
    }
}
