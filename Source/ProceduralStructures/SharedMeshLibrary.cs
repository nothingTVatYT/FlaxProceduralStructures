using System.Collections.Generic;
using FlaxEngine;

namespace Game.ProceduralStructures {
    public class SharedMeshLibrary {
        Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        public void RegisterMesh(string key, Mesh mesh) {
            meshes.Add(key, mesh);
        }

        public bool HasMesh(string key) {
            return meshes.ContainsKey(key) && meshes[key] != null;
        }

        public Mesh GetMesh(string key) {
            return meshes[key];
        }
    }
}
