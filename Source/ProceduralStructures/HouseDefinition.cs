using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace ProceduralStructures;

public class HouseDefinition
{
    public enum Side {Front, Back, Right, Left}

    [Serializable]
    public class WallCutout {
        public string name;
        public Side side = Side.Front;
        public Rectangle dimension;
        public Material material;
        public float uvScale = 0.01f;
        public Prefab prefab;
    }

    [Serializable]
    public class Stairs {
        public Side side = Side.Front;
        public bool inside = false;
        public float offset = 0;
        public float baseWidth = 100;
        public float baseLength = 60f;
        public float baseHeight = 0;
        public float descentAngle = 0f;
        public float totalHeight;
        public float stepHeight = 25;
        public float stepDepth = 40;
        public Material material;
        public float uvScale;
    }

    [Serializable]
    public class BuildingStructure
    {
        public string name;
        public float height;
        public bool hollow;
        public bool addCeiling;
        public bool addFloor = true;
        public float wallThickness = 50;

        [Tooltip("This is an indent per unit height.")]
        public float slopeX = 0;
        [Tooltip("This is an indent per unit height.")]
        public float slopeZ = 0;
        public Material material;
        public float uvScale = 0.01f;
        public WallCutout[] cutouts;
        public Stairs[] stairs;
    }

    [Header("Basement Settings")]
    public float heightOffset = -200;
    public float width = 900;
    public float length = 600;
    public bool constructFrameHouse;
 
    public string name;
    public List<BuildingStructure> layers = new List<BuildingStructure>();

    [Header("Roof")]
    public float roofHeight;
    public Material materialRoof;
    public float uvScaleRoof = 0.01f;
    public Material materialGable;
    public float uvScaleGable = 0.01f;
    public float roofExtendX;
    public float roofExtendZ;
    public float roofThickness;

    public float TotalHeight => roofHeight + layers.Sum(l => l.height);
}
