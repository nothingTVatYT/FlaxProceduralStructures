using System.Collections.Generic;
using FlaxEngine;
using ProceduralStructures.GK;

namespace ProceduralStructures;

public class ConvexHull : MeshObject
{
    List<Vector3> points = new();

    public void AddPoint(Vector3 p)
    {
        points.Add(p);
    }

    public void CalculateHull()
    {
        bool splitverts = false;
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        var chc = new ConvexHullCalculator();
        chc.GenerateHull(points, splitverts, ref verts, ref tris, ref normals);

        Vertices.Clear();
        Triangles.Clear();
        foreach (Vector3 v in verts)
        {
            AddUnchecked(v);
        }

        for (int i = 0; i < tris.Count; i += 3)
        {
            Triangles.Add(new Triangle(Vertices[tris[i]], Vertices[tris[i + 1]], Vertices[tris[i + 2]]));
        }
    }
}