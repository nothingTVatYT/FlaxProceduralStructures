using System;
using FlaxEngine;

namespace ProceduralStructures
{
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
                Category = "Other",
                Author = "nothingTVatYT",
                AuthorUrl = null,
                HomepageUrl = null,
                RepositoryUrl = "https://github.com/FlaxEngine/ProceduralStructures",
                Description = "This is an example plugin project.",
                Version = new Version(),
                IsAlpha = false,
                IsBeta = false,
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
}
