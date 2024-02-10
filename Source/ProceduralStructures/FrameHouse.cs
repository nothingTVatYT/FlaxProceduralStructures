using FlaxEngine;

namespace ProceduralStructures;

public class FrameHouse : Script {
    public Actor ConstructionRoot;
    public FrameDefinition Frame;
    public float BeamThickness = 10f;
    public Material BeamMaterial;
    public float UvScale = 1f;
    public Material WallMaterial;
}