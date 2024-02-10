using System;
using System.Collections.Generic;
using FlaxEngine;

namespace ProceduralStructures;

public class Triangle : IEquatable<Triangle>
{
    public Vertex V0, V1, V2;
    public Vector2 Uv0, Uv1, Uv2;

    public Vector3 Normal
    {
        get
        {
            var ab = V1.Pos - V0.Pos;
            var ac = V2.Pos - V0.Pos;
            return Vector3.Cross(ab, ac).Normalized;
        }
    }

    public Vector3 Center => (V0.Pos + V1.Pos + V2.Pos) / 3;

    public float Area => Vector3.Cross(V1.Pos - V0.Pos, V2.Pos - V0.Pos).Length / 2;

    public Triangle(Vertex v0, Vertex v1, Vertex v2)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
        V0.SetTriangle(this);
        V1.SetTriangle(this);
        V2.SetTriangle(this);
    }

    public Triangle(TEdge a, TEdge b, TEdge c)
    {
        V0 = a.A;
        V1 = b.A;
        V2 = c.A;
        ResetTriangleLinks();
    }

    public override int GetHashCode()
    {
        return V0.GetHashCode() + V1.GetHashCode() + V2.GetHashCode();
    }

    public void FlipNormal()
    {
        (V1, V2) = (V2, V1);
    }

    public Vertex[] GetVertices()
    {
        return new[] { V0, V1, V2 };
    }

    public void SetVertex(int idx, Vertex v)
    {
        RemoveTriangleLinks();
        switch (idx)
        {
            case 0:
                V0 = v;
                break;
            case 1:
                V1 = v;
                break;
            case 2:
                V2 = v;
                break;
            default:
                ResetTriangleLinks();
                throw new InvalidOperationException("Index must be 0, 1 or 2 in SetVertex");
        }

        ResetTriangleLinks();
    }

    public bool Equals(Triangle other)
    {
        return other != null && GetHashCode() == other.GetHashCode() && Normal == other.Normal &&
               GetCommonVertices(other) == 3;
    }

    public override bool Equals(object obj)
    {
        if (obj is Triangle triangle) return Equals(triangle);
        return false;
    }

    public Triangle GetDuplicate()
    {
        var other = V0.Triangles.Find(t => t != this && Equals(t));
        if (other != null) return other;
        other = V1.Triangles.Find(t => t != this && Equals(t));
        if (other != null) return other;
        other = V2.Triangles.Find(t => t != this && Equals(t));
        return other;
    }

    public bool PointIsAbove(Vector3 point)
    {
        // barycentric coordinate check
        // see https://gamedev.stackexchange.com/questions/28781/easy-way-to-project-point-onto-triangle-or-plane
        var u = V1.Pos - V0.Pos;
        var v = V2.Pos - V0.Pos;
        var n = Vector3.Cross(u, v);
        var w = point - V0.Pos;
        var gamma = Vector3.Dot(Vector3.Cross(u, w), n) / Vector3.Dot(n, n);
        var beta = Vector3.Dot(Vector3.Cross(w, v), n) / Vector3.Dot(n, n);
        var alpha = 1 - gamma - beta;
        return (alpha is >= 0 and <= 1 &&
                beta is >= 0 and <= 1 &&
                gamma is >= 0 and <= 1);
    }

    /// <summary>checks whether any vertex is contained by this triangle with a tolerance(+-) on the normal</summary>
    public bool ContainsAnyVertex(IEnumerable<Vertex> other, out Vertex hit, float heightTolerance = 0.2f)
    {
        var n = Normal;
        foreach (var v in other)
        {
            if (V0 == v || V1 == v || V2 == v)
            {
                continue;
            }

            if (Vector3.Dot(Vector3.Cross(n, V1.Pos - V0.Pos), v.Pos - V0.Pos) < 0)
            {
                continue;
            }

            if (Vector3.Dot(Vector3.Cross(n, V2.Pos - V1.Pos), v.Pos - V1.Pos) < 0)
            {
                continue;
            }

            if (Vector3.Dot(Vector3.Cross(n, V0.Pos - V2.Pos), v.Pos - V2.Pos) < 0)
            {
                continue;
            }

            if (Mathf.Abs(Vector3.Dot((v.Pos - V0.Pos), Normal)) < heightTolerance)
            {
                hit = v;
                return true;
            }
        }

        hit = null;
        return false;
    }

    /// <summary>checks whether any vertex is contained by this triangle with a tolerance(+-) on the normal</summary>
    public bool ContainsAnyVertex(IEnumerable<Vertex> other, float heightTolerance = 0.2f)
    {
        return ContainsAnyVertex(other, out _, heightTolerance);
    }

    /// <summary>checks whether the point is in the half space above this triangle</summary>
    public bool FacesPoint(Vector3 point)
    {
        return Vector3.Dot(Normal, point - V0.Pos) > 0; //Face.Epsilon;
    }

    public static bool FacesPoint(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 point)
    {
        return Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), point - v0) > 0; //Face.Epsilon;
    }

    public int GetCommonVertices(Triangle other)
    {
        var commonVertices = 0;
        foreach (var v in GetVertices())
        {
            foreach (var w in other.GetVertices())
            {
                if (v.Equals(w))
                {
                    commonVertices++;
                }
            }
        }

        return commonVertices;
    }

    public bool SharesTurningEdge(Triangle other)
    {
        var commonVertices = 0;
        var midx = 0;
        var oidx = 0;
        var mv = GetVertices();
        var ov = other.GetVertices();
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                if (!mv[i].Equals(ov[j])) continue;
                commonVertices++;
                if (commonVertices < 2)
                {
                    midx = i;
                    oidx = j;
                }
                else
                {
                    if (i == midx + 1 && j == (oidx + 1) % 3)
                    {
                        return true;
                    }

                    if (i == midx + 2 && j == (oidx + 2) % 3)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool SharesEdgeWith(Triangle other)
    {
        return GetCommonVertices(other) == 2;
    }

    public List<TEdge> GetNonManifoldEdges()
    {
        var result = new List<TEdge>();
        var v0v1 = V0.Triangles.FindAll(t => V1.Triangles.Contains(t));
        var v1v2 = V1.Triangles.FindAll(t => V2.Triangles.Contains(t));
        var v2v0 = V2.Triangles.FindAll(t => V0.Triangles.Contains(t));
        if (v0v1.Count != 2)
        {
            result.Add(new TEdge(V0, V1, this));
        }

        if (v1v2.Count != 2)
        {
            result.Add(new TEdge(V1, V2, this));
        }

        if (v2v0.Count != 2)
        {
            result.Add(new TEdge(V2, V0, this));
        }

        return result;
    }

    public float MaxAngle()
    {
        var phi = Vector3.Angle(V1.Pos - V0.Pos, V2.Pos - V0.Pos);
        phi = Mathf.Max(phi, Vector3.Angle(V2.Pos - V1.Pos, V0.Pos - V1.Pos));
        phi = Mathf.Max(phi, Vector3.Angle(V1.Pos - V2.Pos, V0.Pos - V2.Pos));
        return phi;
    }

    public void RemoveTriangleLinks()
    {
        V0.Triangles.Remove(this);
        V1.Triangles.Remove(this);
        V2.Triangles.Remove(this);
    }

    public void ResetTriangleLinks()
    {
        V0.SetTriangle(this);
        V1.SetTriangle(this);
        V2.SetTriangle(this);
    }

    public List<Triangle> GetAdjacentTriangles()
    {
        var result = new HashSet<Triangle>();
        V0.Triangles.ForEach(t =>
        {
            if (V1.Triangles.Contains(t) || V2.Triangles.Contains(t)) result.Add(t);
        });
        V1.Triangles.ForEach(t =>
        {
            if (V2.Triangles.Contains(t)) result.Add(t);
        });
        result.Remove(this);
        return new List<Triangle>(result);
    }

    public List<Triangle> GetAdjacentPlanarTriangles(float tolerance = 1f)
    {
        var result = new HashSet<Triangle>();
        var n = Normal;
        V0.Triangles.ForEach(t => result.Add(t));
        V1.Triangles.ForEach(t => result.Add(t));
        V2.Triangles.ForEach(t => result.Add(t));
        result.Remove(this);
        result.RemoveWhere(t => Vector3.Angle(t.Normal, n) > tolerance);
        return new List<Triangle>(result);
    }

    public Triangle GetNearestAdjacentTriangleByNormal(float tolerance = 5f)
    {
        var neighbors = GetAdjacentTriangles();
        if (neighbors.Count == 0) return null;
        var bestMatch = 0;
        var minAngle = float.MaxValue;
        for (var i = 0; i < neighbors.Count; i++)
        {
            var angle = Vector3.Angle(neighbors[i].Normal, Normal);
            if (angle < minAngle)
            {
                minAngle = angle;
                bestMatch = i;
            }
        }

        if (minAngle <= tolerance)
            return neighbors[bestMatch];
        return null;
    }

    public void SetUVProjected(float uvScale)
    {
        var n = Normal;
        var dlr = Mathf.Abs(Vector3.Dot(n, Vector3.Left));
        var dfb = Mathf.Abs(Vector3.Dot(n, Vector3.Backward));
        var dud = Mathf.Abs(Vector3.Dot(n, Vector3.Up));
        //float dlr = Vector3.Dot(n, Vector3.left);
        //float dfb = Vector3.Dot(n, Vector3.back);
        //float dud = Vector3.Dot(n, Vector3.up);
        var a = V0.Pos;
        var b = V1.Pos;
        var c = V2.Pos;
        Uv0 = new Vector2((dlr * a.Z + dfb * a.X + dud * a.X) * uvScale,
            (dlr * a.Y + dfb * a.Y + dud * a.Z) * uvScale);
        Uv1 = new Vector2((dlr * b.Z + dfb * b.X + dud * b.X) * uvScale,
            (dlr * b.Y + dfb * b.Y + dud * b.Z) * uvScale);
        Uv2 = new Vector2((dlr * c.Z + dfb * c.X + dud * c.X) * uvScale,
            (dlr * c.Y + dfb * c.Y + dud * c.Z) * uvScale);
    }

    public Triangle SetUVTunnelProjection(Vector3 tunnelCenter, Vector3 direction, float uOffset, float uScale,
        float vScale)
    {
        Uv0 = UVTunnelProjection(V0.Pos, tunnelCenter, direction, uOffset, uScale, vScale);
        Uv1 = UVTunnelProjection(V1.Pos, tunnelCenter, direction, uOffset, uScale, vScale);
        Uv2 = UVTunnelProjection(V2.Pos, tunnelCenter, direction, uOffset, uScale, vScale);
        return this;
    }

    private Vector2 UVTunnelProjection(Vector3 vec, Vector3 tunnelCenter, Vector3 direction, float uOffset,
        float uScale, float vScale)
    {
        var dot = Vector3.Dot(vec - tunnelCenter, direction);
        var ms = tunnelCenter + dot * direction;
        var u = (dot + uOffset) * uScale;
        var lineToVertex = vec - ms;
        var vertexToCylinderDirection = lineToVertex.Normalized;
        Vector3 directionToUp = new(-vertexToCylinderDirection.Y, vertexToCylinderDirection.X, 0);

        var v = Mathf.Abs(Mathf.Atan2(directionToUp.Y, directionToUp.X) / Mathf.Pi / 2);
        v *= vScale * -Mathf.Pi * 2; // * lineToVertex.magnitude;
        return new Vector2(u, v);
    }

    public Triangle SetUVCylinderProjection(Vector3 cylinderCenter, Vector3 direction, float uOffset, float uScale,
        float vScale)
    {
        var dot = Vector3.Dot(Center - cylinderCenter, direction);
        var ms = cylinderCenter + dot * direction;
        var u = -(dot + uOffset) * uScale;
        var lineToVertex = Center - ms;
        var vertexToCylinderDirection = lineToVertex.Normalized;
        var v = Mathf.Atan2(vertexToCylinderDirection.Y, vertexToCylinderDirection.X) / Mathf.Pi / 2;
        if (v < 0) v += 1;
        var favorOne = v > 0.5f;

        Uv0 = UVCylinderProjection(V0.Pos, cylinderCenter, direction, favorOne, uOffset, uScale, vScale);
        Uv1 = UVCylinderProjection(V1.Pos, cylinderCenter, direction, favorOne, uOffset, uScale, vScale);
        Uv2 = UVCylinderProjection(V2.Pos, cylinderCenter, direction, favorOne, uOffset, uScale, vScale);
        return this;
    }

    private Vector2 UVCylinderProjection(Vector3 vertex, Vector3 cylinderCenter, Vector3 direction, bool favorOne,
        float uOffset, float uScale, float vScale)
    {
        var dot = Vector3.Dot(vertex - cylinderCenter, direction);
        var ms = cylinderCenter + dot * direction;
        var u = (dot + uOffset) * uScale;
        var lineToVertex = vertex - ms;
        var vertexToCylinderDirection = lineToVertex.Normalized;
        var v = Mathf.Atan2(vertexToCylinderDirection.Y, vertexToCylinderDirection.X) / Mathf.Pi / 2;
        v *= vScale * -Mathf.Pi * 2; // * lineToVertex.magnitude;
        return new Vector2(u, v);
    }

    public bool RayHit(Vector3 origin, Vector3 direction, bool ignoreBack, out bool fromBack,
        out Vector3 intersection)
    {
        var edge1 = V1.Pos - V0.Pos;
        var edge2 = V2.Pos - V0.Pos;
        fromBack = false;
        intersection = Vector3.Zero;
        if (GeometryTools.RayHitTriangle(origin, direction, V0.Pos, V1.Pos, V2.Pos, out intersection, out _))
        {
            return true;
        }

        if (!ignoreBack)
        {
            fromBack = true;
            if (GeometryTools.RayHitTriangle(origin, direction, V0.Pos, V2.Pos, V1.Pos, out intersection, out _))
            {
                return true;
            }
        }

        return false;
    }

    public bool EdgeIntersection(Vector3 a, Vector3 b, out Vector3 intersection)
    {
        if (MeshObject.SameInTolerance(V0.Pos, a) || MeshObject.SameInTolerance(V1.Pos, a) ||
            MeshObject.SameInTolerance(V2.Pos, a))
        {
            intersection = a;
            return true;
        }

        if (MeshObject.SameInTolerance(V0.Pos, b) || MeshObject.SameInTolerance(V1.Pos, b) ||
            MeshObject.SameInTolerance(V2.Pos, b))
        {
            intersection = b;
            return true;
        }

        if (GeometryTools.PointOnEdge(V0.Pos, a, b, out var rayHitLength))
        {
            intersection = V0.Pos;
            return true;
        }

        if (GeometryTools.PointOnEdge(V1.Pos, a, b, out rayHitLength))
        {
            intersection = V1.Pos;
            return true;
        }

        if (GeometryTools.PointOnEdge(V2.Pos, a, b, out rayHitLength))
        {
            intersection = V2.Pos;
            return true;
        }

        if (GeometryTools.RayHitTriangle(a, b - a, V0.Pos, V1.Pos, V2.Pos, out intersection, out rayHitLength))
        {
            if (rayHitLength <= 1)
            {
                return true;
            }
        }

        if (GeometryTools.RayHitTriangle(a, b - a, V0.Pos, V2.Pos, V1.Pos, out intersection, out rayHitLength))
        {
            if (rayHitLength <= 1)
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        return $"T{GetHashCode()}[{V0},{V1},{V2}]";
    }
}