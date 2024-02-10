using System;
using FlaxEngine;

namespace Game.ProceduralStructures;

public class Edge : IEquatable<Edge>
{
    public Vector3 A;
    public Vector3 B;

    public Edge(Vector3 a, Vector3 b) {
        A = a;
        B = b;
    }

    public Edge Flipped() {
        return new Edge(B, A);
    }

    public bool OppositeDirection(Edge other) {
        var d = Vector3.Dot(B-A, other.B-other.A);
        return d < 0;
    }
    
    public override bool Equals(object other)
    {
        return other is Edge o && Equals(o);
    }

    public override int GetHashCode()
    {
        return A.GetHashCode() + 31 * B.GetHashCode();
    }

    public override string ToString()
    {
        return $"E(a={A},b={B})";
    }

    public bool Equals(Edge other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return (A-other.A).LengthSquared < 1e-3f && (B-other.B).LengthSquared < 1e-3f;
    }
}