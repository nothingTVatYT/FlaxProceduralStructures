using Flax.Build;

public class ProceduralStructuresTarget : GameProjectTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for game
        Modules.Add("ProceduralStructures");
    }
}
