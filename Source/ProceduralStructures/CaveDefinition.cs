using System;
using System.Collections.Generic;
using FlaxEngine;

namespace ProceduralStructures;

[Serializable]
public class CaveDefinition
{
    public enum Shape
    {
        Tunnel,
        OShaped
    }

    [Tooltip("Number of times interpolated vertices are added")] [Range(0, 5)]
    public int ShapeSmoothing = 1;

    public MeshObject.ShadingType ShadingType = MeshObject.ShadingType.Auto;
    public List<WayPointList> WayPointLists;
    [Tooltip("Shape of the cave")] public Shape CrosscutShape = Shape.Tunnel;

    [Tooltip("unscaled height of the cave")]
    public float BaseHeight = 300;

    [Tooltip("unscaled width of the cave")]
    public float BaseWidth = 300;

    [Tooltip("average distance (in cm) between vertices along the curve")] [Range(10f, 300f)]
    public float UResolution;

    public Material Material;
    public float UScale = 0.01f;
    public float VScale = 0.01f;
    public bool CloseBeginning = true;
    public bool CloseEnd = true;
    public bool RandomizeVertices = false;
    public Vector3 RandomDisplacement = Vector3.Zero;

    private BezierSpline _spline;

    public bool IsValid()
    {
        return WayPointLists != null && WayPointLists.Count > 0 && WayPointLists[0].Count >= 2;
    }

    public IEnumerable<Vector3> GetVertices(WayPointList list)
    {
        _spline = new BezierSpline(list.WayPoints);
        float t = 0;
        if (UResolution < 0.1f) UResolution = 0.1f;
        var stepSize = UResolution / _spline.EstimatedLength;
        while (t < (1f + stepSize))
        {
            var v = GetVertex(t);
            t += stepSize;
            yield return v;
        }
    }

    public IEnumerable<Tangent> GetTangents(WayPointList list)
    {
        _spline = new BezierSpline(list.WayPoints);
        float t = 0;
        if (UResolution < 0.1f) UResolution = 0.1f;
        var stepSize = UResolution / _spline.EstimatedLength;
        while (t < (1f + stepSize))
        {
            var v = GetTangent(t);
            t += stepSize;
            yield return v;
        }
    }

    Vector3 GetVertex(float t)
    {
        return _spline.GetVertex(t);
    }

    Tangent GetTangent(float t)
    {
        return _spline.GetTangent(t);
    }

    public MeshObject GetConnection(int index, float where)
    {
        var bezier = new BezierSpline(WayPointLists[index].WayPoints);
        var center = bezier.GetVertex(where);
        var direction = bezier.GetTangent(where);
        var ps = new ProceduralStructure();
        var connectionObject = ps.GetCaveConnection(this, center, direction);
        return connectionObject;
    }
}