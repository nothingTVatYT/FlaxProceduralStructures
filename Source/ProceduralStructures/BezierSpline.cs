using System;
using FlaxEngine;
using System.Collections.Generic;

namespace ProceduralStructures;

public class BezierSpline
{
    private List<WayPoint> _points;
    private Vector3[] _controlPoints1;
    private Vector3[] _controlPoints2;
    private float[] _estimatedSegmentLength;

    public float EstimatedLength { get; }

    public BezierSpline(List<WayPoint> points)
    {
        _points = points;
        _estimatedSegmentLength = new float[points.Count - 1];
        EstimatedLength = 0;
        var vectors = new List<Vector3>(points.Count);
        foreach (var wp in points)
        {
            vectors.Add(wp.Position);
        }

        GetCurveControlPoints(vectors, out _controlPoints1, out _controlPoints2);
        for (var i = 0; i < points.Count - 1; i++)
        {
            _estimatedSegmentLength[i] = EstimateSegmentLength(i, 10);
            EstimatedLength += _estimatedSegmentLength[i];
        }

        var firstEstimation = EstimatedLength;
        EstimatedLength = 0;
        for (var i = 0; i < points.Count - 1; i++)
        {
            var st = Mathf.CeilToInt(_estimatedSegmentLength[i] / firstEstimation * 10);
            _estimatedSegmentLength[i] = EstimateSegmentLength(i, st);
            EstimatedLength += _estimatedSegmentLength[i];
        }
    }

    public Vector3 GetVertex(float t)
    {
        var segment = GetSegment(t, out _, out var segmentT);
        if (segment > _points.Count - 2) segment = _points.Count - 2;
        return GetInterpolatedPoint(_points[segment].Position, _controlPoints1[segment], _controlPoints2[segment],
            _points[segment + 1].Position, segmentT);
    }

    public Tangent GetTangent(float t)
    {
        var segment = GetSegment(t, out _, out var segmentT);
        if (segment > _points.Count - 2) segment = _points.Count - 2;
        var tangent = GetInterpolatedTangent(_points[segment].Position, _controlPoints1[segment],
            _controlPoints2[segment], _points[segment + 1].Position, segmentT);
        tangent.RelativePosition = t;
        tangent.ScaleWidth = Mathf.Lerp(_points[segment].ScaleWidth, _points[segment + 1].ScaleWidth, segmentT);
        tangent.ScaleHeight = Mathf.Lerp(_points[segment].ScaleHeight, _points[segment + 1].ScaleHeight, segmentT);
        return tangent;
    }

    private float EstimateSegmentLength(int segment, int steps)
    {
        float result = 0;
        var prev = _points[segment].Position;
        for (var i = 1; i <= steps; i++)
        {
            var t = i * 1f / steps;
            var v = GetInterpolatedPoint(_points[segment].Position, _controlPoints1[segment], _controlPoints2[segment],
                _points[segment + 1].Position, t);
            result += (v - prev).Length;
            prev = v;
        }

        return result;
    }

    private int GetSegment(float t, out float relativeSegmentStart, out float relativeT)
    {
        relativeSegmentStart = 0;
        relativeT = 0;
        for (var i = 0; i < _estimatedSegmentLength.Length; i++)
        {
            var relativeSegmentEnd = relativeSegmentStart + _estimatedSegmentLength[i] / EstimatedLength;
            if (t >= relativeSegmentStart && t <= relativeSegmentEnd)
            {
                relativeT = (t - relativeSegmentStart) / (relativeSegmentEnd - relativeSegmentStart);
                return i;
            }

            relativeSegmentStart = relativeSegmentEnd;
        }

        relativeT = 1;
        return _estimatedSegmentLength.Length - 1;
    }

    private static Vector3 GetInterpolatedPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var u = 1f - t;
        return p0 * u * u * u + p1 * 3 * u * u * t + p2 * 3 * u * t * t + p3 * t * t * t;
    }

    private static Tangent GetInterpolatedTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 a, b;
        a = GetInterpolatedPoint(p0, p1, p2, p3, t >= 0.05f ? t - 0.05f : t);
        b = GetInterpolatedPoint(p0, p1, p2, p3, t < 1f ? t + 0.05f : t);

        var direction = (b - a).Normalized;
        return new Tangent(GetInterpolatedPoint(p0, p1, p2, p3, t), direction);
    }

    // from https://www.codeproject.com/Articles/31859/Draw-a-Smooth-Curve-through-a-Set-of-2D-Points-wit
    /// <summary>
    /// Get open-ended Bezier Spline Control Points.
    /// </summary>
    /// <param name="knots">Input Knot Bezier spline points.</param>
    /// <param name="firstControlPoints">Output First Control points
    /// array of knots.Length - 1 length.</param>
    /// <param name="secondControlPoints">Output Second Control points
    /// array of knots.Length - 1 length.</param>
    /// <exception cref="ArgumentNullException"><paramref name="knots"/>
    /// parameter must be not null.</exception>
    /// <exception cref="ArgumentException"><paramref name="knots"/>
    /// array must contain at least two points.</exception>
    private static void GetCurveControlPoints(List<Vector3> knots,
        out Vector3[] firstControlPoints, out Vector3[] secondControlPoints)
    {
        var n = knots.Count - 1;
        if (n == 1)
        {
            // Special case: Bezier curve should be a straight line.
            firstControlPoints = new Vector3[1];
            // 3P1 = 2P0 + P3
            firstControlPoints[0] = (knots[0] * 2 + knots[1]) / 3f;

            secondControlPoints = new Vector3[1];
            // P2 = 2P1 – P0
            secondControlPoints[0] = 2 * firstControlPoints[0] - knots[0];
            return;
        }

        // Calculate first Bezier control points
        // Right hand side vector
        var rhs = new float[n];

        // Set right hand side X values
        for (var i = 1; i < n - 1; ++i)
            rhs[i] = 4 * knots[i].X + 2 * knots[i + 1].X;
        rhs[0] = knots[0].X + 2 * knots[1].X;
        rhs[n - 1] = (8 * knots[n - 1].X + knots[n].X) / 2f;
        // Get first control points X-values
        var x = GetFirstControlPoints(rhs);

        // Set right hand side Y values
        for (var i = 1; i < n - 1; ++i)
            rhs[i] = 4 * knots[i].Y + 2 * knots[i + 1].Y;
        rhs[0] = knots[0].Y + 2 * knots[1].Y;
        rhs[n - 1] = (8 * knots[n - 1].Y + knots[n].Y) / 2.0f;
        // Get first control points Y-values
        var y = GetFirstControlPoints(rhs);

        // Set right hand side Z values
        for (var i = 1; i < n - 1; ++i)
            rhs[i] = 4 * knots[i].Z + 2 * knots[i + 1].Z;
        rhs[0] = knots[0].Z + 2 * knots[1].Z;
        rhs[n - 1] = (8 * knots[n - 1].Z + knots[n].Z) / 2.0f;
        // Get first control points Y-values
        var z = GetFirstControlPoints(rhs);

        // Fill output arrays.
        firstControlPoints = new Vector3[n];
        secondControlPoints = new Vector3[n];
        for (var i = 0; i < n; ++i)
        {
            // First control point
            firstControlPoints[i] = new Vector3(x[i], y[i], z[i]);
            // Second control point
            if (i < n - 1)
                secondControlPoints[i] = new Vector3(
                    2 * knots[i + 1].X - x[i + 1],
                    2 * knots[i + 1].Y - y[i + 1],
                    2 * knots[i + 1].Z - z[i + 1]);
            else
                secondControlPoints[i] = new Vector3(
                    (knots[n].X + x[n - 1]) / 2,
                    (knots[n].Y + y[n - 1]) / 2,
                    (knots[n].Z + z[n - 1]) / 2);
        }
    }

    /// <summary>
    /// Solves a tridiagonal system for one of coordinates (x or y)
    /// of first Bezier control points.
    /// </summary>
    /// <param name="rhs">Right hand side vector.</param>
    /// <returns>Solution vector.</returns>
    private static float[] GetFirstControlPoints(float[] rhs)
    {
        var n = rhs.Length;
        var x = new float[n]; // Solution vector.
        var tmp = new float[n]; // Temp workspace.

        var b = 2f;
        x[0] = rhs[0] / b;
        for (var i = 1; i < n; i++) // Decomposition and forward substitution.
        {
            tmp[i] = 1 / b;
            b = (i < n - 1 ? 4f : 3.5f) - tmp[i];
            x[i] = (rhs[i] - x[i - 1]) / b;
        }

        for (var i = 1; i < n; i++)
            x[n - i - 1] -= tmp[n - i] * x[n - i]; // Backsubstitution.

        return x;
    }
}