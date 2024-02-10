using System;
using FlaxEngine;

namespace ProceduralStructures;

/// <summary>
/// The sample game plugin.
/// </summary>
/// <seealso cref="FlaxEngine.GamePlugin" />
public class ProceduralStructures : GamePlugin
{
    /// <inheritdoc />
    public ProceduralStructures()
    {
        _description = new PluginDescription
        {
            Name = "ProceduralStructures",
            Category = "Content",
            Author = "nothingTVatYT",
            AuthorUrl = null,
            HomepageUrl = null,
            RepositoryUrl = "https://github.com/FlaxEngine/FlaxProceduralStructures",
            Description = "Generate structures like cities and caves procedurally.",
            Version = new Version(0, 1),
            IsAlpha = false,
            IsBeta = true,
        };
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        Debug.Log("Hello from plugin code!");
    }

    /// <inheritdoc />
    public override void Deinitialize()
    {
        // Use it to cleanup data

        base.Deinitialize();
    }
}