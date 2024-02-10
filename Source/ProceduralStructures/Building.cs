using System.Collections.Generic;
using System.Linq;
using FlaxEngine;
using Object = FlaxEngine.Object;

namespace Game.ProceduralStructures {
    public class Building
    {
        public delegate void ExcludeFromNavmesh(Actor gameObject);
        public ExcludeFromNavmesh excludeFromNavmesh;

        private readonly List<Face> _faces = new();
        private readonly Dictionary<string, List<Face>> _facesByMaterial = new();
        public const string ADDED_INTERIOR = "generatedInterior";
        public static readonly List<string> NamesOfGeneratedObjects = new() {"LOD0", "LOD1", "LOD2", ADDED_INTERIOR};
        private readonly List<string> _nonNavmeshStaticMaterials = new();
        public uint[] CachedTriangles { get; private set; }

        public Float3[] CachedVertices { get; private set; }

        public List<Face> GetFacesByMaterial(Material material)
        {
            var materialName = material == null ? "" : material.Path;
            if (_facesByMaterial.TryGetValue(materialName, out var byMaterial)) {
                return byMaterial;
            }
            var faces = new List<Face>();
            _facesByMaterial[materialName] = faces;
            return faces;
        }

        private void GroupFacesByMaterial() {
            _facesByMaterial.Clear();
            foreach (var face in _faces) {
                GetFacesByMaterial(face.Material).Add(face);
            }
        }

        public void ClearNavmeshStaticOnMaterial(string material) {
            _nonNavmeshStaticMaterials.Add(material);
        }

        public void AddFace(Face face) {
            _faces.Add(face);
        }

        public void AddObject(BuildingObject child) {
            child.ApplyTransform().ApplyDefaultMaterial();
            _faces.AddRange(child.Faces);
        }

        /*
        public void Build(Actor target, int lod) {
            var lodTarget = GetChildByName(target, "LOD" + lod);
            if (lodTarget == null) {
                lodTarget = new EmptyActor();
                lodTarget.Name = "LOD" + lod;
                lodTarget.Parent = target;
                lodTarget.LocalPosition = Vector3.Zero;
                lodTarget.LocalOrientation = Quaternion.Identity;
                lodTarget.LocalScale = Vector3.One;
                lodTarget.StaticFlags = target.StaticFlags;
            }
            Build(lodTarget);
        }
        */

        public void Prepare()
        {
            GroupFacesByMaterial();
        }

        public IEnumerable<string> GetMaterials()
        {
            return _facesByMaterial.Keys;
        }

        public int GetNumberOfMaterials()
        {
            return _facesByMaterial.Count;
        }
        
        public void Build(Actor target) {
            ClearMeshes(target);
            GroupFacesByMaterial();
            foreach (var keyValue in _facesByMaterial) {
                Material material = null;
                if (keyValue.Value.Count > 0)
                {
                    material = keyValue.Value[0].Material;
                }
                //mesh.Name = "Generated Mesh (" + keyValue.Key.name + ")";
                AddMesh(target, out var mesh, material);
                BuildMesh(keyValue.Value, mesh);
            }
        }

        public void BuildMesh(string materialName, Mesh mesh)
        {
            BuildMesh(_facesByMaterial[materialName], mesh);
        }

        public Material GetMaterialByName(string materialName)
        {
            var faces = _facesByMaterial[materialName];
            if (faces == null || faces.Count == 0)
                return null;
            return faces.First().Material;
        }
        
        public void AddMesh(Actor target, out Mesh mesh, Material material)
        {
            var materialName = MeshObject.CreateMaterialName(material);
            var childByMaterial = target.FindActor("mat-" + materialName);
            if (childByMaterial == null) {
                childByMaterial = new EmptyActor();
                childByMaterial.Name = "mat-" + materialName;
                childByMaterial.Parent = target;
                childByMaterial.LocalPosition = Vector3.Zero;
                childByMaterial.LocalOrientation = Quaternion.Identity;
                childByMaterial.LocalScale = Vector3.One;
                childByMaterial.StaticFlags = target.StaticFlags;
                if (_nonNavmeshStaticMaterials.Contains(materialName) && excludeFromNavmesh != null) {
                    excludeFromNavmesh(childByMaterial);
                }
            }

            var model = Content.CreateVirtualAsset<Model>();
            model.SetupLODs(new[] { 1 });
            mesh = model.LODs[0].Meshes[0];
            var childModel = childByMaterial.GetOrAddChild<StaticModel>();
            childModel.Model = model;
            childModel.LocalScale = Vector3.One;
            if (material != null)
                childModel.SetMaterial(0, material.CreateVirtualInstance());
            // see https://github.com/FlaxEngine/FlaxEngine/issues/1687
            var collisionData = Content.CreateVirtualAsset<CollisionData>();
            collisionData.CookCollision(CollisionDataType.TriangleMesh, model);
            childModel.GetOrAddChild<MeshCollider>().CollisionData = collisionData;
        }

        public static void ClearMeshes(Actor target) {
            for (var i = target.ChildrenCount-1; i>=0; i--) {
                var go = target.GetChild(i);
                if (NamesOfGeneratedObjects.Contains(go.Name)) {
                    Object.Destroy(go);
                }
            }
        }

        public static Actor GetChildByName(Actor parent, string name)
        {
            return parent.FindActor(name);
        }
        
        protected Mesh BuildMesh(List<Face> faces, Mesh mesh) {
            int triangles;
            var verticesInFaces = CountVertices(faces, out triangles);
            var vertices = new Float3[verticesInFaces];
            var uv = new Float2[verticesInFaces];
            var tris = new uint[6 * (faces.Count - triangles) + 3 * triangles];
            ushort index = 0;
            var trisIndex = 0;
            foreach (var face in faces) {
                vertices[index] = face.A;
                uv[index] = face.UvA;
                index++;
                vertices[index] = face.B;
                uv[index] = face.UvB;
                index++;
                vertices[index] = face.C;
                uv[index] = face.UvC;
                index++;
                if (!face.IsTriangle) {
                    vertices[index] = face.D;
                    uv[index] = face.UvD;
                    index++;
                    tris[trisIndex++] = (ushort)(index - 4); // A
                    tris[trisIndex++] = (ushort)(index - 3); // B
                    tris[trisIndex++] = (ushort)(index - 2); // C
                    tris[trisIndex++] = (ushort)(index - 4); // A
                    tris[trisIndex++] = (ushort)(index - 2); // C
                    tris[trisIndex++] = (ushort)(index - 1); // D
                } else {
                    tris[trisIndex++] = (ushort)(index - 3); // A
                    tris[trisIndex++] = (ushort)(index - 2); // B
                    tris[trisIndex++] = (ushort)(index - 1); // C
                }
            }
            mesh.UpdateMesh(vertices, tris, null, null, uv);
            CachedVertices = vertices;
            CachedTriangles = tris;
            return mesh;
        }

        protected static int CountVertices(List<Face> faces, out int triangles) {
            var vertices = 0;
            triangles = 0;
            foreach (var face in faces) {
                if (face.IsTriangle) {
                    vertices += 3;
                    triangles++;
                } else {
                    vertices += 4;
                }
            }
            return vertices;
        }

    }
}