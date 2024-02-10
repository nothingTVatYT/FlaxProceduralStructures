#if FLAX_EDITOR
using System;
using System.Collections.Generic;
using FlaxEngine;
using Object = FlaxEngine.Object;

namespace Game.ProceduralStructures;

public class ProceduralStructureCache {
    private readonly Dictionary<string, Guid> _prefabs = new();
    public bool ContainsKey(string key) {
        return _prefabs.ContainsKey(key);
    }

    public Actor InstantiateGameObject(string key, Actor parent, string name)
    {
        if (!ContainsKey(key))
            return null;
        var prefab = _prefabs[key];
        var instance = PrefabManager.SpawnPrefab(Object.Find<Prefab>(ref prefab));
        instance.Name = name;
        instance.Parent = parent;
        instance.LocalPosition = Vector3.Zero;
        instance.LocalOrientation = Quaternion.Identity;
        instance.LocalScale = Vector3.One;
        return instance;
    }

    public void AddPrefab(string key, Actor actor) {
        if (key == null)
            throw new ArgumentNullException(nameof(key),"must not be null");
        if (actor == null)
            throw new ArgumentNullException(nameof(actor), "must not be null");
        PrefabManager.CreatePrefab(actor, "_ProceduralStructures/" + key + ".prefab", true);
        _prefabs[key] = actor.PrefabID;
    }
}
#endif
