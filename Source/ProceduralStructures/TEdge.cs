using System;
using System.Collections.Generic;

namespace ProceduralStructures;

public class TEdge : IEquatable<TEdge>
{
    public Vertex A;
    public Vertex B;
    public List<Triangle> Triangles = new();

    public TEdge(Vertex a, Vertex b, params Triangle[] t)
    {
        A = a;
        B = b;
        if (t != null)
        {
            Triangles.AddRange(t);
        }
    }

    public bool Equals(TEdge other)
    {
        return other != null && GetHashCode() == other.GetHashCode() && A.Equals(other.A) && B.Equals(other.B);
    }

    public override int GetHashCode()
    {
        return A.GetHashCode() + B.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is TEdge edge) return Equals(edge);
        return false;
    }

    public void ResetEdgeLinks()
    {
        A.SetConnected(B);
        B.SetConnected(A);
    }

    public void RemoveEdgeLinks()
    {
        A.Connected.Remove(B);
        B.Connected.Remove(A);
    }


    public override string ToString()
    {
        return "E[" + A + "," + B + "]";
    }

    public void Flip()
    {
        (A, B) = (B, A);
    }
}