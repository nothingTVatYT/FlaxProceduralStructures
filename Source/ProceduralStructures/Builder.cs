using System.Collections.Generic;
using System.Linq;
using FlaxEngine;
// ReSharper disable InconsistentNaming

namespace Game.ProceduralStructures {
    public static class Builder {
        public static int CUTOUT = 1;
        public enum MatchingVertex { XY, XZ, YZ, XYZ }
        public static List<Face> IndentFace(Face face, Vector3 direction, float uvScale=1f) {
            var faces = new List<Face>();
            var prev = Vector3.Zero;
            var firstVertex = true;
            var el = direction.Length;
            var vertices = new[] { face.A, face.D, face.C, face.B, face.A };
            foreach (var v in vertices) {
                if (!firstVertex) {
                    var face1 = new Face(prev, prev + direction, v + direction, v);
                    face1.SetUvFront(Vector3.Distance(prev, v) * uvScale, el * uvScale);
                    face1.Material = face.Material;
                    faces.Add(face1);
                }
                prev = v;
                firstVertex = false;
            }
            faces.Add(face.MoveFaceBy(direction));
            return faces;
        }

        public static Face[] SplitFaceABCD(Face face, float rAB, float rCD) {
            var mab = Vector3.Lerp(face.A, face.B, rAB);
            var mcd = Vector3.Lerp(face.C, face.D, rCD);
            var f1 = new Face(mab, face.B, face.C, mcd);
            var f2 = new Face(face.A, mab, mcd, face.D);
            f1.UvA = Vector2.Lerp(face.UvA, face.UvB, rAB);
            f1.UvB = face.UvB;
            f1.UvC = face.UvC;
            f1.UvD = Vector2.Lerp(face.UvC, face.UvD, rCD);
            f2.UvA = face.UvA;
            f2.UvB = f1.UvA;
            f2.UvC = f1.UvD;
            f2.UvD = face.UvD;
            f1.Material = face.Material;
            f2.Material = face.Material;
            return new Face[] { f1, f2 };
        }

        public static Face[] SplitFaceBCDA(Face face, float rBC, float rDA) {
            var mbc = Vector3.Lerp(face.B, face.C, rBC);
            var mda = Vector3.Lerp(face.D, face.A, rDA);
            var f1 = new Face(face.A, face.B, mbc, mda);
            var f2 = new Face(mda, mbc, face.C, face.D);
            f1.UvA = face.UvA;
            f1.UvB = face.UvB;
            f1.UvC = Vector2.Lerp(face.UvB, face.UvC, rBC);
            f1.UvD = Vector2.Lerp(face.UvD, face.UvA, rDA);
            f2.UvA = f1.UvD;
            f2.UvB = f1.UvC;
            f2.UvC = face.UvC;
            f2.UvD = face.UvD;
            f1.Material = face.Material;
            f2.Material = face.Material;
            return new Face[] { f1, f2 };
        }

        public static Face[] SliceFace(Face face, float dx, float dy) {
            if (dx > 0) {
                var e = new Vector3(face.B.X + dx, face.B.Y, face.B.Z);
                var f = new Vector3(face.A.X + dx, face.A.Y, face.A.Z);
                var relX = dx/(face.D.X-face.A.X);
                var left = new Face(face.A, face.B, e, f)
                {
                    UvA = face.UvA,
                    UvB = face.UvB,
                    UvC = Vector2.Lerp(face.UvB, face.UvC, relX),
                    UvD = Vector2.Lerp(face.UvA, face.UvD, relX)
                };
                var right = new Face(f, e, face.C, face.D)
                {
                    UvA = left.UvD,
                    UvB = left.UvC,
                    UvC = face.UvC,
                    UvD = face.UvD
                };
                return new[] { left, right };
            } else {
                var e = new Vector3(face.A.X, face.A.Y + dy, face.A.Z);
                var f = new Vector3(face.D.X, face.D.Y + dy, face.D.Z);
                var relY = dy/(face.B.Y-face.A.Y);
                var bottom = new Face(face.A, e, f, face.D)
                {
                    UvA = face.UvA,
                    UvB = Vector2.Lerp(face.UvA, face.UvB, relY),
                    UvC = Vector2.Lerp(face.UvD, face.UvC, relY),
                    UvD = face.UvD
                };
                var top = new Face(e, face.B, face.C, f)
                {
                    UvA = bottom.UvB,
                    UvB = face.UvB,
                    UvC = face.UvC,
                    UvD = bottom.UvC
                };
                return new[] { bottom, top };
            }
        }

        public static List<Vector3> MoveVertices(List<Vector3> list, Vector3 translation) {
            var result = new List<Vector3>(list.Count);
            foreach (var v in list) {
                result.Add(v + translation);
            }
            return result;
        }

        public static List<Vector3> RotateVertices(List<Vector3> list, Quaternion rotation) {
            var result = new List<Vector3>(list.Count);
            foreach (var v in list) {
                result.Add(v * rotation);
            }
            return result;
        }

        public static List<Vector3> ScaleVertices(List<Vector3> list, Vector3 scale) {
            var result = new List<Vector3>(list.Count);
            foreach (var v in list) {
                result.Add(Vector3.Multiply(v, scale));
            }
            return result;
        }

        public static void SetUVCylinderProjection(List<Face> faces, Vector3 center, Vector3 direction, float uOffset, float uvScale) {
            foreach (var face in faces) {
                face.SetUvCylinderProjection(center, direction, uOffset, uvScale);
            }
        }

        public static List<Face> ExtrudeEdges(List<Vector3> vertices, Vector3 direction, float uvScale=1f) {
            var faces = new List<Face>();
            var prev = Vector3.Zero;
            var firstVertex = true;
            var el = direction.Length;
            foreach (var v in vertices) {
                if (!firstVertex) {
                    var face = new Face(prev, prev + direction, v + direction, v);
                    face.SetUvFront(Vector3.Distance(prev, v) * uvScale, el * uvScale);
                    faces.Add(face);
                }
                prev = v;
                firstVertex = false;
            }
            return faces;
        }

        public static List<Face> BridgeEdges(List<Edge> edgeList1, List<Edge> edgeList2, bool flipNormals, float uvScale) {
            if (edgeList1.Count == 0 || edgeList1.Count != edgeList2.Count) {
                Debug.LogWarning("Cannot bridge edges");
            }
            var result = new List<Face>();
            for (var i = 0; i < edgeList1.Count; i++) {
                var face = new Face(edgeList1[i], edgeList2[i]);
                if (flipNormals) face.InvertNormals();
                face.SetUvProjectedLocal(uvScale);
                result.Add(face);
            }
            return result;
        }

        public static List<Face> BridgeEdgeLoopsPrepared(List<Vector3> fromVertices, List<Vector3> toVertices, float uvScale = 1f) {
            var fromRing = new CircularReadonlyList<Vector3>(fromVertices);
            var toRing = new CircularReadonlyList<Vector3>(toVertices);
            var faces = new List<Face>();
            for (var i = 0; i < fromRing.Count; i++) {
                var face = new Face(fromRing[i], fromRing[i+1], toRing[i+1], toRing[i]);
                if (face.IsValid()) {
                    face.SetUvProjected(uvScale);
                    faces.Add(face);
                }
            }
            return faces;
        }

        public static List<Face> BridgeEdgeLoops(List<Vector3> fromVertices, List<Vector3> toVertices, float uvScale=1f) {
            var faces = new List<Face>();
            //TemporaryTesting();
            var numberOfVertices = fromVertices.Count;
            if (numberOfVertices < 3) {
                Debug.LogWarning("There are not enough vertices to bridge: " + numberOfVertices);
                return faces;
            }
            if (numberOfVertices != toVertices.Count) {
                Debug.LogWarning("The vertices counts do not match: from=" + numberOfVertices + ", to=" + toVertices.Count);
                return faces;
            }
            var fromRing = new CircularReadonlyList<Vector3>(fromVertices);
            var toRing = new CircularReadonlyList<Vector3>(toVertices);
            var fromNormal = Vector3.Cross(fromRing[1]-fromRing[0], fromRing[-1]-fromRing[0]).Normalized;
            var toNormal = Vector3.Cross(toRing[1]-toRing[0], toRing[-1]-toRing[0]).Normalized;
            // the normals should point to opposite directions
            var dot = Vector3.Dot(fromNormal, toNormal);
            if (dot > 0) {
                toRing.Reverse();
            }
            toRing.Reverse();
            // now check which vertices we should use for bridging
            var minDeviation = float.MaxValue;
            var bestPairing = 0;
            for (var o = 0; o < fromRing.Count; o++) {
                toRing.IndexOffset = o;
                var directions = new List<Vector3>();
                for (var i = 0; i < fromRing.Count; i++) {
                    directions.Add((toRing[i]-fromRing[i]).Normalized);
                }
                // sum the deviations from the expected value of one to each other
                float sumDeviations = 0;
                for (var i = 1; i < directions.Count; i++) {
                    var dotDir = Vector3.Dot(directions[i-1], directions[i]);
                    sumDeviations += 1f - dotDir;
                }
                if (sumDeviations < minDeviation) {
                    minDeviation = sumDeviations;
                    bestPairing = toRing.IndexOffset;
                }
            }
            toRing.IndexOffset = bestPairing;

            for (var i = 0; i < fromRing.Count; i++) {
                var face = new Face(fromRing[i], fromRing[i+1], toRing[i+1], toRing[i]);
                face.SetUvForSize(uvScale);
                faces.Add(face);
            }
            return faces;
        }

        public static List<Face> ExtrudeEdges(Face face, Vector3 direction, float uvScale = 1f) {
            return ExtrudeEdges(new List<Vector3> {face.A, face.B, face.C, face.D, face.A}, direction, uvScale);
        }

        public static void ClampToPlane(List<Vector3> front, List<Vector3> back, Vector3 plane, Vector3 normal) {
            for (var i = 0; i < front.Count; i++) {
                // if vertex is behind the plane (not on normal side) project it on the plane
                var dot = Vector3.Dot(front[i] - plane, normal);
                if (dot < 0) {
                    var v = front[i] - plane;
                    var dist = v.X*normal.X + v.Y*normal.Y + v.Z*normal.Z;
                    var projected = front[i] - dist*normal;
                    // collapse front and back vertices
                    Debug.LogFormat("move front {0} to {1}, dist={2}", front[i], projected, dist);
                    front[i] = projected;
                    back[i] = projected;
                } else {
                    dot = Vector3.Dot(back[i] - plane, normal);
                    if (dot > 0) {
                        var v = back[i] - plane;
                        var dist = v.X*normal.X + v.Y*normal.Y + v.Z*normal.Z;
                        var projected = back[i] - dist*normal;
                        Debug.LogFormat("move back {0} to {1}, dist={2]", front[i], projected, dist);
                        front[i] = projected;
                        back[i] = projected;
                    }
                }
            }
        }

        public static List<Face> CloneAndMoveFacesOnNormal(List<Face> faces, float thickness, float uvScale) {
            var result = new List<Face>();
            var closedLoop = faces[0].SharesEdgeWith(faces[^1]);
            //Debug.Log("is closed loop: " + closedLoop);
            foreach (var face in faces) {
                var secondFace = face.DeepCopy().MoveFaceBy(-face.Normal * thickness).InvertNormals();
                // relations: a <-> d, b <-> c
                result.Add(secondFace);
            }
            // check on overlaps and gaps
            var startIndex = closedLoop ? 0 : 1;
            var previousFace = closedLoop ? result[^1] : result[0];
            for (var i = startIndex; i < result.Count; i++) {
                var face = result[i];
                // assume edges AB and DC should be the same
                // calculate the line intersection of both AD
                float m;
                if (LineLineIntersect(previousFace.A, previousFace.D, face.A, face.D, out m)) {
                    previousFace.A += (previousFace.D - previousFace.A) * m;
                    face.D = previousFace.A;
                    // calculate the line intersection of both bc
                    if (LineLineIntersect(previousFace.B, previousFace.C, face.B, face.C, out m)) {
                        previousFace.B += (previousFace.C - previousFace.B) * m;
                        face.C = previousFace.B;
                    } else {
                        Debug.Log("no intersection of BC: " + previousFace + " " + face);
                    }
                } else {
                    Debug.Log("no intersection of AD: " + previousFace + " " + face);
                }
                previousFace = face;
            }
            //result.AddRange(faces);
            return result;
        }

        public static void MoveVertices(List<Face> faces, Vector3 from, MatchingVertex matching, Vector3 to) {
            var delta = to - from;
            foreach (var face in faces) {
                if (Matches(face.A, from, matching)) {
                    face.A += delta;
                }
                if (Matches(face.B, from, matching)) {
                    face.B += delta;
                }
                if (Matches(face.C, from, matching)) {
                    face.C += delta;
                }
                if (!face.IsTriangle && Matches(face.D, from, matching)) {
                    face.D += delta;
                }
            }
        }

        public static bool Matches(Vector3 v, Vector3 pattern, MatchingVertex matching)
        {
            var result = matching switch
            {
                MatchingVertex.XZ => Mathf.Approximately(v.X, pattern.X) && Mathf.Approximately(v.Z, pattern.Z),
                MatchingVertex.XY => Mathf.Approximately(v.X, pattern.X) && Mathf.Approximately(v.Y, pattern.Y),
                MatchingVertex.YZ => Mathf.Approximately(v.Y, pattern.Y) && Mathf.Approximately(v.Z, pattern.Z),
                MatchingVertex.XYZ => Mathf.Approximately(v.X, pattern.X) && Mathf.Approximately(v.Y, pattern.Y) &&
                                      Mathf.Approximately(v.Z, pattern.Z),
                _ => false
            };
            return result;
        }

        /* Other than in Unity 2D this rect defines positive y up */
        public static Face ProjectRectOnFrontFace(Rectangle rect, float z) {
            var a = new Vector3(rect.X, rect.Y, z);
            var b = new Vector3(rect.X, rect.Y+rect.Height, z);
            var c = new Vector3(rect.X+rect.Width, rect.Y+rect.Height, z);
            var d = new Vector3(rect.X+rect.Width, rect.Y, z);
            return new Face(a, b, c, d);
        }

        public static Face FindFirstFaceByTag(List<Face> faces, int tag)
        {
            return faces.FirstOrDefault(f => f.IsTagged(tag));
        }

        public static float DistanceXZ(Vector3 a, Vector3 b) {
            var dx = a.X - b.X;
            var dz = a.Z - b.Z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static Vector3 FindCentroid(List<Vector3> list) {
            float x = 0;
            float y = 0;
            float z = 0;
            foreach (var t in list) {
                x += t.X;
                y += t.Y;
                z += t.Z;
            }
            x /= list.Count;
            y /= list.Count;
            z /= list.Count;
            return new Vector3(x, y, z);
        }

        public static bool LineLineIntersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float m) {
            m = 0;
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
            if (divisorXY == 0 && divisorXZ == 0) {
                // cannot devide by 0 => there is no solution
                Debug.Log("divisors are 0 for " + u + " and " + v);
                return false;
            }
            float n;
            if (u.Y != 0) {
                n = (c.Y*u.X-a.Y*u.X-u.Y*c.X+a.X*u.Y) / divisorXY;
                m = (c.Y+n*v.Y-a.Y) / u.Y;
            } else if (u.X != 0) {
                n = (c.Z*u.X-a.Z*u.X-u.Z*c.X+a.X*u.Z) / divisorXZ;
                m = (c.X+n*v.X-a.X) / u.X;
            } else {
                n = (c.Z*u.X-a.Z*u.X-u.Z*c.X+a.X*u.Z) / divisorXZ;
                m = (c.Z+n*v.Z-a.Z) / u.Z;
            }
            // intersection point according to first line equation
            var h = a + m * u;
            // intersection point according to second line equation
            var i = c + n * v;
            if ((h-i).LengthSquared < 1e-3f) {
                // good enough. we have a valid solution in 3d
                return true;
            }
            Debug.Log("don't match: " + h + " and " + i);
            return false;
        }
    }
}