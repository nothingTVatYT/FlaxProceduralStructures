using System.Collections;
using System.Collections.Generic;
using FlaxEngine;

namespace Game.ProceduralStructures {
    public class BuildingObject {
        public Vector3 Position = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public List<Face> Faces = new();
        public Material Material;

        public bool IsEmpty => Faces.Count == 0;

        public void Clear() {
            Faces.Clear();
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
        }

        public BuildingObject ResetTransform() {
            foreach (var face in Faces) {
                face.Rotate(Quaternion.Invert(Rotation)).MoveFaceBy(-Position);
            }
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            return this;
        }

        public BuildingObject ApplyTransform() {
            foreach (var face in Faces) {
                face.MoveFaceBy(Position).Rotate(Rotation);
            }
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            return this;
        }

        public BuildingObject ApplyDefaultMaterial() {
            if (Material != null) {
                foreach (var face in Faces) {
                    if (face.Material == null) {
                        face.Material = Material;
                    }
                }
            }
            return this;
        }

        public BuildingObject AddFace(Face face) {
            Faces.Add(face);
            return this;
        }

        public BuildingObject AddFaces(List<Face> newFaces) {
            Faces.AddRange(newFaces);
            return this;
        }

        public BuildingObject AddObject(BuildingObject other) {
            other.ApplyTransform().ApplyDefaultMaterial();
            Faces.AddRange(other.Faces);
            return this;
        }

        public BuildingObject RemoveFace(Face face) {
            if (!Faces.Remove(face))
                Debug.LogWarning("could not remove " + face);
            return this;
        }

        public Face LocalToWorld(Face face) {
            face.Rotate(Quaternion.Invert(Rotation)).MoveFaceBy(-Position);
            return face;
        }

        public BuildingObject TranslateFaces(Vector3 translation) {
            foreach (var face in Faces) {
                face.MoveFaceBy(translation);
            }
            Position += translation;
            return this;
        }

        public BuildingObject TranslatePosition(Vector3 translation) {
            Position -= translation * Quaternion.Invert(Rotation);
            return this;
        }

        public BuildingObject TransformFaces(Vector3 translation, Quaternion rot) {
            foreach (var face in Faces) {
                face.MoveFaceBy(translation).Rotate(rot);
            }
            Position += translation;
            Rotation *= rot;
            return this;
        }

        public BuildingObject RotateFaces(Quaternion rot) {
            foreach (var face in Faces) {
                face.Rotate(rot);
            }
            return this;
        }

        public BuildingObject MoveFaces(Vector3 offset) {
            foreach (var face in Faces) {
                face.MoveFaceBy(offset);
            }
            return this;
        }

        /* public BuildingObject RotateUVs() {
            foreach (var face in faces) {
                face.RotateUV();
            }
            return this;
        } */

        public BoundingBox CalculateGlobalBounds()
        {
            var bounds = new BoundingBox
            {
                Center = (Faces[0].A + Position) * Rotation
            };
            foreach (var face in Faces) {
                foreach (var p in face.GetVertices()) {
                    bounds.Merge((p + Position) * Rotation);
                }
            }
            return bounds;
        }

        public BuildingObject SetUVBoxProjection(float uvScale) {
            foreach (var face in Faces) {
                var dlr = Mathf.Abs(Vector3.Dot(face.Normal, Vector3.Left));
                var dfb = Mathf.Abs(Vector3.Dot(face.Normal, Vector3.Backward));
                var dud = Mathf.Abs(Vector3.Dot(face.Normal, Vector3.Up));
                face.UvA = new Vector2((dlr*face.A.Z + dfb*face.A.X + dud*face.A.X) * uvScale, (dlr*face.A.Y + dfb*face.A.Y + dud*face.A.Z) * uvScale);
                face.UvB = new Vector2((dlr*face.B.Z + dfb*face.B.X + dud*face.B.X) * uvScale, (dlr*face.B.Y + dfb*face.B.Y + dud*face.B.Z) * uvScale);
                face.UvC = new Vector2((dlr*face.C.Z + dfb*face.C.X + dud*face.C.X) * uvScale, (dlr*face.C.Y + dfb*face.C.Y + dud*face.C.Z) * uvScale);
                if (!face.IsTriangle) {
                    face.UvD = new Vector2((dlr*face.D.Z + dfb*face.D.X + dud*face.D.X) * uvScale, (dlr*face.D.Y + dfb*face.D.Y + dud*face.D.Z) * uvScale);
                }
            }
            return this;
        }

        public BuildingObject IndentFace(Face face, Vector3 direction, float uvScale) {
            Faces.Remove(face);
            Faces.AddRange(Builder.IndentFace(face, direction, uvScale));
            return this;
        }

        public BuildingObject ExtrudeEdges(List<Vector3> vertices, Vector3 direction, float uvScale) {
            Faces.AddRange(Builder.ExtrudeEdges(vertices, direction, uvScale));
            return this;
        }

        public BuildingObject MoveVertices(Vector3 from, Builder.MatchingVertex matching, Vector3 to) {
            Builder.MoveVertices(Faces, from, matching, to);
            return this;
        }

        public List<Vector3> GetAllVertices() {
            var result = new List<Vector3>();
            foreach (var face in Faces) {
                result.AddRange(face.GetVerticesList());
            }
            return result;
        }

        public BuildingObject InvertNormals() {
            foreach (var face in Faces) {
                face.InvertNormals();
            }
            return this;
        }

        public BuildingObject SplitFace(Face face, Vector3 newVertex) {
            Faces.Remove(face);
            Faces.Add(new Face(face.A, face.B, newVertex));
            Faces.Add(new Face(face.B, face.C, newVertex));
            if (face.IsTriangle) {
                Faces.Add(new Face(face.C, face.A, newVertex));
            } else {
                Faces.Add(new Face(face.C, face.D, newVertex));
                Faces.Add(new Face(face.D, face.A, newVertex));
            }
            return this;
        }

        public BuildingObject MakeHole(Vector3 origin, Vector3 direction, Vector3 up, float width, float height, Material material = null, float maxDistance = 0f, float uvScale = 0.01f) {
            var result = new List<Face>();
            Vector3 intersection;
            bool fromBack;
            var affectedFaces = new List<Face>();
            var unaffectedFaces = new List<Face>();
            foreach (var face in Faces) {
                if (face.RayHit(origin, direction, false, out fromBack, out intersection)) {
                    var distance = Vector3.Distance(origin, intersection);
                    if (maxDistance == 0f || distance <= maxDistance) {
                        face.SortOrder = distance;
                        affectedFaces.Add(face);
                    } else {
                        unaffectedFaces.Add(face);
                    }
                } else {
                    unaffectedFaces.Add(face);
                }
            }
            // sort by distance
            affectedFaces.Sort((f1, f2) => f1.SortOrder.CompareTo(f2.SortOrder));
            // keep track of the previous cut face
            Face previousCutFace = null;

            for (var cut = 0; cut < 4; cut++) {
                Face thisCutFace = null;
                foreach (var face in affectedFaces) {
                    if (face.RayHit(origin, direction, false, out fromBack, out intersection)) {
                        // compute corners of the hole
                        var localRight = Vector3.Cross(face.Normal, up);
                        var ha = intersection - localRight * width/2 - up * height/2;
                        var hb = ha + up * height;
                        var hc = hb + localRight * width;
                        var hd = hc - up * height;
                        // a plane through ha,hb and any point on the face normal that is not on the face defines our left cutting plane
                        // the normal of this cutting plane is face.normal x up = localRight
                        var vCut = ha;
                        var nCut = localRight;
                        if (cut == 1) {
                            vCut = hc;
                        } else if (cut == 2) {
                            vCut = hb;
                            nCut = up;
                        } else if (cut == 3) {
                            vCut = ha;
                            nCut = up;
                        }
                        var dA = Vector3.Dot(face.A-vCut, nCut);
                        var dB = Vector3.Dot(face.B-vCut, nCut);
                        var dC = Vector3.Dot(face.C-vCut, nCut);
                        var dD = Vector3.Dot(face.D-vCut, nCut);
                        //Debug.LogFormat("cut{4}: da={0},db={1},dc={2},dd={3}", dA, dB, dC, dD, cut);
                        // if all determinants have the same sign there is no edge to split
                        if (Mathf.Sign(dA) != Mathf.Sign(dB) || Mathf.Sign(dB) != Mathf.Sign(dC) || Mathf.Sign(dC) != Mathf.Sign(dD)) {
                            // check which edges we have to split
                            var rAB = (dA*dB)>=0 ? 0 : Mathf.Abs(dA/Mathf.Abs(dA-dB));
                            var rBC = (dB*dC)>=0 ? 0 : Mathf.Abs(dB/Mathf.Abs(dB-dC));
                            var rCD = (dC*dD)>=0 ? 0 : Mathf.Abs(dC/Mathf.Abs(dC-dD));
                            var rDA = (dD*dA)>=0 ? 0 : Mathf.Abs(dD/Mathf.Abs(dD-dA));
                            //Debug.LogFormat("cut{4}: rAB={0},rBC={1},rCD={2},rDA={3}", rAB, rBC, rCD, rDA, cut);
                            if (rAB > 0) {
                                if (rCD > 0) {
                                    // we cut through AB and CD
                                    var f = Builder.SplitFaceABCD(face, rAB, rCD);
                                    if (cut == 3) {
                                        if (dA < 0) {
                                            result.Add(f[1]);
                                            thisCutFace = f[0];
                                        } else {
                                            result.Add(f[0]);
                                            thisCutFace = f[1];
                                        }
                                    } else {
                                        result.Add(f[0]);
                                        result.Add(f[1]);
                                    }
                                } else {
                                    result.Add(face);
                                }
                            } else if (rBC > 0) {
                                if (rDA > 0) {
                                    // we cut through BC and DA
                                    var f = Builder.SplitFaceBCDA(face, rBC, rDA);
                                    if (cut == 3) {
                                        if (dA < 0) {
                                            result.Add(f[0]);
                                            thisCutFace = f[1];
                                        } else {
                                            result.Add(f[1]);
                                            thisCutFace = f[0];
                                        }
                                    } else {
                                        result.Add(f[0]);
                                        result.Add(f[1]);
                                    }
                                } else {
                                    result.Add(face);
                                }
                            } else {
                                result.Add(face);
                            }
                            if (thisCutFace != null) {
                                if (previousCutFace != null) {
                                    var bridged = Builder.BridgeEdgeLoops(
                                        new List<Vector3> { previousCutFace.A, previousCutFace.B, previousCutFace.C, previousCutFace.D},
                                        new List<Vector3> { thisCutFace.A, thisCutFace.B, thisCutFace.C, thisCutFace.D},
                                        uvScale);
                                    if (material != null) {
                                        foreach (var b in bridged) {
                                            b.Material = material;
                                        }
                                    }
                                    unaffectedFaces.AddRange(bridged);
                                    previousCutFace = null;
                                } else {
                                    previousCutFace = thisCutFace;
                                }
                                thisCutFace.Tag(Builder.CUTOUT);
                            }
                        } else {
                            // that should never happen and is here just for debugging
                            //Debug.Log("all points are on the same side, no splitting of " + face);
                            result.Add(face);
                        }
                    } else {
                        //Debug.Log("face not affected by split: " + face);
                        //result.Add(face);
                        unaffectedFaces.Add(face);
                    }
                }
                affectedFaces.Clear();
                affectedFaces.AddRange(result);
                result.Clear();
            }
            // if previous cut face is still set we haven't closed the hole and the face is added back to the mesh
            if (previousCutFace != null) {
                previousCutFace.Tag(Builder.CUTOUT);
                affectedFaces.Add(previousCutFace);
            }
            Faces.Clear();
            Faces.AddRange(unaffectedFaces);
            Faces.AddRange(affectedFaces);
            return this;
        }

        public Face FindFirstFaceByTag(int tag) {
            return Builder.FindFirstFaceByTag(Faces, tag);
        }

        public BuildingObject CutFront(Rectangle dim, float uvScale) {
            return CutFront(dim, true, uvScale);
        }

        public BuildingObject CutFront(Rectangle dim, bool indent, float uvScale) {
            var result = new List<Face>();
            // project the 2D rect on this face (the normal needs to point to Vector3.back)
            // the z is not used so we don't care
            // check which faces are affected
            foreach (var face in Faces) {
                var n = new List<Face>();
                var cutoutFace = Builder.ProjectRectOnFrontFace(dim, 0);
                // is the cutout part of this face?
                if (cutoutFace.A.X > face.A.X && cutoutFace.A.Y >= face.A.Y && cutoutFace.C.X < face.C.X && cutoutFace.C.Y <= face.C.Y) {
                    // slice the face into 9 parts leaving the center piece at the size of the cutout
                    var nf = Builder.SliceFace(face, cutoutFace.A.X - face.A.X, 0);

                    var leftColumn = nf[0];
                    var nf2 = Builder.SliceFace(nf[1], cutoutFace.D.X - nf[1].A.X, 0);
                    var middleColumn = nf2[0];
                    var rightColumn = nf2[1];

                    /*                    
                    nf = Builder.SliceFace(leftColumn, 0, cutoutFace.a.y - leftColumn.a.y);
                    n.Add(nf[0]); // bottom left corner
                    nf2 = Builder.SliceFace(nf[1], 0, cutoutFace.b.y-nf[1].a.y);
                    n.Add(nf2[1]); // top left corner
                    n.Add(nf2[0]); // left middle
                    */
                    n.Add(leftColumn);

                    nf = Builder.SliceFace(middleColumn, 0, cutoutFace.A.Y - middleColumn.A.Y);
                    if (Mathf.Abs(nf[0].B.Y - nf[0].A.Y) > 1e-3f)
                        n.Add(nf[0]); // bottom center
                    nf2 = Builder.SliceFace(nf[1], 0, cutoutFace.B.Y - nf[1].A.Y);
                    //result.Add(nf2[0]); // center
                    nf2[0].Tag(Builder.CUTOUT);
                    if (indent)
                        n.AddRange(Builder.IndentFace(nf2[0], new Vector3(0, 0, 0.3f), uvScale));
                    if (Mathf.Abs(nf2[1].B.Y - nf2[1].A.Y) > 1e-3f)
                        n.Add(nf2[1]); // top center

                    /*
                    nf = Builder.SliceFace(rightColumn, 0, cutoutFace.a.y-rightColumn.a.y);
                    n.Add(nf[0]); // bottom right corner
                    nf2 = Builder.SliceFace(nf[1], 0, cutoutFace.b.y-nf[1].a.y);
                    n.Add(nf2[0]); // right middle
                    n.Add(nf2[1]); // top right corner
                    */
                    n.Add(rightColumn);
                    result.AddRange(n);
                } else {
                    //Debug.Log("cutout is not part of this face. cutout=" + cutoutFace + ", face=" + face);
                    result.Add(face);
                }
            }
            Faces = result;
            return this;
        }

        public void ClampToPlane(List<Vector3> front, List<Vector3> back, Vector3 plane, Vector3 normal) {
            for (var i = 0; i < front.Count; i++) {
                // if vertex is behind the plane (not on normal side) project it on the plane
                var dot = Vector3.Dot(front[i] - plane, normal);
                if (dot < 0) {
                    var v = front[i] - plane;
                    float dist = v.X*normal.X + v.Y*normal.Y + v.Z*normal.Z;
                    var projected = front[i] - dist*normal;
                    // collapse front and back vertices
                    MoveVertices(front[i], Builder.MatchingVertex.XYZ, projected);
                    MoveVertices(back[i], Builder.MatchingVertex.XYZ, projected);
                    front[i] = projected;
                    back[i] = projected;
                }
            }
        }
    }
}