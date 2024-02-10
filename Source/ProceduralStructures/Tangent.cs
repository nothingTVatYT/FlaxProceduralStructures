using System;
using FlaxEngine;

namespace ProceduralStructures;

[Serializable]
public class Tangent {
    public Vector3 Position;
    public Vector3 Direction;
    public float RelativePosition;
    public float ScaleWidth;
    public float ScaleHeight;

    public Tangent(Vector3 position, Vector3 direction, float relPos = 0, float scaleWidth = 1f, float scaleHeight = 1f) {
        Position = position;
        Direction = direction;
        RelativePosition = relPos;
        ScaleWidth = scaleWidth;
        ScaleHeight = scaleHeight;
    }

    public static Tangent Lerp(Tangent t1, Tangent t2, float t) {
        var pos = Vector3.Lerp(t1.Position, t2.Position, t);
        var direction = Vector3.Lerp(t1.Direction, t2.Direction, t);
        var relPos = Mathf.Lerp(t1.RelativePosition, t2.RelativePosition, t);
        var scaleWidth = Mathf.Lerp(t1.ScaleWidth, t2.ScaleWidth, t);
        var scaleHeight = Mathf.Lerp(t1.ScaleHeight, t2.ScaleHeight, t);
        return new Tangent(pos, direction, relPos, scaleWidth, scaleHeight);
    }

    public override string ToString()
    {
        return string.Format("T[@" + Position + "," + Direction + "," + RelativePosition + "]");
    }
}