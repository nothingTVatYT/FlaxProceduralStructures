using FlaxEngine;

namespace Game.ProceduralStructures {
    public static class GeometryTools {
        public const float Epsilon = 1e-3f;
        public const float EpsilonSquared = Epsilon * Epsilon;

        public static bool RayHitTriangle(Vector3 origin, Vector3 direction, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 intersection, out float rayHitLength) {
            var edge1 = v1-v0;
            var edge2 = v2-v0;
            var normal = Vector3.Cross(edge1, edge2);
            intersection = Vector3.Zero;
            rayHitLength = 0;
            if (Vector3.Dot(normal, origin-v0) < 0) {
                // origin is on the backside
                return false;
            }
            var h = Vector3.Cross(direction, edge2);
            var aa = Vector3.Dot(edge1, h);
            if (aa > -1e-3 && aa < 1e-3) {
                // ray is parallel to triangle
                return false;
            }
            var ff = 1f/aa;
            var s = origin - v0;
            var uu = ff * Vector3.Dot(s, h);
            if (uu < 0 || uu > 1)
                return false;
            var q = Vector3.Cross(s, edge1);
            var vv = ff * Vector3.Dot(direction, q);
            if (vv < 0 || uu+vv > 1)
                return false;
            var tt = ff * Vector3.Dot(edge2, q);
            if (tt <= 1e-3f) return false;
            intersection = origin + direction * tt;
            rayHitLength = tt;
            return true;
        }

        public static bool EdgeEdgeIntersectIgnoreEnds(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 intersection) {
            if (!LineLineIntersect(a, b, c, d, out intersection, out var m, out var n)) return false;
            switch (m)
            {
                case > 0 and < 1 when n is >= 0 and <= 1:
                case >= 0 and <= 1 when n is > 0 and < 1:
                    return true;
            }
            return false;
        }

        public static bool EdgeEdgeIntersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 intersection) {
            if (LineLineIntersect(a, b, c, d, out intersection, out var m, out var n)) {
                return m is >= 0 and <= 1 && n is >= 0 and <= 1;
            }
            return false;
        }

        public static bool EdgeLineIntersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 intersection) {
            if (LineLineIntersect(a, b, c, d, out intersection, out var m, out _)) {
                return m is >= 0 and <= 1;
            }
            return false;
        }

        public static bool LineLineIntersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 intersection, out float m, out float n) {
            m = 0;
            n = 0;
            intersection = Vector3.Zero;
            // direction from a to b
            var u = b-a;
            // direction from c to d
            var v = d-c;
            // an intersection must fulfil: a+m*u=c+n*v
            // we get three equations by splitting the x, y and z components and we can eliminate m
            // by reordering the equations so that we can set the one for x and y in one equation and solve
            // for n: n=(c.y*u.x-a.y*u.x-u.y*c.x+a.x*u.y) / (v.x*u.y - v.y*u.x)
            // m=(c.y+n*v.y-a.y) / u.y
            var divisorXY = v.X*u.Y - v.Y*u.X;
            var divisorXZ = v.X*u.Z - v.Z*u.X;
            var divisorYZ = v.Y*u.Z - v.Z*u.Y;
            if (divisorXY == 0 && divisorXZ == 0 && divisorYZ == 0) {
                // cannot devide by 0 => there is no solution
                //Debug.Log("no intersection in XY,XZ and YZ");
                return false;
            }
            if (divisorXY != 0) {
                n = (c.Y*u.X-a.Y*u.X-u.Y*c.X+a.X*u.Y) / divisorXY;
            } else if (divisorXZ != 0) {
                n = (c.Z*u.X-a.Z*u.X-u.Z*c.X+a.X*u.Z) / divisorXZ;
            } else {
                n = (c.Z*u.Y-a.Z*u.Y-u.Z*c.Y+a.Y*u.Z) / divisorYZ;
            }
            if (u.Y != 0) {
                m = (c.Y+n*v.Y-a.Y) / u.Y;
            } else if (u.X != 0) {
                m = (c.X+n*v.X-a.X) / u.X;
            } else {
                m = (c.Z+n*v.Z-a.Z) / u.Z;
            }
            // intersection point according to first line equation
            var h = a + m * u;
            // intersection point according to second line equation
            var i = c + n * v;
            if ((h-i).LengthSquared < 1e-3f) {
                // good enough. we have a valid solution in 3d
                intersection = a + m*u;
                return true;
            }
            //Debug.Log("no intersection but found h=" + h + " and i=" + i);
            return false;
        }

        public static bool PointOnEdge(Vector3 p, Vector3 a, Vector3 b, out float m) {
            if (!PointOnLine(p, a, b, out m)) return false;
            return m is >= 0 and <= 1;
        }
        
        public static bool PointOnLine(Vector3 p, Vector3 a, Vector3 b, out float m) {
            var u = b-a;
            m = 0;
            if (u.X != 0) {
                m = (p.X-a.X)/u.X;
            } else if (u.Y != 0) {
                m = (p.Y-a.Y)/u.Y;
            } else if (u.Z != 0) {
                m = (p.Z-a.Z)/u.Z;
            }
            var iv = a+m*u;
            return (iv-p).LengthSquared < EpsilonSquared;
        }
    }
}