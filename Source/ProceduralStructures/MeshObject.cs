using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using FlaxEngine;
using FlaxEngine.Utilities;

namespace Game.ProceduralStructures {
    public class MeshObject {

        protected class DeferredAction {
            public enum CommandType { SplitTriangleByLine }
            public CommandType Command;
            public Triangle Triangle;
            public Vector3 A;
            public Vector3 B;
            public static DeferredAction SplitTriangleAction(Triangle triangle, Vector3 a, Vector3 b) {
                var deferredAction = new DeferredAction
                {
                    Command = CommandType.SplitTriangleByLine,
                    Triangle = triangle,
                    A = a,
                    B = b
                };
                return deferredAction;
            }
        }

        public enum ShadingType { Flat, Smooth, Auto }

        private const float Epsilon = 1e-3f;
        private const float EpsilonSquared = Epsilon * Epsilon;
        protected List<Vertex> Vertices = new();
        protected List<Triangle> Triangles = new();
        public float UvScale = 1;
        public ShadingType Shading = ShadingType.Flat;
        public Transform Transform;
        public Actor TargetActor;
        public Material Material;
        private Float3[] _cachedVertices;
        private int[] _cachedTriangles;
        protected Dictionary<string, List<Vertex>> VertexLists = new();

        public float Area => Triangles.Sum(triangle => triangle.Area);

        public int VerticesCount => Vertices.Count;

        public int TrianglesCount => Triangles.Count;

        protected virtual int Add(Vector3 pos) {
            return AddUnique(pos);
        }

        protected int AddUnique(Vector3 pos) {
            var nv = new Vertex(pos);
            var idx = Vertices.FindIndex((v) => (nv.Equals(v)));
            if (idx >= 0) return idx;
            nv.Id = Vertices.Count;
            Vertices.Add(nv);
            return Vertices.Count - 1;
        }

        protected int AddUnchecked(Vector3 pos) {
            var v = new Vertex(pos)
            {
                Id = Vertices.Count
            };
            Vertices.Add(v);
            return Vertices.Count - 1;
        }

        public virtual List<Vertex> AddRange(IEnumerable<Vector3> points) {
            var result = new List<int>();
            foreach (var v in points) {
                var i = Add(v);
                if (!result.Contains(i)) {
                    result.Add(i);
                }
            }
            return VertexList(result);
        }

        public List<Vertex> VertexList(List<int> l) {
            var result = new List<Vertex>(l.Count);
            result.AddRange(l.Select(i => Vertices[i]));
            return result;
        }

        public Vertex GetVertex(int idx) {
            if (idx >= 0 && idx < Vertices.Count) return Vertices[idx];
            return null;
        }

        public void SetVertexList(string name, List<Vertex> list) => VertexLists[name] = list;

        public List<Vertex> GetVertexList(string name) => VertexLists[name];

        public List<Vector3> PointList() {
            var result = new List<Vector3>(Vertices.Count);
            result.AddRange(Vertices.Select(v => v.Pos));
            return result;
        }

        public List<Triangle> TriangleList(IEnumerable<int> l)
        {
            return l.Select(i => Triangles[i]).ToList();
        }

        public int AddTriangle(Vertex v0, Vertex v1, Vertex v2) {
            var triangle = new Triangle(v0, v1, v2);
            // checking for duplicates takes way too long
            // if this is necessary, triangles has to become a HashSet
            /*
            Triangle duplicate = triangles.Find(t => t.Equals(triangle));
            if (duplicate != null) {
                Debug.LogWarning("Refuse to add a duplicated triangle " + triangle);
                triangle.RemoveTriangleLinks();
                return triangles.IndexOf(duplicate);
            } else {
                triangles.Add(triangle);
                // for visualizing only
                triangle.SetUVProjected(uvScale);
            }
            */
            Triangles.Add(triangle);
            return Triangles.Count-1;
        }

        public int AddTriangle(Triangle triangle) {
            var duplicate = Triangles.Find(t => t.Equals(triangle));
            if (duplicate != null) {
                Debug.LogWarning("Refuse to add a duplicated triangle " + triangle);
                triangle.RemoveTriangleLinks();
                return Triangles.IndexOf(duplicate);
            }
            Triangles.Add(triangle);
            return Triangles.Count-1;
        }

        public void Remove(Triangle triangle) {
            triangle.RemoveTriangleLinks();
            Triangles.Remove(triangle);
        }

        public void RemoveTriangles(IEnumerable<Triangle> triangleList) {
            foreach (var t in triangleList) {
                //t.RemoveTriangleLinks();
                Triangles.Remove(t);
            }
        }
        
        public void Remove(Vertex v) {
            RemoveTriangles(v.Triangles);
            Vertices.Remove(v);
        }

        public void AddObject(MeshObject other) {
            foreach (var t in other.Triangles) {
                var v0 = Vertices[AddUnique(other.WorldPosition(t.V0.Pos))];
                var v1 = Vertices[AddUnique(other.WorldPosition(t.V1.Pos))];
                var v2 = Vertices[AddUnique(other.WorldPosition(t.V2.Pos))];
                var nt = Triangles[AddTriangle(v0, v1, v2)];
                nt.Uv0 = t.Uv0;
                nt.Uv1 = t.Uv1;
                nt.Uv2 = t.Uv2;
            }
        }

        public List<int> AddCube(Vector3 center, Vector3 extends, float uvScale = 1f) {
            var v0 = Vertices[Add(center + new Vector3(-extends.X, -extends.Y, -extends.Z))];
            var v1 = Vertices[Add(center + new Vector3(-extends.X, extends.Y, -extends.Z))];
            var v2 = Vertices[Add(center + new Vector3(extends.X, extends.Y, -extends.Z))];
            var v3 = Vertices[Add(center + new Vector3(extends.X, -extends.Y, -extends.Z))];
            var v4 = Vertices[Add(center + new Vector3(-extends.X, -extends.Y, extends.Z))];
            var v5 = Vertices[Add(center + new Vector3(-extends.X, extends.Y, extends.Z))];
            var v6 = Vertices[Add(center + new Vector3(extends.X, extends.Y, extends.Z))];
            var v7 = Vertices[Add(center + new Vector3(extends.X, -extends.Y, extends.Z))];
            var createdTriangles = new List<int>(12)
            {
                // front
                AddTriangle(v0, v1, v2),
                AddTriangle(v0, v2, v3),
                // left
                AddTriangle(v4, v5, v1),
                AddTriangle(v4, v1, v0),
                // right
                AddTriangle(v3, v2, v6),
                AddTriangle(v3, v6, v7),
                // back
                AddTriangle(v7, v6, v5),
                AddTriangle(v7, v5, v4),
                // top
                AddTriangle(v1, v5, v6),
                AddTriangle(v1, v6, v2),
                // bottom
                AddTriangle(v4, v0, v3),
                AddTriangle(v4, v3, v7)
            };
            SetUVBoxProjection(createdTriangles, uvScale);
            return createdTriangles;
        }

        public void CleanupMesh() {
            var toBeRemoved = new HashSet<Triangle>();
            foreach (var triangle in Triangles)
            {
                if (toBeRemoved.Contains(triangle)) continue;
                foreach (var adjacent in triangle.GetAdjacentTriangles())
                {
                    if (!(Mathf.Abs(Vector3.Dot(triangle.Normal, adjacent.Normal) + 1f) < 1e-3f)) continue;
                    if (triangle.SharesTurningEdge(adjacent)) continue;
                    toBeRemoved.Add(triangle);
                    toBeRemoved.Add(adjacent);
                }
            }
            foreach (var t in toBeRemoved) Remove(t);
        }

        public static void LinkEdges(List<TEdge> edges) {
            foreach (var edge in edges) {
                edge.A.SetConnected(edge.B);
            }
        }

        private static bool IsInEdgeSet(Vertex a, Vertex b, IReadOnlySet<TEdge> set) {
            if (set.Count == 0) return false;
            var e = new TEdge(a, b);
            return set.Contains(e);
        }

        private static bool IsPlanar(IEnumerable<Vertex> l) {
            var list = new List<Vertex>(l);
            if (list.Count <= 3) {
                return true;
            }
            var normal = Vector3.Cross(list[1].Pos-list[2].Pos, list[1].Pos-list[0].Pos).Normalized;
            for (var i = 2; i < list.Count-1; i++) {
                var nextNormal = Vector3.Cross(list[i].Pos-list[i+1].Pos, list[i].Pos-list[i-1].Pos).Normalized;
                if (Mathf.Abs(Vector3.Dot(normal, nextNormal)) > 0.1f) {
                    return false;
                }
            }
            return true;
        }

        private static bool EnclosesAnyEdge(IEnumerable<Vertex> l, HashSet<TEdge> edges) {
            if (edges.Count == 0) return false;
            // only need to check if there are at least four vertices
            var cl = new CircularList<Vertex>(l);
            if (cl.Count < 4) return false;
            cl.Reverse();
            foreach (var edge in edges) {
                var idxA = cl.IndexOf(edge.A);
                if (idxA == cl.NotFound) continue;
                var idxB = cl.IndexOf(edge.B);
                if (idxB == cl.NotFound) continue;
                // the only case allowed is consecutive
                if (cl.IsConsecutiveIndex(idxB, idxA) || cl.IsConsecutiveIndex(idxA, idxB)) continue;
                return true;
            }
            return false;
        }

        private static IEnumerable<List<Vertex>> FollowLoop(Stack<Vertex> l, Vertex head, HashSet<Vertex> set, HashSet<TEdge> visitedEdges, int maxLength, IReadOnlySet<EdgeLoop> foundEdgeLoops) {
            if (l == null) {
                l = new Stack<Vertex>();
                foreach (var v in set) {
                    l.Push(v);
                    foreach (var nl in FollowLoop(l, v, set, visitedEdges, maxLength, foundEdgeLoops)) {
                        maxLength = Mathf.Min(maxLength, nl.Count);
                        yield return nl;
                        if (nl.Count == 3) {
                            yield break;
                        }
                    }
                    l.Pop();
                }
                yield break;
            }
            if (l.Count > 2 && l.Peek().Connected.Contains(head)) {
                // C# converts a stack into a list as if you would pop each element top to bottom, not bottom to top
                var list = new List<Vertex>(l);
                list.Reverse();
                // only report edge loops not found previously
                if (foundEdgeLoops.Contains(new EdgeLoop(list))) yield break;
                //maxLength = Mathf.Min(maxLength, list.Count);
                yield return list;
                yield break;
            }
            if (l.Count >= maxLength) {
                //Debug.Log("break because loop >= " + maxLength);
                yield break;
            }
            var tail = l.Count == 0 ? head : l.Peek();
            foreach (var v in tail.Connected)
            {
                if (!set.Contains(v) || l.Contains(v) || IsInEdgeSet(tail, v, visitedEdges) || !IsPlanar(l)) continue;
                l.Push(v);
                if (!EnclosesAnyEdge(l, visitedEdges)) {
                    foreach (var nl in FollowLoop(l, head, set, visitedEdges, maxLength, foundEdgeLoops)) {
                        maxLength = Mathf.Min(maxLength, nl.Count);
                        yield return nl;
                        if (nl.Count == 3) {
                            yield break;
                        }
                    }
                }
                l.Pop();
            }
        }

        public static HashSet<EdgeLoop> FindSmallEdgeLoops(HashSet<Vertex> set, int maxLength) {
            // find a vertex with only two connections
            var start = set.FirstOrDefault(v => v.Connected.Count == 2);
            // if there is none we may have a closed object and we can pick any one with connected >=2
            if (start == null)
            {
                foreach (var v in set.Where(v => v.Connected.Count > 2))
                {
                    start = v;
                    break;
                }
            }
            if (start == null) {
                Debug.LogWarning("There is no closed polygon in this edge loop.");
                return null;
            }
            var visitedEdges = new HashSet<TEdge>();
            List<List<Vertex>> allLoops;
            maxLength = Mathf.Min(set.Count, maxLength);

            var foundEdgeLoops = new HashSet<EdgeLoop>();
            // this would be an endless loop but the break seems to work now
            while (set.Count > 0) {
                allLoops = new List<List<Vertex>>(FollowLoop(null, null, set, visitedEdges, maxLength, foundEdgeLoops));
                if (allLoops.Count == 0) {
                    break;
                }
                allLoops.Sort((x,y) => x.Count.CompareTo(y.Count));
                var loop = allLoops[0];
                foundEdgeLoops.Add(new EdgeLoop(loop));

                for (var i = 0; i < loop.Count; i++) {
                    var j = i < loop.Count-1 ? i+1 : 0;
                    // is this is a border edge exclude its reverse as well
                    if (loop[i].Connected.Count == 2 || loop[j].Connected.Count == 2) {
                        visitedEdges.Add(new TEdge(loop[j], loop[i]));
                    }
                    visitedEdges.Add(new TEdge(loop[i], loop[j]));
                }
            }
            Debug.Log("found " + foundEdgeLoops.Count + " unique small edge loops.");
            return foundEdgeLoops;
        }

        private void CheckOnOverlaps(IList<TEdge> edges) {
            bool restartSearch;
            do {
                restartSearch = false;
                for (var i = 0; i < edges.Count-1; i++) {
                    var edge1 = edges[i];
                    for (var j = i+1; j < edges.Count; j++) {
                        var edge2 = edges[j];
                        if (GeometryTools.EdgeEdgeIntersectIgnoreEnds(edge1.A.Pos, edge1.B.Pos, edge2.A.Pos, edge2.B.Pos, out var intersection)) {
                            Debug.Log("edges intersect: " + edge1 + " and " + edge2 + " at " + intersection);
                            // check which ones we have to split
                            if (!SameInTolerance(edge1.A.Pos, intersection) && !SameInTolerance(edge1.B.Pos, intersection)) {
                                // split edge1 because the intersection is not near any end
                                var m = Vertices[Add(intersection)];
                                var newEdge1 = new TEdge(edge1.A, m);
                                var newEdge2 = new TEdge(m, edge1.B);
                                edge1.RemoveEdgeLinks();
                                newEdge1.ResetEdgeLinks();
                                newEdge2.ResetEdgeLinks();
                                edges.Remove(edge1);
                                edges.Add(newEdge1);
                                edges.Add(newEdge2);
                                Debug.Log("Replaced " + edge1 + " with " + newEdge1 + " and " + newEdge2);
                                restartSearch = true;
                            }
                            if (!SameInTolerance(edge2.A.Pos, intersection) && !SameInTolerance(edge2.B.Pos, intersection)) {
                                // split edge2 because the intersection is not near any end
                                var m = Vertices[Add(intersection)];
                                var newEdge1 = new TEdge(edge2.A, m);
                                var newEdge2 = new TEdge(m, edge2.B);
                                edge2.RemoveEdgeLinks();
                                newEdge1.ResetEdgeLinks();
                                newEdge2.ResetEdgeLinks();
                                edges.Remove(edge2);
                                edges.Add(newEdge1);
                                edges.Add(newEdge2);
                                Debug.Log("Replaced " + edge2 + " with " + newEdge1 + " and " + newEdge2);
                                restartSearch = true;
                            }
                        }
                        if (restartSearch) break;
                    }
                    if (restartSearch) break;
                }
            } while (restartSearch);
        }

        public List<Triangle> CloseUnorderedEdgeLoops(List<TEdge> edges, float uvScale) {
            var sw = new DebugStopwatch().Start("close unordered edge loop");
            LinkEdges(edges);
            var countBefore = edges.Count;
            CheckOnOverlaps(edges);
            Debug.Log("check on overlaps - before=" + countBefore + ", after=" + edges.Count);
            // get all vertices in edges, edges are encoded in Vertex.connected
            var allVertices = new HashSet<Vertex>();
            var borderEdges = new List<TEdge>();
            foreach (var edge in edges) {
                allVertices.Add(edge.A);
                allVertices.Add(edge.B);
                if (edge.A.Connected.Count <=2 || edge.B.Connected.Count <= 2) {
                    borderEdges.Add(edge);
                }
            }
            var center = GetCenter(allVertices);
            // find edge loops
            var loops = FindSmallEdgeLoops(allVertices, edges.Count);
            Debug.Log(sw.Stop());
            var generated = new List<Triangle>();
            if (loops != null) {
                foreach (var loop in loops) {
                    generated.AddRange(CloseEdgeLoop(loop.Vertices));
                }
            }
            SetNormals(generated);
            // we have no idea what should be the normals but just assume that the center is inside
            // and the majority of the faces should have their normals point outwards
            var dotSum = generated.Sum(t => Vector3.Dot(t.Normal, (center - t.Center).Normalized));
            if (dotSum > 0) {
                FlipNormals(generated);
            }
            return generated;
        }

        private IEnumerable<Triangle> CloseEdgeLoop(IEnumerable<Vertex> loop) {
            var generated = new List<Triangle>();
            // we assume this is a small edge loop that is at least more or less planar
            //var loopCenter = GetCenter(loop);
            var verts = new CircularList<Vertex>(loop);
            // search ear
            var index = 0;
            var noAction = 0;
            while (verts.Count > 3) {
                var dirFw = (verts[index+1].Pos - verts[index].Pos).Normalized;
                var dirBk = (verts[index-1].Pos - verts[index].Pos).Normalized;
                var normal = Vector3.Cross(dirFw, dirBk);
                var normFw = Vector3.Cross(dirFw, normal);
                if (Vector3.Dot(dirBk, normFw) < -0.1f) {
                    // this should be an ear
                    generated.Add(Triangles[AddTriangle(verts[index-1], verts[index], verts[index+1])]);
                    verts.RemoveAt(index);
                    noAction = 0;
                } else {
                    index++;
                    noAction++;
                    if (noAction < verts.Count) continue;
                    Debug.LogWarning("There is a bug in CloseEdgeLoop: we haven't found an ear all the way around.");
                    break;
                }
            }
            // just close the last three vertices
            generated.Add(Triangles[AddTriangle(verts[0], verts[1], verts[2])]);
            return generated;
        }

        private Vertex GetVertexWithTwoConnections(IEnumerable<Vertex> l, HashSet<Vertex> exclusion) {
            foreach (var v in l) {
                if (!exclusion.Contains(v) && v.Connected.Count >= 2) {
                    return v;
                }
            }
            return null;
        }

        public void SetNormals(List<Triangle> triangles) {
            var all = new List<Triangle>(triangles);
            var connected = new List<Triangle>();
            var first = all[0];
            do {
                connected.Add(first);
                while (connected.Count < triangles.Count) {
                    var foundNext = false;
                    for (var i = 0; i < connected.Count; i++) {
                        var next = connected[i];
                        foreach (var t in next.GetAdjacentTriangles()) {
                            if (all.Contains(t) && !connected.Contains(t)) {
                                if (next.SharesTurningEdge(t)) {
                                    t.FlipNormal();
                                }
                                connected.Add(t);
                                foundNext = true;
                            }
                        }
                    }
                    if (!foundNext) break;
                }
                if (connected.Count < all.Count) {
                    all.RemoveAll(t => connected.Contains(t));
                    if (all.Count == 0) break;
                    first = all[0];
                }
            } while (connected.Count < triangles.Count);
        }

        public bool RayHitTriangle(Vector3 origin, Vector3 direction, bool ignoreFromBack, out Triangle triangleHit, out bool fromBack, out Vector3 intersection) {
            fromBack = false;
            var minDistance = float.MaxValue;
            triangleHit = null;
            var triangleHitFromback = false;
            intersection = Vector3.Zero;
            foreach (var triangle in Triangles) {
                if (triangle.RayHit(origin, direction, ignoreFromBack, out fromBack, out intersection)) {
                    var distance = (intersection-origin).Length;
                    if (distance < minDistance) {
                        minDistance = distance;
                        triangleHit = triangle;
                        triangleHitFromback = fromBack;
                    }
                }
            }
            fromBack = triangleHitFromback;
            return triangleHit != null;
        }

        public bool SwapEdges(Triangle t1, Triangle t2) {
            var success = false;
            var t1v = t1.GetVertices();
            var t2v = t2.GetVertices();
            for (var i = 0; i < t1v.Length; i++) {
                var j = System.Array.IndexOf(t2v, t1v[i]);
                if (j < 0) {
                    // t1v[i] is a unique vertex
                    for (var k = 0; k < t2v.Length; k++) {
                        var l = System.Array.IndexOf(t1v, t2v[k]);
                        if (l < 0) {
                            // t2v[l] is the second unique vertex
                            // swapping the edge means we replace t1v[i+2] with t2v[l] and t2v[l+2] with t1v[i]
                            var i2 = (i+2) % 3;
                            var l2 = (l+2) % 3;
                            t1.RemoveTriangleLinks();
                            t2.RemoveTriangleLinks();
                            var new1 = new Triangle(t1.V0, t1.V1, t1.V2);
                            var new2 = new Triangle(t2.V0, t2.V1, t2.V2);
                            new1.SetVertex(i2, t2v[k]);
                            new2.SetVertex(l2, t1v[i]);
                            Remove(t1);
                            Remove(t2);
                            AddTriangle(new1);
                            AddTriangle(new2);
                            success = true;
                        }
                    }
                }
            }
            return success;
        }

        public int GetTriangleHit(Vector3 origin, Vector3 direction) {
            foreach (var triangle in Triangles) {
                Vector3 intersection;
                if (Face.RayHitTriangle(origin, direction, triangle.V0.Pos, triangle.V1.Pos, triangle.V2.Pos, out intersection)) {
                    return Triangles.IndexOf(triangle);
                }
            }
            return -1;
        }

        public void ScaleVertices(List<Vertex> list, Vector3 scale) {
            foreach (var v in list) {
                v.Pos = Vector3.Multiply(v.Pos, scale);
            }
        }

        public void RandomizeVertices(Vector3 displacement) {
            foreach (var vertex in Vertices) {
                vertex.Pos = vertex.Pos + displacement * (Random.Shared.NextFloat(1) - 0.5f);
            }
        }

        public void SetUVCylinderProjection(IEnumerable<int> triangleIndices, Vector3 center, Vector3 direction, float uOffset, float uScale, float vScale) {
            foreach (var ti in triangleIndices) {
                var triangle = Triangles[ti];
                triangle.SetUVCylinderProjection(center, direction, uOffset, uScale, vScale);
            }
        }

        public void SetUVCylinderProjection(Vector3 center, Vector3 direction, float uOffset, float uScale,
            float vScale)
        {
            foreach (var triangle in Triangles)
            {
                triangle.SetUVCylinderProjection(center, direction, uOffset, uScale, vScale);
            }
        }
        
        public void SetUVTunnelProjection(IEnumerable<int> triangleIndices, Vector3 center, Vector3 direction, float uOffset, float uScale, float vScale) {
            foreach (var ti in triangleIndices) {
                var triangle = Triangles[ti];
                triangle.SetUVTunnelProjection(center, direction, uOffset, uScale, vScale);
            }
        }

        public void SetUVTunnelProjection(Vector3 center, Vector3 direction, float uOffset, float uScale,
            float vScale)
        {
            foreach (var triangle in Triangles)
            {
                triangle.SetUVTunnelProjection(center, direction, uOffset, uScale, vScale);
            }
        }
        
        public void ClampToPlane(List<Vertex> front, List<Vertex> back, Vector3 plane, Vector3 normal) {
            for (var i = 0; i < front.Count; i++) {
                // if vertex is behind the plane (not on normal side) project it on the plane
                var dot = Vector3.Dot(front[i].Pos - plane, normal);
                if (dot < 0) {
                    var v = front[i].Pos - plane;
                    var dist = v.X*normal.X + v.Y*normal.Y + v.Z*normal.Z;
                    var projected = front[i].Pos - dist*normal;
                    // collapse front and back vertices
                    front[i].Pos = projected;
                    back[i].Pos = projected;
                }
            }
        }

        public int[] CreateTriangleFan(List<Vertex> l) {
            var centroid = GetCenter(l);
            var fanCenter = Vertices[AddUnique(centroid)];
            var result = new List<int>();
            if (l.Count >= 3) {
                for (var i = 0; i < l.Count; i++) {
                    var j = i < l.Count-1 ? i+1 : 0;
                    result.Add(AddTriangle(fanCenter, l[i], l[j]));
                }
            }
            return result.ToArray();
        }

        public bool IsBehind(Triangle triangle, Vector3 point) {
            var normal = triangle.Normal;
            return Vector3.Dot(normal, point-triangle.V0.Pos) < 0;
        }

        public bool IsInFront(Triangle triangle, Vector3 point) {
            var normal = triangle.Normal;
            return Vector3.Dot(normal, point-triangle.V0.Pos) > 0;
        }

        public int[] MakePyramid(Triangle triangle, Vertex newVertex) {
            var indices = new int[3];
            indices[0] = AddTriangle(triangle.V0, newVertex, triangle.V1);
            indices[1] = AddTriangle(triangle.V1, newVertex, triangle.V2);
            indices[2] = AddTriangle(triangle.V2, newVertex, triangle.V0);
            return indices;
        }

        public int[] SplitTriangle(Triangle triangle, Vertex v3) {
            if (!Triangles.Remove(triangle)) {
                Debug.LogWarning("Could not remove " + triangle + ".");
            }
            triangle.RemoveTriangleLinks();
            var indices = new int[3];
            indices[0] = AddTriangle(triangle.V0, triangle.V1, v3);
            indices[1] = AddTriangle(triangle.V1, triangle.V2, v3);
            indices[2] = AddTriangle(triangle.V2, triangle.V0, v3);
            //Debug.Log("Created " + triangles[indices[0]] + " and " + triangles[indices[1]] + " and " + triangles[indices[2]] + " from " + triangle);
            return indices;
        }

        public List<Triangle> SplitTriangleByLine(Triangle triangle, Vector3 a, Vector3 b) {
            var result = new List<Triangle>();
            // calculate the two new vertices, i.e. the intersections of a line through ab and two edges
            Vector3 iv;
            Vertex v3, v4;
            DebugLocalLine(a, b, Color.Yellow);
            DebugLocalLine(triangle.V0.Pos, triangle.V1.Pos, Color.Red);
            if (GeometryTools.EdgeLineIntersect(triangle.V0.Pos, triangle.V1.Pos, a, b, out iv)) {
                v3 = Vertices[Add(iv)];
                if (GeometryTools.EdgeLineIntersect(triangle.V1.Pos, triangle.V2.Pos, a, b, out iv)) {
                    // intersect v0v1 and v1v2
                    triangle.RemoveTriangleLinks();
                    result.Add(triangle);
                    v4 = Vertices[Add(iv)];
                    result.Add(Triangles[AddTriangle(v3, triangle.V1, v4)]);
                    result.Add(Triangles[AddTriangle(triangle.V0, v3, v4)]);
                    triangle.V1 = v4;
                    triangle.ResetTriangleLinks();
                } else if (GeometryTools.EdgeLineIntersect(triangle.V2.Pos, triangle.V0.Pos, a, b, out iv)) {
                    // intersect v0v1 and v2v0
                    triangle.RemoveTriangleLinks();
                    result.Add(triangle);
                    v4 = Vertices[Add(iv)];
                    result.Add(Triangles[AddTriangle(v3, triangle.V1, triangle.V2)]);
                    result.Add(Triangles[AddTriangle(v3, triangle.V2, v4)]);
                    triangle.V1 = v3;
                    triangle.V2 = v4;
                    triangle.ResetTriangleLinks();
                }
            } else if (GeometryTools.EdgeLineIntersect(triangle.V1.Pos, triangle.V2.Pos, a, b, out iv)) {
                v3 = Vertices[Add(iv)];
                if (GeometryTools.EdgeLineIntersect(triangle.V2.Pos, triangle.V0.Pos, a, b, out iv)) {
                    // intersect v1v2 and v2v0
                    triangle.RemoveTriangleLinks();
                    result.Add(triangle);
                    v4 = Vertices[Add(iv)];
                    result.Add(Triangles[AddTriangle(triangle.V0, v3, v4)]);
                    result.Add(Triangles[AddTriangle(v4, v3, triangle.V2)]);
                    triangle.V2 = v3;
                    triangle.ResetTriangleLinks();
                }
            }
            return result;
        }

        public void SplitBigTriangles(float maxRelativeSize, float offset) {
            var totalArea = Area;
            var center = GetCenter();
            var trianglesToSplit = new List<Triangle>();
            do {
                foreach (var triangle in Triangles) {
                    if (triangle.Area / totalArea > maxRelativeSize) {
                        trianglesToSplit.Add(triangle);
                    }
                }
                if (trianglesToSplit.Count > 0) {
                    foreach (var triangle in trianglesToSplit) {
                        var vIdx = Add(triangle.Center + (triangle.Center - center).Normalized * offset);
                        if (vIdx != Vertices.Count-1) {
                            Debug.LogWarning("the vertex is reused.");
                        }
                        var n = Vertices[vIdx];
                        SplitTriangle(triangle, n);
                    }
                    trianglesToSplit.Clear();
                }
            } while (trianglesToSplit.Count > 0);
        }

        public int[] BridgeEdgeLoops(List<Vertex> fromVertices, List<Vertex> toVertices, float uvScale = 1f) {
            var indices = new List<int>();
            var fromRing = new CircularReadonlyList<Vertex>(fromVertices);
            var toRing = new CircularReadonlyList<Vertex>(toVertices);
            var faces = new List<Face>();
            for (var i = 0; i < fromRing.Count; i++) {
                indices.Add(AddTriangle(fromRing[i], fromRing[i+1], toRing[i+1]));
                indices.Add(AddTriangle(fromRing[i], toRing[i+1], toRing[i]));
            }
            return indices.ToArray();
        }

        public int[] FillPolygon(List<TEdge> edges) {
            var result = new List<int>();
            if (edges.Count == 3) {
                result.Add(AddTriangle(new Triangle(edges[0], edges[1], edges[2])));
                return result.ToArray();
            }
            var c = edges[0].A;
            for (var i = 0; i < edges.Count-1; i++) {
                result.Add(AddTriangle(new Triangle(c, edges[i].B, edges[i+1].B)));
            }
            return result.ToArray();
        }

        public int[] FillPolygon(List<Vertex> edgeLoop, List<Vertex> hole) {
            var createdTriangles = new List<int>();

            // Debug.Log("polygon has " + edgeLoop.Count + " vertices.");
            // Debug.Log("hole has " + hole.Count + " vertices.");
            hole.Reverse();

            var vIdx = 0;
            var hIdx = 0;
            for (var h = 0; h < hole.Count; h++) {
                var v = hole[h];
                for (var i = 0; i < edgeLoop.Count-1; i++) {
                    var j = i < edgeLoop.Count-2 ? i+1 : 0;
                    var t = new Triangle(v, edgeLoop[j], edgeLoop[i]);
                    if (t.ContainsAnyVertex(hole) || t.ContainsAnyVertex(edgeLoop)) continue;
                    vIdx = i;
                    hIdx = h;
                    createdTriangles.Add(AddTriangle(t));
                    break;
                }
                if (createdTriangles.Count > 0) break;
            }
            if (createdTriangles.Count == 0) {
                Debug.LogWarning("Could not create a starting triangle to close the edge loop.");
            } else {
                var polygon = new List<Vertex>();
                polygon.AddRange(edgeLoop.GetRange(0, vIdx+1));
                polygon.AddRange(hole.GetRange(hIdx, hole.Count-hIdx));
                polygon.AddRange(hole.GetRange(0, hIdx+1));
                polygon.AddRange(edgeLoop.GetRange(vIdx+1, edgeLoop.Count-vIdx-1));
                
                // for (int i = 0; i < polygon.Count; i++) {
                //     DebugLocalPoint(polygon[i].pos, "pv-"+i+"-"+polygon[i].id);
                // }
                createdTriangles.AddRange(FillPolygon(polygon));
            }
            return createdTriangles.ToArray();
        }

        public int[] FillPolygon(List<Vertex> edgeLoop) {
            var center = GetCenter(edgeLoop);
            var normal = -Vector3.Cross(edgeLoop[1].Pos-edgeLoop[0].Pos, edgeLoop[2].Pos-edgeLoop[0].Pos).Normalized;
            var list = new LinkedList<Vertex>(edgeLoop);
            var current = list.First;
            var createdTriangles = new List<int>();
            var tAtOverflow = 0;
            while (list.Count > 2) {
                // search a vertex that has an inner angle of less than 180 forming with its neighbors
                var nextNeighbor = current.Next ?? list.First;
                var tangent = nextNeighbor.Value.Pos - current.Value.Pos;
                //DebugLocalVector(current.Value.pos, tangent, Color.green);
                var secondNextNeighbor = nextNeighbor.Next ?? list.First;
                var nextTangent = secondNextNeighbor.Value.Pos - nextNeighbor.Value.Pos;
                //DebugLocalVector(nextNeighbor.Value.pos, nextTangent, Color.green);
                var vin = Vector3.Cross(tangent, normal);
                //DebugLocalVector(current.Value.pos, vin, Color.red);
                var nextIn = Vector3.Cross(nextTangent, normal);
                //DebugLocalVector(nextNeighbor.Value.pos, nextIn, Color.red);
                if (Vector3.Dot(vin, nextTangent) > 0) {
                    var tNew = new Triangle(current.Value, secondNextNeighbor.Value, nextNeighbor.Value);
                    if (tNew.ContainsAnyVertex(list)) {
                        // this would be an invalid triangle
                        current = current.Next ?? list.First;
                        tNew.RemoveTriangleLinks();
                    } else {
                        createdTriangles.Add(AddTriangle(tNew));
                        var newStartingPoint = secondNextNeighbor;
                        list.Remove(nextNeighbor);
                    }
                } else {
                    if (current.Next == null) {
                        // inhibit endless loops
                        if (createdTriangles.Count == tAtOverflow) {
                            Debug.LogWarning("Could not create any more triangles with " + list.Count + " vertices left.");
                            break;
                        }
                        tAtOverflow = createdTriangles.Count;
                    }
                    current = current.Next ?? list.First;
                }
                //if (createdTriangles.Count > 0) break;
            }
            return createdTriangles.ToArray();
        }

        public void Clear() {
            Vertices.Clear();
            Triangles.Clear();
        }

        public Vector3 GetCenter() {
            var sum = Vector3.Zero;
            foreach (var vertex in Vertices) {
                sum += vertex.Pos;
            }
            return sum / Vertices.Count;
        }

        public Vector3 GetCenter(IEnumerable<Vertex> l) {
            var sum = Vector3.Zero;
            var items = 0;
            foreach (var vertex in l) {
                sum += vertex.Pos;
                items++;
            }
            return sum / items;
        }

        public void FlipNormals() {
            foreach (var triangle in Triangles) {
                triangle.FlipNormal();
            }
        }

        public void FlipNormals(IEnumerable<int> l) {
            foreach (var i in l) {
                Triangles[i].FlipNormal();
            }
        }

        public void FlipNormals(IEnumerable<Triangle> l) {
            foreach (var t in l) {
                t.FlipNormal();
            }
        }

        public void SetUVBoxProjection0(float uvScale) {
            foreach (var triangle in Triangles) {
                triangle.SetUVProjected(uvScale);
            }
        }

        public void SetUVBoxProjection(float uvScale) {
            var connectedFaces = new List<Face>();
            foreach (var face in Triangles) {
                var dlr = Mathf.Abs(Vector3.Dot(face.Normal, Vector3.Left));
                var dfb = Mathf.Abs(Vector3.Dot(face.Normal, Vector3.Backward));
                var dud = Mathf.Abs(Vector3.Dot(face.Normal, Vector3.Up));
                face.Uv0 = new Vector2((dlr*face.V0.Pos.Z + dfb*face.V0.Pos.X + dud*face.V0.Pos.X) * uvScale, (dlr*face.V0.Pos.Y + dfb*face.V0.Pos.Y + dud*face.V0.Pos.Z) * uvScale);
                face.Uv1 = new Vector2((dlr*face.V1.Pos.Z + dfb*face.V1.Pos.X + dud*face.V1.Pos.X) * uvScale, (dlr*face.V1.Pos.Y + dfb*face.V1.Pos.Y + dud*face.V1.Pos.Z) * uvScale);
                face.Uv2 = new Vector2((dlr*face.V2.Pos.Z + dfb*face.V2.Pos.X + dud*face.V2.Pos.X) * uvScale, (dlr*face.V2.Pos.Y + dfb*face.V2.Pos.Y + dud*face.V2.Pos.Z) * uvScale);
            }
        }

        public void SetUVBoxProjection(IEnumerable<int> l, float uvScale) {
            foreach (var i in l) {
                Triangles[i].SetUVProjected(uvScale);
            }
        }

        public void Rotate(Quaternion rotation) {
            foreach (var v in Vertices) {
                v.Pos = v.Pos * rotation;
            }
        }

        public void Translate(Vector3 offset) {
            foreach (var v in Vertices) {
                v.Pos = v.Pos + offset;
            }
        }

        public Vector3 WorldPosition(Vertex v) {
            if (Transform != null) {
                return Transform.TransformPoint(v.Pos);
            }
            return v.Pos;
        }

        public Vector3 WorldPosition(Vector3 v) {
            if (Transform != null) {
                return Transform.TransformPoint(v);
            }
            return v;
        }

        public Vector3 LocalPosition(Vector3 v) {
            if (Transform != null) {
                return Transform.WorldToLocal(v);
            }
            return v;
        }

        public bool TriangleTriangleIntersection(Triangle triangle1, Triangle triangle2, out Vector3[] intersections) {
            var verticesInFront = 0;
            var verticesBehind = 0;
            foreach  (var v in triangle2.GetVertices()) {
                if (triangle1.FacesPoint(v.Pos)) {
                    verticesInFront++;
                } else {
                    verticesBehind++;
                }
            }
            if (verticesInFront == 0 || verticesBehind == 0) {
                intersections = new Vector3[0];
                return false;
            }
            var intersectionPoints = new List<Vector3>();
            Vector3 intersection;
            if (triangle1.EdgeIntersection(triangle2.V0.Pos, triangle2.V1.Pos, out intersection)) {
                intersectionPoints.Add(intersection);
            }
            if (triangle1.EdgeIntersection(triangle2.V1.Pos, triangle2.V2.Pos, out intersection)) {
                intersectionPoints.Add(intersection);
            }
            if (triangle1.EdgeIntersection(triangle2.V2.Pos, triangle2.V0.Pos, out intersection)) {
                intersectionPoints.Add(intersection);
            }
            if (triangle2.EdgeIntersection(triangle1.V0.Pos, triangle1.V1.Pos, out intersection)) {
                intersectionPoints.Add(intersection);
            }
            if (triangle2.EdgeIntersection(triangle1.V1.Pos, triangle1.V2.Pos, out intersection)) {
                intersectionPoints.Add(intersection);
            }
            if (triangle2.EdgeIntersection(triangle1.V2.Pos, triangle1.V0.Pos, out intersection)) {
                intersectionPoints.Add(intersection);
            }
            if (intersectionPoints.Count == 0) {
                intersections = new Vector3[0];
                return false;
            }
            if (intersectionPoints.Count != 2) {
                Debug.LogWarning("TriangleTriangleIntersection found " + intersectionPoints.Count + " points.");
                intersections = intersectionPoints.ToArray();
                return false;
            }
            intersections = intersectionPoints.ToArray();
            return true;
        }

        public void AddConnector(MeshObject other) {
            var changedTriangles = new List<Triangle>();
            var center = GetCenter();
            var otherCenter = LocalPosition(other.WorldPosition(other.GetCenter()));
            var projectionDirection = (center - otherCenter).Normalized;
            var centerVertex = new Vertex(center + (center - otherCenter) * 10f);

            //DebugLocalPoint(center, "DEBUG-Center");
            var newVertices = new List<Vertex>();
            var addedVertices = new List<Vertex>();
            foreach (var v in other.Vertices) {
                newVertices.Add(Vertices[Add(LocalPosition(other.WorldPosition(v)))]);
            }
            var ring = new CircularReadonlyList<Vertex>(newVertices);
            for (var i = 0; i < newVertices.Count; i++) {
                var cutTriangle = new Triangle(centerVertex, ring[i], ring[i+1]);
                var deferredActions = new List<DeferredAction>();
                foreach (var triangle in Triangles) {
                    if (triangle.FacesPoint(ring[i].Pos)) {
                        if (triangle.RayHit(ring[i].Pos, projectionDirection, false, out _, out var intersection)) {
                            addedVertices.Add(Vertices[Add(intersection)]);
                        }
                    }
                }
            }

            var cutObject = new MeshObject();
            cutObject.Transform = Transform;
            var fromVertices = new List<Vertex>();
            var toVertices = new List<Vertex>();
            foreach (var v in addedVertices) {
                var localPos = v.Pos;
                toVertices.Add(cutObject.Vertices[cutObject.Add(localPos + projectionDirection * 0.5f)]);
                fromVertices.Add(cutObject.Vertices[cutObject.Add(localPos - projectionDirection * 0.5f)]);
            }
            cutObject.BridgeEdgeLoops(fromVertices, toVertices, 1);
            cutObject.CreateTriangleFan(toVertices);
            cutObject.FlipNormals(cutObject.CreateTriangleFan(fromVertices));

            var outerVertices = RemoveEverythingInside(cutObject);
            var createdTriangles = FillPolygon(outerVertices, addedVertices);
            newVertices.Reverse();
            BridgeEdgeLoops(addedVertices, newVertices, UvScale);
        }

        public List<Vertex> RemoveEverythingInside(MeshObject other) {
            var affectedVertices = new HashSet<Vertex>();
            var affectedTriangles = new HashSet<Triangle>();
            var outerVertices = new HashSet<Vertex>();
            // first check vertices
            foreach (var v in Vertices) {
                if (other.Contains(v.Pos)) {
                    affectedVertices.Add(v);
                    foreach (var t in v.Triangles) {
                        affectedTriangles.Add(t);
                        outerVertices.Add(t.V0);
                        outerVertices.Add(t.V1);
                        outerVertices.Add(t.V2);
                    }
                }
            }
            foreach (var t in Triangles) {
                if (affectedTriangles.Contains(t)) {
                    continue;
                }
                foreach (var ot in other.Triangles) {
                    Vector3[] intersections;
                    if (TriangleTriangleIntersection(t, ot, out intersections)) {
                        affectedTriangles.Add(t);
                        outerVertices.Add(t.V0);
                        outerVertices.Add(t.V1);
                        outerVertices.Add(t.V2);
                        break;
                    }
                }
            }
            outerVertices.RemoveWhere( v => affectedVertices.Contains(v));

            // Debug.Log("Found " + affectedVertices.Count + " vertices and " + affectedTriangles.Count + " triangles affected.");
            // Debug.Log("Found " + innerVertices.Count + " inner and " + outerVertices.Count + " outer vertices.");
            // foreach (Vertex v in outerVertices) {
            //     DebugLocalPoint(v.pos, Color.blue);
            // }
            foreach(var v in affectedVertices) {
                Remove(v);
            }
            foreach(var t in affectedTriangles) {
                Remove(t);
            }

            outerVertices.RemoveWhere(v => v.Triangles.Count == 0);
            if (outerVertices.Count == 0) {
                Debug.LogWarning("There was nothing to remove inside the cutting object");
            }
            var ov = new List<Vertex>(outerVertices);
            return SortConnectedVertices(ov);
        }

        public List<Vertex> FindBoundaryAround(List<Vertex> surounding) {
            Debug.Log("boundary check: got " + surounding.Count + " vertices.");
            var result = new List<Vertex>();
            var attachedTriangles = new HashSet<Triangle>();
            surounding.ForEach(v => v.Triangles.ForEach(t => attachedTriangles.Add(t)));
            Debug.Log("attached triangles: " + attachedTriangles.Count);
            var edges = new List<TEdge>();
            foreach (var triangle in attachedTriangles) {
                edges.AddRange(triangle.GetNonManifoldEdges());
            }
            Debug.Log("non-manifold edges: " + edges.Count);
            if (edges.Count == 0) {
                Debug.LogWarning("expected non-manifold edges from: " + new List<Triangle>(attachedTriangles).Elements() + " using " + surounding.Elements());
                return result;
            }
            edges.ForEach(e => e.Flip());
            var linked = new LinkedList<TEdge>();
            var start = edges[edges.Count-1];
            linked.AddFirst(start);
            edges.Remove(start);
            var end = start;
            var deadStart = false;
            var deadEnd = false;
            while (edges.Count > 0) {
                var successor = edges.Find(e => e.A == end.B);
                if (successor == null) {
                    successor = edges.Find(e => e.B == end.B);
                    if (successor != null) {
                        edges.Remove(successor);
                        successor.Flip();
                    }
                } else {
                    edges.Remove(successor);
                }
                if (successor != null) {
                    linked.AddLast(successor);
                    end = successor;
                } else {
                    deadEnd = true;
                }
                var predecessor = edges.Find(e => e.B == start.A);
                if (predecessor == null) {
                    predecessor = edges.Find(e => e.A == start.A);
                    if (predecessor != null) {
                        edges.Remove(predecessor);
                        predecessor.Flip();
                    }
                } else {
                    edges.Remove(predecessor);
                }
                if (predecessor != null) {
                    linked.AddFirst(predecessor);
                    start = predecessor;
                } else {
                    deadStart = true;
                }
                if (deadEnd && deadStart) {
                    Debug.LogWarning("Could not find a linked list of edges, " + edges.Count + " left.");
                    // build what we have so far for debugging
                    Build(TargetActor, Material);
                    throw new System.Exception("Could not find a linked list of edges.");
                    //break;
                }
            }
            foreach (var e in linked) {
                result.Add(e.A);
            }
            return result;
        }

        public List<Vertex> SortConnectedVertices(ICollection<Vertex> l) {
            var loop = new List<Vertex>();
            loop.AddRange(l);
            // ignore stray vertices
            loop.RemoveAll(v => v.Triangles.Count == 0);
            var sortedList = new List<Vertex>();
            var start = loop[0];
            if (!Vertices.Contains(start)) {
                Debug.LogWarning("Found a starting vertex " + start + " that is not part of this object.");
            }
            Vertex next = null;
            sortedList.Add(start);
            loop.Remove(start);
            while (loop.Count > 0) {
                foreach (var triangle in start.Triangles) {
                    // sanity check
                    if (!Triangles.Contains(triangle)) {
                        Debug.LogWarning("A triangle is linked in " + start + " which is not part of this object: " + triangle);
                        continue;
                    }
                    foreach (var v in triangle.GetVertices()) {
                        if (v != start && loop.Contains(v) && !IsSharedEdge(start, v)) {
                            next = v;
                            break;
                        }
                    }
                    if (next != null) break;
                }
                if (next != null) {
                    sortedList.Add(next);
                    loop.Remove(next);
                    start = next;
                    if (!Vertices.Contains(next)) {
                        Debug.LogWarning("Found a vertex " + next + " that is not part of this object.");
                        break;
                    }
                    next = null;
                } else {
                    Debug.LogWarning("The list is not connected. " + loop.Count + " vertices are left: " + loop.Elements());
                    break;
                }
            }
            return sortedList;
        }

        public bool IsSharedEdge(Vertex a, Vertex b) {
            var common = a.Triangles.FindAll(t => b.Triangles.Contains(t));
            return common.Count > 1;
        }

        public bool Contains(Vector3 v) {
            foreach (var t in Triangles) {
                if (!IsBehind(t, v)) {
                    return false;
                }
            }
            return true;
        }

        public void Decimate(int maxVertices) {
            while (Vertices.Count > maxVertices) {
                var minDist = float.MaxValue;
                var i1 = 0;
                var i2 = 0;
                for (var i = 0; i < Vertices.Count-1; i++) {
                    for (var j = i+1; j < Vertices.Count; j++) {
                        var dist = (Vertices[i].Pos - Vertices[j].Pos).Length;
                        if (dist < minDist) {
                            minDist = dist;
                            i1 = i; i2 = j;
                        }
                    }
                }
                Vertices[i1].Pos = (Vertices[i1].Pos + Vertices[i2].Pos)/2;
                Vertices.RemoveAt(i2);
            }
        }

        public void DebugLocalPoint(Vector3 vector3, string name) {
            Actor go = new EmptyActor();
            go.Name = name;
            if (Transform != null) {
                go.Position = Transform.TransformPoint(vector3);
            } else {
                go.Position = vector3;
            }
        }

        public void DebugLocalLine(Vector3 a, Vector3 b, Color color) {
            var start = a - (b-a).Normalized * 10f;
            var end = b + (b-a).Normalized * 10f;
            DebugDraw.DrawLine(Transform.TransformPoint(start), Transform.TransformPoint(end), color, 5);
        }

        public void DebugLocalEdge(Vector3 a, Vector3 b, Color color) {
            DebugDraw.DrawLine(Transform.TransformPoint(a), Transform.TransformPoint(b), color, 5);
        }

        public void DebugLocalVector(Vector3 a, Vector3 direction, Color color) {
            var end = a + direction * 200f;
            DebugDraw.DrawLine(Transform.TransformPoint(a), Transform.TransformPoint(end), color, 5);
        }

        public void DebugLocalPoint(Vector3 a, Color color) {
            var w = Transform.TransformPoint(a);
            var d = 50f;
            DebugDraw.DrawLine(w-Vector3.Right*d, w+Vector3.Right*d, color, 5);
            DebugDraw.DrawLine(w-Vector3.Up*d, w+Vector3.Down*d, color, 5);
            DebugDraw.DrawLine(w-Vector3.Forward*d, w+Vector3.Backward*d, color, 5);
        }

        void DumpVertexLists(List<List<Vertex>> lists, string name) {
            // TODO: add extensions
            //System.IO.File.WriteAllLines(name, lists.ConvertAll<string>(l => l.Elements()));
        }

        string ToString(Vector3[] l) {
            var result = l.Aggregate("[", (current, v) => current + $"({v.X:F4},{v.Y:F4},{v.Z:F4})");
            result += "]";
            return result;
        }
        public Mesh BuildMesh(Mesh mesh) {
            var uniqueVertices = 0;
            foreach (var vertex in Vertices) {
                uniqueVertices += vertex.Triangles.Count;
            }
            var verts = new List<Float3>();
            var uv = new List<Float2>();
            //int[] tris = new int[3 * triangles.Count];
            var tris = new List<int>(3 * Triangles.Count);
            var normals = new List<Float3>();

            ushort vertIndex = 0;
            foreach (var triangle in Triangles) {
                var n = triangle.Normal;
                verts.Add(triangle.V0.Pos);
                uv.Add(triangle.Uv0);
                tris.Add(vertIndex);
                normals.Add(CalculateNormal(triangle.V0, triangle));
                vertIndex++;
                verts.Add(triangle.V1.Pos);
                uv.Add(triangle.Uv1);
                tris.Add(vertIndex);
                normals.Add(CalculateNormal(triangle.V1, triangle));
                vertIndex++;
                verts.Add(triangle.V2.Pos);
                uv.Add(triangle.Uv2);
                tris.Add(vertIndex);
                normals.Add(CalculateNormal(triangle.V2, triangle));
                vertIndex++;
            }
            mesh.UpdateMesh(verts, tris, normals, null, uv);
            _cachedVertices = verts.ToArray();
            _cachedTriangles = tris.ToArray();
            return mesh;
        }

        Vector3 CalculateNormal(Vertex v, Triangle triangle) {
            var n = Vector3.Zero;
            var ct = 0;
            switch (Shading) {
                case ShadingType.Flat:
                    n = triangle.Normal;
                    break;
                case ShadingType.Smooth:
                    foreach (var t in v.Triangles) {
                        n += t.Normal;
                    }
                    n /= v.Triangles.Count;
                    break;
                case ShadingType.Auto:
                    n = triangle.Normal;
                    ct = 1;
                    foreach (var t in v.Triangles) {
                        if (t != triangle && Vector3.Angle(triangle.Normal, t.Normal) < 60f) {
                            n += t.Normal;
                            ct++;
                        }
                    }
                    n /= ct;
                    break;
            }
            return n;
        }
        
        public virtual void PrepareForBuild() {

        }

        public void Build(Actor target, Material material) {
            PrepareForBuild();
            var childByMaterial = target.Children.FirstOrDefault(t => t.Name == "mat-" + CreateMaterialName(material));
            if (childByMaterial == null)
            {
                childByMaterial = new EmptyActor();
                childByMaterial.Name = "mat-" + CreateMaterialName(material);
                childByMaterial.Parent = target;
                childByMaterial.LocalPosition = Vector3.Zero;
                childByMaterial.LocalOrientation = Quaternion.Identity;
                childByMaterial.LocalScale = Vector3.One;
                childByMaterial.StaticFlags = target.StaticFlags;
            }

            var model = Content.CreateVirtualAsset<Model>();
            model.SetupLODs(new[] { 1 });
            BuildMesh(model.LODs[0].Meshes[0]);
            var childModel = childByMaterial.GetOrAddChild<StaticModel>();
            childModel.Model = model;
            childModel.LocalScale = new Float3(1);
            childModel.SetMaterial(0, material);
            var meshCollider = childByMaterial.GetOrAddChild<MeshCollider>();
            var collisionData = meshCollider.CollisionData;
            if (collisionData == null)
            {
                collisionData = Content.CreateVirtualAsset<CollisionData>();
                meshCollider.CollisionData = collisionData;
            }
            collisionData.CookCollision(CollisionDataType.TriangleMesh, _cachedVertices, _cachedTriangles);
            meshCollider.CollisionData = collisionData;
        }

        public void UpdateCollisionData(CollisionData collisionData)
        {
            collisionData.CookCollision(CollisionDataType.TriangleMesh, _cachedVertices, _cachedTriangles);
        }

        public static bool SameInTolerance(Vector3 a, Vector3 b) {
            return (a-b).LengthSquared <= EpsilonSquared;
        }

        public static string CreateMaterialName(string path)
        {
            var shortName = path;
            var idx = shortName.LastIndexOf('/');
            if (idx < 0)
                idx = shortName.LastIndexOf('\\');
            if (idx >= 0)
                shortName = shortName[(idx+1)..];
            if (shortName.EndsWith(".flax"))
            {
                shortName = shortName[..^5];
            }

            return shortName;
        }
        
        public static string CreateMaterialName(Material material)
        {
            return material == null ? "<none>" : CreateMaterialName(material.Path);
        }
    }
}
