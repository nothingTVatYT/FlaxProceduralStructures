#if FLAX_EDITOR
using FlaxEditor.CustomEditors;
using FlaxEngine;
using FlaxEditor.CustomEditors.Editors;

namespace ProceduralStructures;

[CustomEditor(typeof(HouseBuilder))]
public class HouseBuilderEditor : GenericEditor {

    private HouseBuilder _houseBuilder;

    public override void Initialize(LayoutElementsContainer layout)
    {
        layout.Label("House Builder Editor", TextAlignment.Center);
        var group = layout.Group("House Definition");

        base.Initialize(group);

        layout.Space(20);
        var button = layout.Button("Rebuild with interior");

        // Use Values[] to access the script or value being edited.
        // It is an array, because custom editors can edit multiple selected scripts simultaneously.
        button.Button.Clicked += () => RebuildWithInterior(Values[0] as HouseBuilder);
    }

    private void RebuildWithInterior(HouseBuilder houseBuilder)
    {
        var p = new ProceduralHouse();
        p.excludeFromNavmesh += ExcludeFromNavmesh;
        p.RebuildHouseWithInterior(houseBuilder.houseDefinitionAsset.Instance as HouseDefinition, houseBuilder.Actor);
    }
    
    /*
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        houseBuilder = (HouseBuilder)target;
        if (GUILayout.Button("Rebuild with interior")) {
            Undo.RegisterFullObjectHierarchyUndo(houseBuilder.gameObject, "Rebuild with interior");
            ProceduralHouse p = new ProceduralHouse();
            p.excludeFromNavmesh += ExcludeFromNavmesh;
            p.RebuildHouseWithInterior(houseBuilder.houseDefinition, houseBuilder.gameObject);
            EditorUtilities.CreateSecondaryUV(houseBuilder.gameObject.GetComponentsInChildren<MeshFilter>());
        }
        if (GUILayout.Button("Remove Meshes")) {
            Undo.RegisterFullObjectHierarchyUndo(houseBuilder.gameObject, "Remove meshes");
            Building.ClearMeshes(houseBuilder.gameObject);
        }
    } */

    public void ExcludeFromNavmesh(Actor gameObject) {
        //StaticEditorFlags flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        //GameObjectUtility.SetStaticEditorFlags(gameObject, flags);
    }
    
}
#endif
