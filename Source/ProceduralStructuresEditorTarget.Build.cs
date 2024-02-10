using Flax.Build;

public class ProceduralStructuresEditorTarget : GameProjectEditorTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for editor
        Modules.Add("ProceduralStructures");
        Modules.Add("ProceduralStructuresEditor");
    }
}
