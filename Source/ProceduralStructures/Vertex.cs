using System;
using System.Collections.Generic;
using FlaxEngine;

namespace ProceduralStructures;

public class Vertex : IEquatable<Vertex>
{
    public int Id;
    public Vector3 Pos;
    public List<Triangle> Triangles = new();
    public List<Vertex> Connected = new();

    public Vertex(Vector3 pos)
    {
        Pos = pos;
        Id = GetHashCode();
    }

    public void SetTriangle(Triangle triangle)
    {
        if (!Triangles.Contains(triangle))
        {
            Triangles.Add(triangle);
        }
    }

    public void SetConnected(Vertex v)
    {
        if (!Connected.Contains(v))
        {
            Connected.Add(v);
        }

        if (!v.Connected.Contains(this))
        {
            v.Connected.Add(this);
        }
    }

    public void Unlink(Vertex v)
    {
        v.Connected.Remove(this);
        Connected.Remove(v);
    }

    public bool Equals(Vertex obj)
    {
        return obj != null && MeshObject.SameInTolerance(Pos, obj.Pos);
    }

    public override int GetHashCode()
    {
        return Pos.X.GetHashCode() + 3 * Pos.Y.GetHashCode() + 5 * Pos.Z.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is Vertex vertex && Equals(vertex);
    }

    public override string ToString()
    {
        return $"V{Id}[{Pos.X:F4},{Pos.Y:F4},{Pos.Z:F4}]";
    }
}