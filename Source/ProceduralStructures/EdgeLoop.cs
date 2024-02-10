using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProceduralStructures;

public class EdgeLoop : IEquatable<EdgeLoop>
{
    private List<Vertex> _vertices;
    private int _hashcode;

    public ReadOnlyCollection<Vertex> Vertices => _vertices.AsReadOnly();

    public int Count => _vertices?.Count ?? 0;

    /// <summary>This is an unordered edge loop, i.e. vertices in the opposite order is considered the same</summary>
    /// The list of vertices is copied because it's reordered and could be reversed
    public EdgeLoop(IEnumerable<Vertex> vertices)
    {
        _vertices = new List<Vertex>(vertices);
        ReorderList();
        CalculateHashCode();
    }

    private void ReorderList()
    {
        var idxFirst = 0;
        var xyz = float.MaxValue;
        for (var i = 0; i < _vertices.Count; i++)
        {
            var v = _vertices[i];
            var sum = v.Pos.X + v.Pos.Y + v.Pos.Z;
            if (sum < xyz)
            {
                xyz = sum;
                idxFirst = i;
            }
        }

        var c = new CircularReadonlyList<Vertex>(_vertices)
        {
            IndexOffset = idxFirst
        };
        if (c.Count > 1)
        {
            if (c[-1].Id > c[1].Id)
            {
                c.Reverse();
            }
        }

        // copy the list from the circular view using the index operator to get the specified order
        var reordered = new List<Vertex>(c.Count);
        reordered.AddRange(c);
        _vertices = reordered;
    }

    private void CalculateHashCode()
    {
        var hash = _vertices.Aggregate(0, (current, v) => (current << 1) + v.GetHashCode());
        _hashcode = hash;
    }

    public override int GetHashCode()
    {
        return _hashcode;
    }

    private bool ItemsMatch(EdgeLoop other)
    {
        if (Count != other.Count)
        {
            return false;
        }

        return !_vertices.Where((t, i) => !t.Equals(other._vertices[i])).Any();
    }

    public override bool Equals(object obj)
    {
        return obj is EdgeLoop loop && Equals(loop);
    }

    public bool Equals(EdgeLoop other)
    {
        return other != null && GetHashCode() == other.GetHashCode() && ItemsMatch(other);
    }
}