using FlaxEngine;
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.Editors;

namespace ProceduralStructures;

[CustomEditor(typeof(HullBuilder))]
public class HullBuilderEditor : GenericEditor
{
    public override void Initialize(LayoutElementsContainer layout)
    {
        var hull = Values[0] as HullBuilder;
        base.Initialize(layout);
        var button = layout.Button("Rebuild");
        button.Button.Clicked += () => { hull?.Rebuild(); };
        // ProceduralStructures.EditorUtilities.CreateSecondaryUV(hull.gameObject.GetComponentsInChildren<MeshFilter>());
    }
}