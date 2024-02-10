using System.Collections.Generic;
using FlaxEngine;

namespace ProceduralStructures;

public class CaveBuilderComponent : Script
{
    public Actor generatedMeshParent;

    public bool UseTransforms;
    public Actor WayPointTransforms;
    public CaveDefinition Definition;

    public override void OnEnable()
    {
        base.OnEnable();
        UpdateWayPoints();
    }

    public void UpdateWayPoints()
    {
        var lists = new List<WayPointList>();
        for (var i = 0; i < WayPointTransforms.ChildrenCount; i++)
        {
            var points = new List<WayPoint>();
            var tunnel = WayPointTransforms.GetChild(i);
            for (var j = 0; j < tunnel.ChildrenCount; j++)
            {
                var tf = tunnel.GetChild(j);
                var wp = new WayPoint(tf.Position, tf.LocalScale.X, tf.LocalScale.Y)
                {
                    Name = tf.Name
                };
                points.Add(wp);
            }

            lists.Add(new WayPointList(tunnel.Name, points));
        }

        Definition.WayPointLists = lists;
    }

    public override void OnDebugDrawSelected()
    {
        if (Definition.IsValid())
        {
            foreach (var list in Definition.WayPointLists)
            {
                var a = list.WayPoints[0].Position;
                var n = 0;
                foreach (var v in Definition.GetVertices(list))
                {
                    if (v != null)
                    {
                        n++;
                        DebugDraw.DrawLine(a, v, Color.Yellow);
                        DebugDraw.DrawSphere(new BoundingSphere(v, 10), Color.Yellow);
                        a = v;
                    }
                }
            }
        }
    }
}