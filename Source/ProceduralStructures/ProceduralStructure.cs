using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace Game.ProceduralStructures {
    public class ProceduralStructure {
        /*
        public void RebuildLadder(LadderDefinition ladder, GameObject target) {
            Building building = new Building();

            // these are the center points of the poles
            Vector3 originLeft = Vector3.left * (ladder.stepWidth + ladder.poleThickness)/2;
            Vector3 originRight = Vector3.right * (ladder.stepWidth + ladder.poleThickness)/2;

            // first two faces are the bottom faces of the poles
            Face leftBottom = Face.CreateXZPlane(ladder.poleThickness, ladder.poleThickness).MoveFaceBy(originLeft).InvertNormals();
            Face rightBottom = Face.CreateXZPlane(ladder.poleThickness, ladder.poleThickness).MoveFaceBy(originRight).InvertNormals();
            BuildingObject leftPole = new BuildingObject();
            leftPole.material = ladder.ladderMaterial;
            BuildingObject rightPole = new BuildingObject();
            rightPole.material = ladder.ladderMaterial;
            leftPole.AddFace(leftBottom);
            float poleHeight = ladder.stepHeight * (ladder.steps+1);
            leftPole.AddFaces(Builder.ExtrudeEdges(leftBottom, Vector3.up * poleHeight, ladder.uvScale));
            rightPole.AddFace(rightBottom);
            rightPole.AddFaces(Builder.ExtrudeEdges(rightBottom, Vector3.up * poleHeight, ladder.uvScale));
            leftPole.AddFace(leftBottom.DeepCopy().MoveFaceBy(Vector3.up * poleHeight).InvertNormals());
            rightPole.AddFace(rightBottom.DeepCopy().MoveFaceBy(Vector3.up * poleHeight).InvertNormals());
            leftPole.RotateUVs();
            rightPole.RotateUVs();

            BuildingObject steps = new BuildingObject();
            for (int i = 0; i < ladder.steps; i++) {
                originLeft.y += ladder.stepHeight;
                originRight.y += ladder.stepHeight;
                leftPole.MakeHole(originLeft, Vector3.right, Vector3.up, ladder.stepThickness, ladder.stepThickness);
                rightPole.MakeHole(originRight, Vector3.left, Vector3.up, ladder.stepThickness, ladder.stepThickness);
                Face leftHoleFace = leftPole.FindFirstFaceByTag(Builder.CUTOUT);
                Face rightHoleFace = rightPole.FindFirstFaceByTag(Builder.CUTOUT);
                if (leftHoleFace != null && rightHoleFace != null) {
                    leftPole.RemoveFace(leftHoleFace);
                    rightPole.RemoveFace(rightHoleFace);
                    steps.AddFaces(Builder.BridgeEdgeLoops(rightHoleFace.GetVerticesList(), leftHoleFace.GetVerticesList(), ladder.uvScale));
                }
            }
            steps.material = ladder.ladderMaterial;
            building.AddObject(steps);

            building.AddObject(leftPole);
            building.AddObject(rightPole);

            building.Build(target, 0);
        }

        public void RebuildWall(WallDefinition wall, GameObject target) {
            int minNumberMarkers = wall.closeLoop ? 3 : 2;
            if (wall.points.Count < minNumberMarkers) {
                Debug.LogWarning("cannot build a wall with " + wall.points.Count + " corner(s).");
                return;
            }
            List<Vector3> points = new List<Vector3>(wall.points.Count);
            List<Vector3> originalPoints = new List<Vector3>(wall.points.Count);
            float maxY = float.MinValue;
            foreach (Transform t in wall.points) {
                Vector3 p = t.position;
                maxY = Mathf.Max(maxY, p.y);
            }
            foreach (Transform t in wall.points) {
                Vector3 p = t.position;
                originalPoints.Add(t.position);
                p.y = maxY;
                points.Add(p);
            }
            Vector3 centroid = Builder.FindCentroid(points);
            if (wall.sortMarkers) {
                wall.points.Sort(delegate(Transform a, Transform b) {
                    float angleA = Mathf.Atan2(a.position.z - centroid.z, a.position.x - centroid.x);
                    float angleB = Mathf.Atan2(b.position.z - centroid.z, b.position.x - centroid.x);
                    return angleA < angleB ? 1 : angleA > angleB ? -1 : 0;
                });
                points.Clear();
                originalPoints.Clear();
                foreach (Transform t in wall.points) {
                    Vector3 p = t.position;
                    originalPoints.Add(t.position);
                    p.y = maxY;
                    points.Add(p);
                }
            }
            for (int i = 0; i < points.Count; i++) {
                points[i] = points[i] - centroid;
                originalPoints[i] = originalPoints[i] - centroid;
            }
            Building building = new Building();
            BuildingObject wallObject = new BuildingObject();
            wallObject.material = wall.material;

            Vector3 wallHeight = new Vector3(0, wall.wallHeight, 0);
            Vector3 heightOffset = new Vector3(0, wall.heightOffset, 0);

            if (wall.closeLoop) {
                points.Add(points[0]);
            }
            List<Face> outerWall = Builder.ExtrudeEdges(points, wallHeight, wall.uvScale);
            List<Face> innerWall = Builder.CloneAndMoveFacesOnNormal(outerWall, wall.wallThickness, wall.uvScale);
            // project it back on the ground, this should be done by above function
            for (int i = 0; i < points.Count; i++) {
                Builder.MoveVertices(outerWall, points[i], Builder.MatchingVertex.XZ, originalPoints[i] + heightOffset);
            }
            for (int i = 0; i < outerWall.Count; i++) {
                Face outer = outerWall[i];
                Face inner = innerWall[i];
                // copy y values
                inner.d.y = outer.a.y;
                inner.a.y = outer.d.y;
                inner.b.y = outer.c.y;
                inner.c.y = outer.b.y;
            }
            wallObject.AddFaces(outerWall);
            wallObject.AddFaces(innerWall);
            // now close the top by extracting all BC edges and create faces
            List<Edge> innerEdges = new List<Edge>();
            List<Edge> outerEdges = new List<Edge>();
            foreach (Face face in innerWall) {
                innerEdges.Add(new Edge(face.b, face.c));
            }
            foreach (Face face in outerWall) {
                outerEdges.Add(new Edge(face.b, face.c));
            }
            List<Face> top = Builder.BridgeEdges(innerEdges, outerEdges, true, wall.uvScale);
            wallObject.AddFaces(top);

            if (!wall.closeLoop) {
                Face start = new Face(new Edge(innerWall[0].d, innerWall[0].c), new Edge(outerWall[0].b, outerWall[0].a));
                start.SetUVForSize(wall.uvScale);
                wallObject.AddFace(start);
                int i = outerWall.Count-1;
                Face end = new Face(new Edge(outerWall[i].d, outerWall[i].c), new Edge(innerWall[i].b, innerWall[i].a));
                end.SetUVForSize(wall.uvScale);
                wallObject.AddFace(end);
            }

            if (wall.generateCornerPieces) {
                for (int i = 0; i < originalPoints.Count; i++) {
                    Vector3 corner = originalPoints[i];
                    int j = i % outerWall.Count;
                    Vector3 direction = (innerWall[j].a - outerWall[j].a).normalized;
                    wallObject.AddObject(GenerateCornerPiece(corner + heightOffset, wallHeight.y+1, wall.wallThickness*2, direction, wall.uvScale));
                }
            }

            building.AddObject(wallObject);
            building.Build(target, 0);
            GameObject go = Building.GetChildByName(target, "LOD0");
            if (go != null) {
                go.transform.position = centroid;
            }

            if (wall.cornerPiece != null) {
                
                foreach (Transform t in wall.points) {
                    GameObject corner = GameObject.Instantiate(wall.cornerPiece);
                    corner.transform.parent = go.transform;
                    corner.transform.position = t.position + (centroid-t.position).normalized * wall.wallThickness/2;
                    corner.transform.LookAt(new Vector3(centroid.x, corner.transform.position.y, centroid.z));
                }
            }
        }

        private BuildingObject GenerateCornerPiece(Vector3 center, float height, float width,
                Vector3 inwards, float uvScale) {
            BuildingObject result = new BuildingObject();                    
            Face top = Face.CreateXZPlane(width, width);
            top.MoveFaceBy(Vector3.up * height).Rotate(Quaternion.LookRotation(inwards, Vector3.up));
            result.AddFaces(Builder.ExtrudeEdges(top, Vector3.down * height, uvScale));
            result.AddFace(top);
            result.position = center;
            return result;
        }
*/
        public void RebuildCave(CaveDefinition cave, Actor target) {
            foreach (var tunnel in cave.WayPointLists) {
                var tunnelObject = target.FindActor(tunnel.Name);
                if (tunnelObject == null) {
                    tunnelObject = new EmptyActor();
                    tunnelObject.Name = tunnel.Name;
                    tunnelObject.Parent = target;
                    tunnelObject.LocalPosition = Vector3.Zero;
                    tunnelObject.LocalScale = Vector3.One;
                    tunnelObject.LocalOrientation = Quaternion.Identity;
                    tunnelObject.StaticFlags = target.StaticFlags;
                }
                RebuildCave(cave, tunnel, tunnelObject);
            }
        }

        public MeshObject GetCaveConnection(CaveDefinition cave, Vector3 center, Tangent tangent) {
            // construct the shape
            var shapeEdgeList = new List<Vector3>();
            switch (cave.CrosscutShape) {
                case CaveDefinition.Shape.OShaped:
                shapeEdgeList = ConstructOShape(cave.BaseWidth, cave.BaseHeight);
                break;
                case CaveDefinition.Shape.Tunnel:
                default:
                shapeEdgeList = ConstructTunnelShape(cave.BaseWidth, cave.BaseHeight);
                break;
            }
            for(var i = 0; i < cave.ShapeSmoothing; i++) {
                shapeEdgeList = SmoothVertices(shapeEdgeList);
            }
            var localPos = tangent.Position;
            var localRotation = Quaternion.LookRotation(tangent.Direction, Vector3.Up);
            var localScale = new Vector3(tangent.ScaleWidth, tangent.ScaleHeight, 1f);
            var connector = new MeshObject();
            var currentEdgeLoop = connector.AddRange(Builder.MoveVertices(Builder.RotateVertices(
                Builder.ScaleVertices(shapeEdgeList, localScale), localRotation), localPos));
            return connector;
        }

        public void RebuildCave(CaveDefinition cave, WayPointList tunnel, Actor target) {
            var cavemesh = new MeshObject();
            // construct the shape
            var shapeEdgeList = new List<Vector3>();
            switch (cave.CrosscutShape) {
                case CaveDefinition.Shape.OShaped:
                shapeEdgeList = ConstructOShape(cave.BaseWidth, cave.BaseHeight);
                break;
                case CaveDefinition.Shape.Tunnel:
                default:
                shapeEdgeList = ConstructTunnelShape(cave.BaseWidth, cave.BaseHeight);
                break;
            }
            for(var i = 0; i < cave.ShapeSmoothing; i++) {
                shapeEdgeList = SmoothVertices(shapeEdgeList);
            }
            List<Vertex> previousEdgeLoop = null;
            var previousPosition = Vector3.Zero;
            var previousDirection = Vector3.Zero;
            var idx = 0;
            float uOffset = 0;
            foreach (var tangent in cave.GetTangents(tunnel)) {
                var localPos = tangent.Position - target.Position;
                var localRotation = Quaternion.LookRotation(tangent.Direction, Vector3.Up);
                var localScale = new Vector3(tangent.ScaleWidth, tangent.ScaleHeight, 1f);
                var currentEdgeLoop = cavemesh.AddRange(Builder.MoveVertices(Builder.RotateVertices(
                    Builder.ScaleVertices(shapeEdgeList, localScale), localRotation), localPos));
                if (previousEdgeLoop != null) {
                    // only add it if there is no overlap
                    var right = Vector3.Cross(tangent.Direction, Vector3.Up);
                    var right0 = Vector3.Cross(previousDirection, Vector3.Up);
                    var w = cave.BaseWidth/2;
                    var m = previousPosition + right0 * w - right * w;
                    // there is a collision if the current location is nearer than m
                    var plane = localPos + (previousPosition - localPos) / 2;
                    var planeNormal = (previousDirection + tangent.Direction) / 2;
                    if ((localPos - previousPosition).LengthSquared < (m - previousPosition).LengthSquared) {
                        //Debug.LogFormat("#{0}, m = {1}, localPos = {2}, previousPosition = {3}", idx, m, localPos, previousPosition);
                        // clamp generatedFaces to plane in normal direction and previous to opposite
                        //Debug.LogFormat("plane({0},{1}), current {2}, previous {3}", plane, planeNormal, currentEdgeLoop.Elements(), previousEdgeLoop.Elements());
                        cavemesh.ClampToPlane(currentEdgeLoop, previousEdgeLoop, plane, planeNormal);
                    }
                    var generatedTriangles = cavemesh.BridgeEdgeLoops(previousEdgeLoop, currentEdgeLoop, cave.UScale);
                    var cylinderCenter = cavemesh.GetCenter(currentEdgeLoop); //(cavemesh.GetCenter(currentEdgeLoop) + cavemesh.GetCenter(previousEdgeLoop))/2;
                    cavemesh.SetUVTunnelProjection(generatedTriangles, cylinderCenter, planeNormal, uOffset, cave.UScale, cave.VScale);
                    previousPosition = localPos;
                    previousDirection = tangent.Direction;
                    previousEdgeLoop = currentEdgeLoop;
                } else {
                    // this is the first crosscut
                    previousPosition = localPos;
                    previousDirection = tangent.Direction;
                    if (cave.CloseBeginning) {
                        var fanTriangles = cavemesh.CreateTriangleFan(currentEdgeLoop);
                        cavemesh.FlipNormals(fanTriangles);
                        cavemesh.SetUVBoxProjection(fanTriangles, cave.UScale);
                    }
                    cavemesh.SetVertexList("beginning", currentEdgeLoop);
                    previousEdgeLoop = currentEdgeLoop;
                }
                uOffset += (previousPosition - localPos).Length;
                idx++;
            }
            // this is the last crosscut
            if (cave.CloseEnd && previousEdgeLoop != null) {
                var fanTriangles = cavemesh.CreateTriangleFan(previousEdgeLoop);
                cavemesh.SetUVBoxProjection(fanTriangles, cave.UScale);
            }
            cavemesh.SetVertexList("end", previousEdgeLoop);
            if (cave.RandomizeVertices) {
                cavemesh.RandomizeVertices(cave.RandomDisplacement);
            }
            cavemesh.Shading = cave.ShadingType;
            cavemesh.Build(target, cave.Material);
        }

        public List<Vector3> SmoothVertices(List<Vector3> l) {
            var newList = new List<Vector3>(l.Count*2);
            for (var i = 0; i < l.Count; i++) {
                var j = i < l.Count-1 ? i+1 : 0;
                newList.Add(0.75f*l[i] + 0.25f*l[j]);
                newList.Add(0.25f*l[i] + 0.75f*l[j]);
            }
            return newList;
        }

        public List<Vector3> ConstructTunnelShape(float width, float height) {
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(-0.4f * width, 0.06f * height, 0);
            var c = new Vector3(-0.5f * width, 0.12f * height, 0);
            var d = new Vector3(-0.5f * width, 0.58f * height, 0);
            var e = new Vector3(-0.36f * width, 0.84f * height, 0);
            var f = new Vector3(-0.18f * width, 0.97f * height, 0);
            var g = new Vector3(0, height, 0);
            var h = new Vector3(-f.X, f.Y, 0);
            var i = new Vector3(-e.X, e.Y, 0);
            var j = new Vector3(-d.X, d.Y, 0);
            var k = new Vector3(-c.X, c.Y, 0);
            var l = new Vector3(-b.X, b.Y, 0);
            return new List<Vector3> { a, b, c, d, e, f, g, h, i, j, k, l };
        }

        private List<Vector3> ConstructOShape(float width, float height) {
            var result = new List<Vector3>(12);
            for (var i = 0; i < 12; i++) {
                result.Add(new Vector3(width/2 * Mathf.Sin(Mathf.DegreesToRadians * i*30), height/2 + height/2 * Mathf.Cos(Mathf.DegreesToRadians * i*30), 0));
            }
            return result;
        }

        public static Actor GetChildByName(Actor parent, string name)
        {
            return parent.Children.FirstOrDefault(a => a.Name.Equals(name));
        }

        public static Actor CreateEmptyChild(Actor parent, string name) {
            Actor go = null;
            if (name != "") {
                go = GetChildByName(parent, name);
                if (go != null) return go;
            }
            go = new EmptyActor();
            go.Name = name;
            go.Parent = parent;
            go.LocalPosition = Vector3.Zero;
            go.LocalOrientation = Quaternion.Identity;
            go.LocalScale = Vector3.One;
            go.StaticFlags = parent.StaticFlags;
            return go;
        }

        public void ConstructFrameHouse(FrameHouse house, Actor target) {
            var frameConstruction = new MeshObject();
            frameConstruction.Transform = target.Transform;
            var frame = house.Frame;
            var thick = house.BeamThickness;
            var baseExtends = new Vector3(thick/2, thick/2, thick/2);
            var sw = new DebugStopwatch().Start("Creating frame");

            foreach (var edge in frame.Edges) {
                if (edge.A < frame.Points.Count && edge.B < frame.Points.Count) {
                    frameConstruction.AddObject(Beam(frame.Points[edge.A], frame.Points[edge.B], house.BeamThickness, house.UvScale));
                }
            }
            foreach (var v in frame.Points) {
                frameConstruction.AddObject(BeamConnector(v, house.BeamThickness, house.UvScale));
            }
            frameConstruction.CleanupMesh();
            Debug.Log(sw.Stop() + ", vertices: " + frameConstruction.VerticesCount + ", triangles: " + frameConstruction.TrianglesCount);

            var wallConstruction = new MeshObject();
            wallConstruction.Transform = target.Transform;
            var vertices = wallConstruction.AddRange(frame.Points);
            var edges = new List<TEdge>();
            frame.Edges.ForEach(e => edges.Add(new TEdge(vertices[e.A], vertices[e.B])));
            var wallTriangles = wallConstruction.CloseUnorderedEdgeLoops(edges, house.UvScale);
            wallConstruction.SetNormals(wallTriangles);
            wallConstruction.SetUVBoxProjection(house.UvScale);

            frameConstruction.Build(target, house.BeamMaterial);
            wallConstruction.Build(target, house.WallMaterial);
        }

        MeshObject Beam(Vector3 a, Vector3 b, float thickness, float uvScale = 1f) {
            var o = new MeshObject();
            var beamLength = (b-a).Length - thickness;
            o.AddCube(Vector3.Zero, new Vector3(beamLength/2, thickness/2, thickness/2));
            o.SetUVBoxProjection(uvScale);
            o.Rotate(Quaternion.LookRotation(b-a, Vector3.Up) * Quaternion.RotationAxis(Vector3.Up, -Mathf.Pi/2));
            o.Translate(a + (b-a)/2);
            return o;
        }

        MeshObject BeamConnector(Vector3 a, float thickness, float uvScale = 1f) {
            var o = new MeshObject();
            o.AddCube(Vector3.Zero, new Vector3(thickness/2, thickness/2, thickness/2));
            o.SetUVBoxProjection(uvScale);
            o.Rotate(Quaternion.RotationAxis(Vector3.Up, -Mathf.Pi/2));
            o.Translate(a);
            return o;
        }
    }
}