using System;
using FlaxEngine;

namespace Game.ProceduralStructures {
    [Serializable]
    public class WayPoint {
        public string Name;
        public Vector3 Position;
        public float ScaleWidth;
        public float ScaleHeight;
        public WayPoint(Vector3 position, float scaleWidth = 1, float scaleHeight = 1) {
            Position = position;
            ScaleWidth = scaleWidth;
            ScaleHeight = scaleHeight;
        }
    }
}