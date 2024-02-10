using FlaxEngine;

namespace ProceduralStructures;

/// <summary>
/// HouseBuilder Script.
/// </summary>
public class HouseBuilder : Script
{
    public JsonAsset houseDefinitionAsset;

    private HouseDefinition houseDefinition =>
        houseDefinitionAsset != null ? (HouseDefinition)houseDefinitionAsset.Instance : null;

    public string StreetName;
    public int Number;

    public override void OnDebugDraw()
    {
        if (Actor.ChildrenCount > 0)
            return;
        DrawGizmo(false);
    }

    public override void OnDebugDrawSelected()
    {
        if (Actor.ChildrenCount > 0)
            return;
        DrawGizmo(true);
    }

    private void DrawGizmo(bool selected)
    {
        if (houseDefinition != null)
        {
            var size = CalculateSize();
            var halfSize = Vector3.Multiply(size, 0.5f);
            var bounds = new OrientedBoundingBox(halfSize, Transform.GetWorld());
            bounds.Transformation.Translation += new Vector3(0, halfSize.Y, 0);
            if (selected)
            {
                DebugDraw.DrawBox(bounds, new Color(1, 1, 1, 0.4f));
            }
            else
            {
                DebugDraw.DrawWireBox(bounds, Color.White);
            }

            var start = new Float3(0, 0, -houseDefinition.length / 2);
            var end = new Float3(0, 0, -houseDefinition.length / 2 - 200);
#if FLAX_EDITOR
            DebugDraw.DrawLines(new[] { start, end }, Transform.GetWorld(), Color.White);
#endif
        }
        else
        {
            var bounds = new OrientedBoundingBox(new Vector3(100, 100, 100), Transform.GetWorld());
            DebugDraw.DrawBox(bounds, Color.White);
        }
    }

    public Vector3 CalculateSize()
    {
        return houseDefinition != null
            ? new Vector3(houseDefinition.width, houseDefinition.TotalHeight, houseDefinition.length)
            : new Vector3(400, 200, 400);
    }

    public Vector3 CalculateCenter()
    {
        return Actor.Position;
    }
}