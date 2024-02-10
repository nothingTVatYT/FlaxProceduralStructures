using System;
using System.Collections.Generic;
using FlaxEngine;
using Game.ProceduralStructures;
using ProceduralStructures;

public class HullBuilder : Script {

    [Serializable]
    public class MeshConnector {
        public enum Side { Beginning, End }
        public CaveBuilderComponent connectedCave;
        public int tunnelIndex = 0;
        public Side side = Side.Beginning;
        public bool decimateVertices = false;
        public int maxVertices = 6;
    }

    public Actor hullRoot;
    public Actor toEnclose;
    public bool ignoreInactive = true;
    public bool flipNormals = true;
    public Material material;
    public float uvScale = 1;
    public MeshObject.ShadingType shading = MeshObject.ShadingType.Flat;
    public bool randomizeVertices = false;
    public Vector3 randomDisplacement;
    public bool addConnector = false;
    public MeshConnector connection;

    public void Rebuild() {
        var body = new ConvexHull
        {
            UvScale = uvScale,
            Transform = Transform,
            // set this early for debugging
            TargetActor = hullRoot,
            Material = material
        };
        Actor wrappedObject = toEnclose;
        if (wrappedObject == null) {
            wrappedObject = Actor;
        }
        for (int i = 0; i < wrappedObject.ChildrenCount; i++) {
            Actor tf = wrappedObject.GetChild(i);
            HouseBuilder[] houseBuilders = tf.GetScripts<HouseBuilder>();
            BoxCollider[] boxColliders = tf.GetChildren<BoxCollider>();
            if (houseBuilders != null && houseBuilders.Length > 0) {
                foreach (HouseBuilder houseBuilder in houseBuilders) {
                    if (!ignoreInactive || houseBuilder.Actor.IsActiveInHierarchy) {
                        foreach (var v in GetCorners(houseBuilder.CalculateCenter(), houseBuilder.CalculateSize())) {
                            body.AddPoint(hullRoot.Transform.WorldToLocal(houseBuilder.Transform.TransformPoint(v)));
                        }
                    }
                }
            } else if (boxColliders != null && boxColliders.Length > 0) {
                foreach (var boxCollider in boxColliders) {
                    if (!ignoreInactive || boxCollider.IsActiveInHierarchy) {
                        foreach (var v in boxCollider.Box.GetCorners()) {
                            body.AddPoint(hullRoot.Transform.WorldToLocal(v));
                        }
                    }
                }
            } else {
                body.AddPoint(hullRoot.Transform.WorldToLocal(tf.Position));
            }
        }

        MeshObject connectedObject = null;
        if (addConnector && connection != null && connection.connectedCave != null) {
            float side = connection.side == MeshConnector.Side.Beginning ? 0 : 1;
            connectedObject = connection.connectedCave.Definition.GetConnection(connection.tunnelIndex, side);
            if (connection.decimateVertices) {
                connectedObject.Decimate(connection.maxVertices);
            }
            connectedObject.TargetActor = hullRoot;
            Vector3 displacement = 0.1f * (Actor.Position - connection.connectedCave.Actor.Position);
            body.AddPoint(connectedObject.GetCenter());
            List<Vector3> connectorPoints = connectedObject.PointList();
            Vector3 connectorCenter = Builder.FindCentroid(connectorPoints);
            //body.AddPoint(hullRoot.Transform.WorldToLocal(connectedObject.WorldPosition(connectorCenter) /*+ displacement*0.8f*/));
            // foreach (Vector3 v in connectedObject.PointList()) {
            //     body.AddPoint(body.LocalPosition(connectedObject.WorldPosition(v) + displacement));
            // }
        }

        body.CalculateHull();

        if (randomizeVertices) {
            body.RandomizeVertices(randomDisplacement);
        }

        if (connectedObject != null) {
            //body.AddConnector(connectedObject);
        }

        if (flipNormals) {
            body.FlipNormals();
        }
        body.Shading = shading;
        body.SetUVBoxProjection(uvScale);
        body.Build(hullRoot, material);
    }

    public IEnumerable<Vector3> GetCorners(Vector3 center, Vector3 size) {
        float dx = size.X/2;
        float dy = size.Y/2;
        float dz = size.Z/2;
        yield return center + new Vector3(dx, dy, dz);
        yield return center + new Vector3(-dx, dy, dz);
        yield return center + new Vector3(dx, -dy, dz);
        yield return center + new Vector3(-dx, -dy, dz);
        yield return center + new Vector3(dx, dy, -dz);
        yield return center + new Vector3(-dx, dy, -dz);
        yield return center + new Vector3(dx, -dy, -dz);
        yield return center + new Vector3(-dx, -dy, -dz);
    }

}
