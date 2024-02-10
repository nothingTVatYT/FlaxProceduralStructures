using System.Collections.Generic;
using System.Linq;
using FlaxEngine;
using static System.String;

namespace Game.ProceduralStructures {
    public class ProceduralHouse {

        public delegate void ExcludeFromNavmesh(Actor gameObject);
        public ExcludeFromNavmesh excludeFromNavmesh;
        SharedMeshLibrary meshLibrary;

        class QueuedMakeHole {
            public QueuedMakeHole(Vector3 origin, Vector3 direction, Vector3 up, float width, float height, Material material, float maxDistance = 0, float uvScale=0.01f) {
                this.origin = origin;
                this.direction = direction;
                this.up = up;
                this.width = width;
                this.height = height;
                this.material = material;
                this.maxDistance = maxDistance;
                this.uvScale = uvScale;
            }
            Vector3 origin;
            Vector3 direction;
            Vector3 up;
            float width;
            float height;
            Material material;
            float maxDistance;
            float uvScale;

            public void MakeHole(BuildingObject layer) {
                layer.MakeHole(origin, direction, up, width, height, material, maxDistance, uvScale);
                var hole = layer.FindFirstFaceByTag(Builder.CUTOUT);
                if (hole != null) {
                    layer.RemoveFace(hole);
                }
            }
        }

        public ProceduralHouse() {
            meshLibrary = null;
        }

        public ProceduralHouse(SharedMeshLibrary meshLibrary) {
            this.meshLibrary = meshLibrary;
        }

        /*
        public void BuildHouseWithInterior(HouseDefinition house, Actor target, ProceduralStructureCache cache) {
            var key = house.name + "-LOD0";
            if (cache.ContainsKey(key)) {
                cache.InstantiateGameObject(key, target, "LOD0");
                cache.InstantiateGameObject(house.name + "-" + Building.ADDED_INTERIOR, target, Building.ADDED_INTERIOR);
            } else {
                RebuildHouseWithInterior(house, target, 0);
                cache.AddPrefab(key, Building.GetChildByName(target, "LOD0"));
                var add0 = target.FindActor(Building.ADDED_INTERIOR);
                if (add0 != null) {
                    cache.AddPrefab(house.name + "-" + Building.ADDED_INTERIOR, add0);
                }
            }
            key = house.name + "-LOD1";
            if (cache.ContainsKey(key)) {
                cache.InstantiateGameObject(key, target, "LOD1");
            } else {
                RebuildHouseWithInterior(house, target, 1);
                cache.AddPrefab(key, Building.GetChildByName(target, "LOD1"));
            }
            SetupLOD(house, target);
        }
        */

        public void RebuildHouseWithInterior(HouseDefinition house, Actor target) {
            var lod0 = RebuildHouseWithInterior(house, target, 0);
            var lod1 = RebuildHouseWithInterior(house, target, 1);
            var allMaterials = lod0.GetMaterials().Union(lod1.GetMaterials());
            var lod0Meshes = lod0.GetNumberOfMaterials();
            var lod1Meshes = lod1.GetNumberOfMaterials();
            var ml = Join(',', allMaterials);
            Debug.Log($"Build models for each material in {ml}");
            Debug.Log($"Build {lod0Meshes} meshes for LOD 0");
            Debug.Log($"Build {lod1Meshes} meshes for LOD 1");
            foreach (var materialName in allMaterials)
            {
                var nodeName = "mat-" + MeshObject.CreateMaterialName(materialName);
                var materialNode = target.FindActor<Actor>(nodeName);
                if (materialNode == null)
                {
                    materialNode = new EmptyActor();
                    materialNode.Parent = target;
                    materialNode.Name = nodeName;
                }
                materialNode.LocalPosition = Vector3.Zero;
                materialNode.LocalOrientation = Quaternion.Identity;
                materialNode.LocalScale = Vector3.One;
                materialNode.StaticFlags = target.StaticFlags;
                var childModel = materialNode.GetOrAddChild<StaticModel>();
                var model = childModel.Model;
                if (model == null)
                    model = Content.CreateVirtualAsset<Model>();
                model.SetupLODs(new[] { 1, 1 });
                var mesh = model.LODs[0].Meshes[0];
                lod0.BuildMesh(materialName, mesh);
                mesh = model.LODs[1].Meshes[0];
                lod1.BuildMesh(materialName, mesh);
                childModel.Model = model;
                childModel.LocalScale = Vector3.One;
                var material = lod0.GetMaterialByName(materialName);
                if (material != null)
                    childModel.SetMaterial(0, material);
                // see https://github.com/FlaxEngine/FlaxEngine/issues/1687
                var meshCollider = materialNode.GetOrAddChild<MeshCollider>();
                var collisionData = meshCollider.CollisionData;
                if (collisionData == null)
                {
                    collisionData = Content.CreateVirtualAsset<CollisionData>();
                    meshCollider.CollisionData = collisionData;
                }
                collisionData.CookCollision(CollisionDataType.TriangleMesh, lod0.CachedVertices, lod0.CachedTriangles);
                materialNode.GetOrAddChild<MeshCollider>().CollisionData = collisionData;
                
            }
        }

        void SetupLOD(HouseDefinition house, Actor target) {
            /*
            LODGroup lodGroup = target.GetComponent<LODGroup>();
            if (lodGroup == null) {
                lodGroup = target.AddComponent<LODGroup>();
            }
            Actor go0 = Building.GetChildByName(target, "LOD0");
            MeshRenderer[] lod0Renderers = go0.GetComponentsInChildren<MeshRenderer>();
            Actor add0 = Building.GetChildByName(target, Building.ADDED_INTERIOR);
            if (add0 != null) {
                MeshRenderer[] addLod0Renderers = add0.GetComponentsInChildren<MeshRenderer>();
                if (addLod0Renderers != null && addLod0Renderers.Length > 0) {
                    MeshRenderer[] total0 = new MeshRenderer[lod0Renderers.Length + addLod0Renderers.Length];
                    System.Array.Copy(lod0Renderers, total0, lod0Renderers.Length);
                    System.Array.Copy(addLod0Renderers, 0, total0, lod0Renderers.Length, addLod0Renderers.Length);
                    lod0Renderers = total0;
                }
            }
            LOD lod0 = new LOD(0.5f, lod0Renderers);
            Actor go1 = Building.GetChildByName(target, "LOD1");
            LOD lod1 = new LOD(0.1f, go1.GetComponentsInChildren<MeshRenderer>());
            lodGroup.SetLODs(new LOD[] { lod0, lod1 });
            lodGroup.RecalculateBounds();
            MeshCollider[] colliders = go1.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider collider in colliders) {
                collider.enabled = false;
            }
            */
        }

        // the lod integer should be 0 to build the complete house and with higher levels we add less details
        // currently one 0 and 1 is used where with lod=1 we skip the interior objects including the inner walls sides
        public Building RebuildHouseWithInterior(HouseDefinition house, Actor target, int lod) {
            // this is the position of the resulting actor
            var center = new Vector3(0, house.heightOffset, 0);
            // width is the distance from the leftmost to the rightmost wall corners seen from the front i.e. on the x axis
            var width = house.width;
            // length is the distance from front to back wall i.e. on the z axis
            var length = house.length;
            var building = new Building();
            var lastLayerIsHollow = false;
            // this is used to close the top of the walls on the last layer when we build the roof
            float lastWallThickness = 0;

            var allLayers = new BuildingObject();
            var delayedBoring = new List<QueuedMakeHole>();

            // each layer describes a floor although multiple layers could be combined to a floor if you disable floor/ceiling creation
            foreach (var bs in house.layers) {
                var height = bs.height;
                var wallThickness = bs.wallThickness;
                lastWallThickness = wallThickness;
                var layer = new BuildingObject();
                layer.Material = bs.material;
                var a = center + new Vector3(-width/2, 0, -length/2);
                var b = center + new Vector3(-width/2, 0, length/2);
                var c = center + new Vector3(width/2, 0, length/2);
                var d = center + new Vector3(width/2, 0, -length/2);
                var a1 = a + new Vector3(bs.slopeX*height, height, bs.slopeZ*height);
                var b1 = b + new Vector3(bs.slopeX*height, height, -bs.slopeZ*height);
                var c1 = c + new Vector3(-bs.slopeX*height, height, -bs.slopeZ*height);
                var d1 = d + new Vector3(-bs.slopeX*height, height, bs.slopeZ*height);

                if (bs.hollow && lod==0) {
                    lastLayerIsHollow = true;
                    var ai = a + new Vector3(wallThickness, 0, wallThickness);
                    var bi = b + new Vector3(wallThickness, 0, -wallThickness);
                    var ci = c + new Vector3(-wallThickness, 0, -wallThickness);
                    var di = d + new Vector3(-wallThickness, 0, wallThickness);
                    // outer faces of walls
                    layer.ExtrudeEdges(new List<Vector3> {a, d, c, b, a}, Vector3.Up * height, bs.uvScale);
                    // inner faces
                    layer.ExtrudeEdges(new List<Vector3> {ai, bi, ci, di, ai}, Vector3.Up * height, bs.uvScale);
                    if (bs.addFloor) {
                        var floor = new Face(ai, bi, ci, di);
                        floor.SetUvFront(width * bs.uvScale, height * bs.uvScale);
                        layer.AddFace(floor);
                    }
                    if (bs.addCeiling) {
                        var ceiling = new Face(ai, bi, ci, di);
                        ceiling.MoveFaceBy(Vector3.Up * (height - wallThickness)).InvertNormals();
                        ceiling.SetUvFront(width * bs.uvScale, height * bs.uvScale);
                        layer.AddFace(ceiling);
                    }

                    if (bs.cutouts != null)
                    {
                        foreach (var co in bs.cutouts)
                        {
                            var origin = center + new Vector3(0, co.dimension.Y + co.dimension.Height / 2, 0);
                            var direction = Vector3.Backward;
                            switch (co.side)
                            {
                                case HouseDefinition.Side.Right:
                                    direction = Vector3.Right;
                                    break;
                                case HouseDefinition.Side.Left:
                                    direction = Vector3.Left;
                                    break;
                                case HouseDefinition.Side.Back:
                                    direction = Vector3.Forward;
                                    break;
                            }

                            var localRight = Vector3.Cross(direction, Vector3.Up);
                            origin += localRight * co.dimension.X;
                            layer.MakeHole(origin, direction, Vector3.Up, co.dimension.Width, co.dimension.Height, co.material, 0, co.uvScale);
                            if (co.prefab != null)
                            {
                                var interiorsObject = AddedInterior(target);
                                var objectInHole = PrefabManager.SpawnPrefab(co.prefab, interiorsObject);
                                //objectInHole.transform.localPosition = center + new Vector3(co.dimension.x, co.dimension.y, 0) + direction*length/2;
                                var distance = length / 2 + 0.1f;
                                if (co.side == HouseDefinition.Side.Left || co.side == HouseDefinition.Side.Right)
                                {
                                    distance = width / 2 - 0.2f;
                                }

                                objectInHole.LocalPosition = origin - new Vector3(0, co.dimension.Height / 2, 0) +
                                                             direction * distance;
                                objectInHole.LocalOrientation = RotationFromSide(co.side);
                                objectInHole.StaticFlags = interiorsObject.StaticFlags;
                            }
                        }
                    }
                } else {
                    lastLayerIsHollow = false;
                    layer.ExtrudeEdges(new List<Vector3> {a, d, c, b, a}, Vector3.Up * height, bs.uvScale);
                    if (bs.cutouts != null)
                        foreach (var co in bs.cutouts) {
                            //layer.CutFront(co.dimension, bs.uvScale);
                            var origin = center + new Vector3(0, co.dimension.Y + co.dimension.Height/2, 0);
                            var direction = Vector3.Backward;
                            switch(co.side) {
                                case HouseDefinition.Side.Right: direction = Vector3.Right; break;
                                case HouseDefinition.Side.Left: direction = Vector3.Left; break;
                                case HouseDefinition.Side.Back: direction = Vector3.Forward; break;
                            }
                            var localRight = Vector3.Cross(direction, Vector3.Up);
                            origin += localRight * co.dimension.X;
                            layer.MakeHole(origin, direction, Vector3.Up, co.dimension.Width, co.dimension.Height, co.material, 0, co.uvScale);
                            var opening = layer.FindFirstFaceByTag(Builder.CUTOUT);
                            if (opening != null) {
                                layer.IndentFace(opening, Vector3.Forward * 0.1f, co.uvScale);
                                opening.Material = co.material;
                                opening.SetUvForSize(co.uvScale);
                                opening.UnTag(Builder.CUTOUT);
                            } else {
                                Debug.Log("no opening found for " + co.name);
                            }
                        }
                }

                // add stairs
                if (bs.stairs != null) {
                    foreach (var stairs in bs.stairs) {
                        // skip inside stairs for LOD >0
                        if (lod > 0 && stairs.inside) {
                            continue;
                        }
                        var stairsPosition = center;
                        var stairsRotation = Quaternion.Identity;
                        switch (stairs.side)
                        {
                            case HouseDefinition.Side.Front:
                                stairsPosition = center - new Vector3(0, 0, -length/2);
                                break;
                            case HouseDefinition.Side.Back:
                                stairsPosition = center - new Vector3(0, height, -length/2);
                                stairsRotation = Quaternion.RotationAxis(Vector3.Up, Mathf.Pi);
                                break;
                            case HouseDefinition.Side.Right:
                                stairsPosition = center - new Vector3(0, height, -width/2);
                                stairsRotation = Quaternion.RotationAxis(Vector3.Up, -Mathf.Pi/2);
                                break;
                            case HouseDefinition.Side.Left:
                                stairsPosition = center - new Vector3(0, height, -width/2);
                                stairsRotation = Quaternion.RotationAxis(Vector3.Up, Mathf.Pi/2);
                                break;
                        }
                        var floor = Face.CreateXzPlane(stairs.baseWidth,stairs.baseLength);
                        floor.SetUvFront(stairs.baseWidth * stairs.uvScale, stairs.baseLength * stairs.uvScale);
                        var stairsBlock = new BuildingObject();
                        stairsBlock.Material = stairs.material;
                        stairsBlock.Rotation = stairsRotation;
                        stairsBlock.AddFace(floor);
                        var dn = Vector3.Down * stairs.stepHeight;
                        var ou = Vector3.Backward * Mathf.Cos(stairs.descentAngle*Mathf.DegreesToRadians) * stairs.stepDepth;
                        var si = Vector3.Right * Mathf.Sin(stairs.descentAngle*Mathf.DegreesToRadians) * stairs.stepDepth;
                        float currentHeight = 0;
                        var stepC = floor.C;
                        var stepD = floor.D;
                        var stepA = floor.A;
                        var stepB = floor.B;
                        //Debug.Log("stairs ou=" + ou + ", si=" + si);
                        while (currentHeight < stairs.totalHeight) {
                            // extrude down
                            stairsBlock.ExtrudeEdges(new List<Vector3> {stepC, stepD, stepA, stepB}, dn, stairs.uvScale);
                            stepC += dn;
                            stepD += dn;
                            stepA += dn;
                            stepB += dn;
                            currentHeight += stairs.stepHeight;
                            if (currentHeight >= stairs.totalHeight) break;
                            // extrude step
                            if (ou.LengthSquared > 0) {
                                stairsBlock.ExtrudeEdges(new List<Vector3> {stepD, stepA}, ou, stairs.uvScale);
                                stepD += ou;
                                stepA += ou;
                            }
                            if (si.LengthSquared > 0) {
                                if (stairs.descentAngle > 0) {
                                    stairsBlock.ExtrudeEdges(new List<Vector3> {stepC, stepD}, si, stairs.uvScale);
                                    stepC += si;
                                    stepD += si;
                                } else {
                                    stairsBlock.ExtrudeEdges(new List<Vector3> {stepA, stepB}, si, stairs.uvScale);
                                    stepA += si;
                                    stepB += si;
                                }
                            }
                        }

                        stairsBlock.SetUVBoxProjection(stairs.uvScale);

                        var zOffset = length/2;
                        if (stairs.side == HouseDefinition.Side.Left || stairs.side == HouseDefinition.Side.Right) {
                            zOffset = width/2;
                        }
                        if (stairs.inside) {
                            zOffset -= stairs.baseLength + wallThickness;
                            stairsBlock.RotateFaces(Quaternion.RotationAxis(Vector3.Up, Mathf.Pi));
                        }
                        stairsBlock.Position = center + new Vector3(stairs.offset, stairs.baseHeight, -stairs.baseLength/2 - zOffset);
                        if (stairs.inside) {
                            var stairsBounds = stairsBlock.CalculateGlobalBounds();
                            var holePosition = stairsBounds.Center;
                            var maxDistance = stairsBounds.Size.Y/2 + 200;
                            DebugDraw.DrawBox(stairsBounds, Color.Aquamarine);
                            delayedBoring.Add(new QueuedMakeHole(holePosition, Vector3.Up, Vector3.Backward, stairsBounds.Size.X-10f, stairsBounds.Size.Z-10f, bs.material, maxDistance));
                        }
                        building.AddObject(stairsBlock);
                    }
                }
                allLayers.AddObject(layer);
                center.Y += height;
            }

            // make a hole above stairs of previous layers
            foreach (var boringRequest in delayedBoring) {
                boringRequest.MakeHole(allLayers);
            }
            delayedBoring.Clear();

            building.AddObject(allLayers);

            if (house.roofHeight > 0) {
                var roofLayer = new BuildingObject();
                roofLayer.Material = house.materialGable;
                var a = center + new Vector3(-width/2, 0, -length/2);
                var b = center + new Vector3(-width/2, 0, length/2);
                var c = center + new Vector3(width/2, 0, length/2);
                var d = center + new Vector3(width/2, 0, -length/2);

                var uvScale = house.uvScaleGable;
                var e1 = center + new Vector3(0, house.roofHeight, -length/2);
                var e2 = center + new Vector3(0, house.roofHeight, length/2);
                var frontFace = new Face(a, e1, d);
                frontFace.SetUvFront(width * uvScale, house.roofHeight * uvScale);
                roofLayer.AddFace(frontFace);
                var backface = new Face(c, e2, b);
                backface.SetUvFront(width * uvScale, house.roofHeight * uvScale);
                roofLayer.AddFace(backface);
                
                if (lastLayerIsHollow) {
                    var innerFrontFace = frontFace.DeepCopy().MoveFaceBy(Vector3.Forward * lastWallThickness).InvertNormals();
                    var innerBackFace = backface.DeepCopy().MoveFaceBy(Vector3.Backward * lastWallThickness).InvertNormals();
                    roofLayer.AddFace(innerFrontFace);
                    roofLayer.AddFace(innerBackFace);
                    var innerLeftRoof = new Face(b, a, e1, e2);
                    var h = Mathf.Sqrt(width/2 * width/2 + house.roofHeight * house.roofHeight);
                    innerLeftRoof.SetUvFront(length * uvScale, h * uvScale);
                    var innerRightRoof = new Face(d, c, e2, e1);
                    innerRightRoof.SetUvFront(length * uvScale, h * uvScale);
                    roofLayer.AddFace(innerLeftRoof);
                    roofLayer.AddFace(innerRightRoof);
                    var innerLeftTopWall = new Face(a,b, b+Vector3.Right * lastWallThickness, a+Vector3.Right * lastWallThickness);
                    innerLeftTopWall.SetUvFront(lastWallThickness, length);
                    var innerRightTopWall = new Face(d+Vector3.Left * lastWallThickness, c+Vector3.Left * lastWallThickness, c, d);
                    innerRightTopWall.SetUvFront(lastWallThickness, length);
                    roofLayer.AddFace(innerLeftTopWall);
                    roofLayer.AddFace(innerRightTopWall);
                }

                var extZ = new Vector3(0, 0, house.roofExtendZ);
                if (house.roofExtendZ > 0) {
                    var rightBackExtend = new Face(e2, c, c + extZ, e2 + extZ);
                    rightBackExtend.SetUvFront(house.roofExtendZ * uvScale, width/2 * uvScale);
                    roofLayer.AddFace(rightBackExtend);
                    var rightFrontExtend = new Face(e1 - extZ, d - extZ, d, e1);
                    rightFrontExtend.SetUvFront(house.roofExtendZ * uvScale, width/2 * uvScale);
                    roofLayer.AddFace(rightFrontExtend);
                    var leftBackExtend = new Face(b, e2, e2 + extZ, b + extZ);
                    leftBackExtend.SetUvFront(house.roofExtendZ * uvScale, width/2 * uvScale);
                    roofLayer.AddFace(leftBackExtend);
                    var leftFrontExtend = new Face(a - extZ, e1 - extZ, e1, a);
                    leftFrontExtend.SetUvFront(house.roofExtendZ * uvScale, width/2 * uvScale);
                    roofLayer.AddFace(leftFrontExtend);
                    e2 += extZ;
                    e1 -= extZ;
                }
                var ar = a - extZ;
                var br = b + extZ;
                var cr = c + extZ;
                var dr = d - extZ;
                if (house.roofExtendX > 0) {
                    var m = -house.roofHeight / (width/2);
                    var extX = new Vector3(house.roofExtendX, house.roofExtendX * m, 0);
                    var rightExtend = new Face(dr, dr+extX, cr+extX, cr);
                    rightExtend.SetUvFront(length * uvScale, house.roofExtendX * uvScale);
                    roofLayer.AddFace(rightExtend);
                    dr += extX;
                    cr += extX;
                    extX = new Vector3(-house.roofExtendX, house.roofExtendX * m, 0);
                    var leftExtend = new Face(br, br+extX, ar+extX, ar);
                    leftExtend.SetUvFront(length * uvScale, house.roofExtendX * uvScale);
                    roofLayer.AddFace(leftExtend);
                    br += extX;
                    ar += extX;
                }

                var roofThickness = new Vector3(0, house.roofThickness, 0);
                var roofEdges = new List<Vector3> {ar, e1, dr, cr, e2, br, ar};
                roofLayer.ExtrudeEdges(roofEdges, roofThickness, uvScale);

                ar += roofThickness;
                br += roofThickness;
                cr += roofThickness;
                dr += roofThickness;
                e1 += roofThickness;
                e2 += roofThickness;

                uvScale = house.uvScaleRoof;
                var leftRoof = new Face(br, e2, e1, ar);
                var halfSlope = Mathf.Sqrt(width/2 * width/2 + house.roofHeight * house.roofHeight);
                leftRoof.SetUvFront((length + 2 * house.roofExtendZ) * uvScale, halfSlope * uvScale);
                leftRoof.Material = house.materialRoof;
                building.AddFace(leftRoof);
                var rightRoof = new Face(dr, e1, e2, cr);
                rightRoof.SetUvFront((length + 2 * house.roofExtendZ) * uvScale, halfSlope * uvScale);
                rightRoof.Material = house.materialRoof;
                building.AddFace(rightRoof);
                building.AddObject(roofLayer);
                building.ClearNavmeshStaticOnMaterial(house.materialRoof == null ? "" : house.materialRoof.Path);
            }

            building.excludeFromNavmesh += HouseExcludeFromNavmesh;
            building.Prepare();
            return building;
        }

        public void HouseExcludeFromNavmesh(Actor gameObject) {
            if (excludeFromNavmesh != null)
                excludeFromNavmesh(gameObject);
        }

        Quaternion RotationFromSide(HouseDefinition.Side side) {
            switch (side) {
                case HouseDefinition.Side.Front: return Quaternion.RotationAxis(Vector3.Up, Mathf.Pi);
                case HouseDefinition.Side.Right: return Quaternion.RotationAxis(Vector3.Up, -Mathf.Pi/2);
                case HouseDefinition.Side.Left: return Quaternion.RotationAxis(Vector3.Up, Mathf.Pi/2);
            }
            return Quaternion.Identity;
        }

        Actor AddedInterior(Actor target) {
            var added = Building.GetChildByName(target, Building.ADDED_INTERIOR);
            if (added == null) {
                added = new EmptyActor();
                added.Parent = target;
                added.LocalPosition = Vector3.Zero;
                added.LocalOrientation = Quaternion.Identity;
                added.LocalScale = Vector3.One;
                added.StaticFlags = target.StaticFlags;
                added.Name = Building.ADDED_INTERIOR;
            }
            return added;
        }
    }
}