using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace Game.ProceduralStructures {
    public class Face
    {
        public Vector3 A,B,C,D;
        public Vector2 UvA,UvB,UvC,UvD;
        public Vector3 Normal => Vector3.Cross(B-A, (IsTriangle ? C : D)-A).Normalized;
        public bool IsTriangle;
        public int Tags;
        public float SortOrder;
        public Material Material;

        public const float Epsilon = 1e-3f;

        public Face() {}
        public Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            A = a;
            B = b;
            C = c;
            D = d;
            IsTriangle = false;
            // initialize UV
            SetUvFront(1, 1);
        }

        public bool IsValid() {
            if (!IsTriangle) {
                var distinctVertices = 4;
                if ((A-B).Length < Epsilon) {
                    distinctVertices--;
                    B = C;
                    C = D;
                    if ((A-B).Length < Epsilon) {
                        return false;
                    }
                }
                if ((A-C).Length < Epsilon) {
                    distinctVertices--;
                    C = D;
                }
                if ((B-C).Length < Epsilon) {
                    distinctVertices--;
                    C = D;
                    if ((B-C).Length < Epsilon) {
                        return false;
                    }
                    if ((A-C).Length < Epsilon) {
                        return false;
                    }
                }
                if (distinctVertices == 4 && ((C-D).Length < Epsilon || (D-A).Length < Epsilon)) {
                    distinctVertices--;
                }
                if (distinctVertices == 4) {
                    return true;
                }
                if (distinctVertices == 3) {
                    IsTriangle = true;
                    return true;
                }
                return false;
            }
            if (IsTriangle) {
                if ((A-B).Length < Epsilon || (B-C).Length < Epsilon || (C-A).Length < Epsilon)
                    return false;
            }
            return true;
        }

        public Face(Vector3 a, Vector3 b, Vector3 c) {
            A = a;
            B = b;
            C = c;
            IsTriangle = true;
            // initialize UV
            SetUvFront(1, 1);
        }

        public static List<Face> PolygonToTriangles(List<Vector3> l) {
            var result = new List<Face>();
            if (l.Count < 3) return result;
            for (var i = 1; i < l.Count - 1; i++) {
                var j = i < l.Count-1 ? i+1 : 1;
                var f = new Face(l[0], l[i], l[j]);
                result.Add(f);
            }
            return result;
        }

        public static List<Face> PolygonToTriangleFan(List<Vector3> l) {
            var centroid = Builder.FindCentroid(l);
            var result = new List<Face>();
            if (l.Count < 3) return result;
            for (var i = 0; i < l.Count; i++) {
                var j = i < l.Count-1 ? i+1 : 0;
                var f = new Face(centroid, l[i], l[j]);
                f.SetUvProjected(1);
                result.Add(f);
            }

            return result;
        }

        public static Face QuadOnPlane(Vector3 o, Vector3 normal, float scale) {
            var left = Vector3.Cross(normal, Vector3.Up);
            var localUp = Vector3.Cross(normal, left) * scale;
            left *= scale;
            return new Face(o + left - localUp, o + left + localUp, o  - left + localUp, o - left - localUp).SetUvForSize(1);
        }

        public Face(Edge edge1, Edge edge2) {
            A = edge1.A;
            B = edge1.B;
            if (edge1.OppositeDirection(edge2)) {
                C = edge2.A;
                D = edge2.B;
            } else {
                C = edge2.B;
                D = edge2.A;
            }
            SetUvFront(1, 1);
        }

        public static Face CreateXzPlane(float width, float length) {
            return new Face(
                new Vector3(-width/2, 0, -length/2),
                new Vector3(-width/2, 0, length/2),
                new Vector3(width/2, 0, length/2),
                new Vector3(width/2, 0, -length/2));
        }

        public Vector3[] GetVertices()
        {
            return IsTriangle ? new[] { A, B, C } : new[] { A, B, C, D };
        }

        public List<Vector3> GetVerticesList()
        {
            return IsTriangle ? new List<Vector3> { A, B, C } : new List<Vector3> { A, B, C, D };
        }
        
        public Face DeepCopy() {
            var n = new Face();
            n.A = A;
            n.B = B;
            n.C = C;
            n.D = D;
            n.IsTriangle = IsTriangle;
            n.UvA = UvA;
            n.UvB = UvB;
            n.UvC = UvC;
            n.UvD = UvD;
            n.Tags = Tags;
            n.SortOrder = SortOrder;
            n.Material = Material;
            return n;
        }

        public Face Rotate(Quaternion rot) {
            A = A * rot;
            B = B * rot;
            C = C * rot;
            if (!IsTriangle) {
                D = D * rot;
            }
            return this;
        }

        public Face InvertNormals() {
            if (IsTriangle) {
                (A, C) = (C, A);
            } else {
                var t = A;
                A = D;
                D = t;
                t = B;
                B = C;
                C = t;
            }
            return this;
        }

        public bool IsPlanar() {
            if (IsTriangle) return true;
            var n1 = Vector3.Cross(B-A, D-A).Normalized;
            var n2 = Vector3.Cross(B-C, D-C).Normalized;
            return Mathf.Abs(Vector3.Dot(n1, n2) - 1f) < 1e-3;
        }

        public List<Edge> GetEdges() {
            var result = new List<Edge>
            {
                new(A, B),
                new(B, C)
            };
            if (IsTriangle) {
                result.Add(new Edge(C, A));
            } else {
                result.Add(new Edge(C, D));
                result.Add(new Edge(D, A));
            }
            return result;
        }

        public bool SharesEdgeWith(Face other) {
            var otherEdges = other.GetEdges();
            return GetEdges().Any(edge => otherEdges.Contains(edge.Flipped()));
        }

        public static bool RayHitTriangle(Vector3 origin, Vector3 direction, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 intersection) {
            var edge1 = v1-v0;
            var edge2 = v2-v0;
            var normal = Vector3.Cross(edge1, edge2);
            intersection = Vector3.Zero;
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
            if (tt > 1e-3) {
                intersection = origin + direction * tt;
                return true;
            }
            return false;
        }

        public bool RayHit(Vector3 origin, Vector3 direction, bool ignoreBack, out bool fromBack, out Vector3 intersection) {
            var edge1 = B-A;
            var edge2 = C-A;
            fromBack = false;
            intersection = Vector3.Zero;
            if (RayHitTriangle(origin, direction, A, B, C, out intersection)) {
                return true;
            }
            if (!IsTriangle && RayHitTriangle(origin, direction, A, C, D, out intersection)) {
                return true;
            }

            if (ignoreBack) return false;
            fromBack = true;
            if (RayHitTriangle(origin, direction, A, C, B, out intersection)) {
                return true;
            }
            return !IsTriangle && RayHitTriangle(origin, direction, A, D, C, out intersection);
        }

        public override string ToString()
        {
            // return "F(" + a +"," + b + "," + c + "," + d + ")";
            return IsTriangle ? $"F(a={A},b={B},c={C},normal={Normal},tags={Tags})" : $"F(a={A},b={B},c={C},d={D},normal={Normal},tags={Tags})";
        }
        public bool IsTagged(int tag) {
            return (Tags & tag) != 0;
        }
        public void Tag(int tag) {
            Tags |= tag;
        }
        public void UnTag(int tag) {
            Tags &= ~tag;
        }
        public Face MoveFaceBy(Vector3 direction) {
            A = A + direction;
            B = B + direction;
            C = C + direction;
            D = D + direction;
            return this;
        }

        public Face SetUvForSize(float uvScale) {
            var width = Vector3.Distance(A, D);
            var height = Vector3.Distance(A, B);
            return SetUvFront(width * uvScale, height * uvScale);
        }

        public Face SetUvProjected(float uvScale) {
            var dlr = Mathf.Abs(Vector3.Dot(Normal, Vector3.Left));
            var dfb = Mathf.Abs(Vector3.Dot(Normal, Vector3.Backward));
            var dud = Mathf.Abs(Vector3.Dot(Normal, Vector3.Up));
            UvA = new Vector2((dlr*A.Z + dfb*A.X + dud*A.X) * uvScale, (dlr*A.Y + dfb*A.Y + dud*A.Z) * uvScale);
            UvB = new Vector2((dlr*B.Z + dfb*B.X + dud*B.X) * uvScale, (dlr*B.Y + dfb*B.Y + dud*B.Z) * uvScale);
            UvC = new Vector2((dlr*C.Z + dfb*C.X + dud*C.X) * uvScale, (dlr*C.Y + dfb*C.Y + dud*C.Z) * uvScale);
            if (!IsTriangle) {
                UvD = new Vector2((dlr*D.Z + dfb*D.X + dud*D.X) * uvScale, (dlr*D.Y + dfb*D.Y + dud*D.Z) * uvScale);
            }
            return this;
        }

        public Face SetUvProjectedLocal(float uvScale) {
            var localRight = ((IsTriangle ? C : D) - A).Normalized;
            var localUp = Vector3.Cross(Normal, localRight);
            if (localUp == Vector3.Zero || Normal == Vector3.Zero) {
                Debug.LogWarning("invalid face: " + this);
            }
            //Face clone = DeepCopy().Rotate(Quaternion.Inverse(Quaternion.FromToRotation(Vector3.right, localRight)));
            var clone = DeepCopy().Rotate(Quaternion.LookRotation(localUp, Normal));
            clone.SetUvProjected(uvScale);
            SetUvFrom(clone);
            return this;
        }

        public Face SetUvCylinderProjection(Vector3 center, Vector3 direction, float uOffset, float uvScale) {
            UvA = UvCylinderProjection(A, center, direction, uOffset, uvScale);
            UvB = UvCylinderProjection(B, center, direction, uOffset, uvScale);
            UvC = UvCylinderProjection(C, center, direction, uOffset, uvScale);
            UvD = UvCylinderProjection(D, center, direction, uOffset, uvScale);
            return this;
        }

        private static Vector2 UvCylinderProjection(Vector3 vertex, Vector3 center, Vector3 direction, float uOffset, float uvScale) {
            var dot = Vector3.Dot(vertex - center, direction);
            var ms = center + dot*direction;
            // this should be replaced with a v scale setting
            float r = 5;
            return new Vector2((dot+uOffset) * uvScale, Vector3.Angle(vertex - ms, Vector3.Down) / 180f * r * uvScale);
        }

        public Face SetUvFrom(Face other) {
            UvA = other.UvA;
            UvB = other.UvB;
            UvC = other.UvC;
            UvD = other.UvD;
            return this;
        }

        public Face SetUvFront(float width, float height) {
            UvA = new Vector2(0, 0);
            if (IsTriangle) {
                UvB = new Vector2(width/2, height);
                UvC = new Vector2(width, 0);
            } else {
                UvB = new Vector2(0, height);
                UvC = new Vector2(width, height);
                UvD = new Vector2(width, 0);
            }
            return this;
        }

        public Face RotateUv(Quaternion rot)
        {
            UvA = Vector2.Transform(UvA, rot);
            UvB = Vector2.Transform(UvB, rot);
            UvC = Vector2.Transform(UvC, rot);
            UvD = Vector2.Transform(UvD, rot);
            return this;
        }

        public Face RotateUv() {
            var rot = Quaternion.RotationAxis(Vector3.Forward, Mathf.Pi/2);
            return RotateUv(rot);
        }
    }
}