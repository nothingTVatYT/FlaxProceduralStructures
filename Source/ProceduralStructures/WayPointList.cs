using System;
using System.Collections.Generic;

namespace Game.ProceduralStructures {
    [Serializable]
    public class WayPointList {
        public string Name;
        public List<WayPoint> WayPoints;
        public WayPointList(string name, List<WayPoint> points) {
            Name = name;
            WayPoints = points;
        }
        public int Count => WayPoints?.Count ?? 0;
    }
}