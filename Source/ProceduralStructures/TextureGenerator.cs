using System;
using System.Collections.Generic;
using System.Text;
using FlaxEngine;
using FlaxEngine.Utilities;

namespace Game;

/// <summary>
/// TextureGenerator Script.
/// </summary>
public class TextureGenerator : Script
{
    private Texture _tempTexture;
    private MaterialInstance _tempMaterialInstance;

    public Material Material;
    public Model Model;
    [Header("Perlin Noise Parameter")]
    public Float2 Scale = new(1, 1);
    
    /// <inheritdoc/>
    public override void OnStart()
    {
        if (Model.WaitForLoaded())
            return;
        GenerateVirtualTexture();
    }
    
    /// <inheritdoc/>
    public override void OnEnable()
    {
        // Here you can add code that needs to be called when script is enabled (eg. register for events)
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Here you can add code that needs to be called when script is disabled (eg. unregister from events)
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Here you can add code that needs to be called every frame
    }

    private unsafe void GenerateVirtualTexture()
    {
        var texture = Content.CreateVirtualAsset<Texture>();
        _tempTexture = texture;
        var initData = new TextureBase.InitData();
        initData.Width = 1024;
        initData.Height = 1024;
        initData.ArraySize = 1;
        initData.Format = PixelFormat.R8G8B8A8_UNorm;
        var data = new byte[initData.Width * initData.Height * PixelFormatExtensions.SizeInBytes(initData.Format)];
        var point = new Float2();
        fixed (byte* dataPtr = data)
        {
            // Generate pixels data (linear gradient)
            var colorsPtr = (Color32*)dataPtr;
            for (int y = 0; y < initData.Height; y++)
            {
                point.Y = y / (float)initData.Height;
                for (int x = 0; x < initData.Width; x++)
                {
                    point.X = x / (float)initData.Width;
                    var noiseVal = Noise.PerlinNoise(point * Scale);
                    colorsPtr[y * initData.Width + x] = Color32.Lerp(Color32.White, Color32.Black, noiseVal);
                }
            }
        }
        initData.Mips = new[]
        {
            // Initialize mip maps data container description
            new TextureBase.InitData.MipData
            {
                Data = data,
                RowPitch = data.Length / initData.Height,
                SlicePitch = data.Length
            },
        };
        texture.Init(ref initData);

        // Use a dynamic material instance with a texture to sample
        var material = Material.CreateVirtualInstance();
        _tempMaterialInstance = material;
        material.SetParameterValue("tex", texture);

        // Add a model actor and use the dynamic material for rendering
        var staticModel = Actor.GetOrAddChild<StaticModel>();
        staticModel.Model = Model;
        staticModel.SetMaterial(0, material);
    }
}

