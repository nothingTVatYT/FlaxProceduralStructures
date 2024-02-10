using FlaxEditor.CustomEditors;
using FlaxEngine;
using FlaxEditor.CustomEditors.Editors;

namespace ProceduralStructures.Editor {
    [CustomEditor(typeof(CaveBuilderComponent))]
    public class CaveBuilderEditor : GenericEditor {

        public override void Initialize(LayoutElementsContainer layout) {
            layout.Label("Cave Builder Editor", TextAlignment.Center);
            var group = layout.Group("Cave Definition");

            base.Initialize(group);
            //layout.Space(20);
            var button1 = layout.Button("Update");
            button1.Button.Clicked += () => { UpdateMesh(Values[0] as CaveBuilderComponent); };
            var button2 = layout.Button("Update Waypoints");
            button2.Button.Clicked += () => { (Values[0] as CaveBuilderComponent)?.UpdateWayPoints(); };
        }

        private void UpdateMesh(CaveBuilderComponent c)
        {
            if (c.generatedMeshParent == null)
            {
                var actor = new EmptyActor
                {
                    Name = "generatedMesh",
                    Parent = c.Actor,
                    LocalPosition = Vector3.Zero,
                    LocalOrientation = Quaternion.Identity,
                    LocalScale = Vector3.One,
                    StaticFlags = c.Actor.StaticFlags
                };
                c.generatedMeshParent = actor;
            }
            c.UpdateWayPoints();
            var ps = new ProceduralStructure();
            ps.RebuildCave(c.Definition, c.generatedMeshParent);
        }
    }
}