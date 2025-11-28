using System.Collections.Generic;
using GorillaTextureLoader.Map;
using UnityEngine;

namespace GorillaTextureLoader;

public class TexturePack(string path, string name, string description, bool hasAdvantage)
{
    public string Path = path;
    public string Name = name;
    public string Description = description;
    public bool HasAdvantage = hasAdvantage;
    public Dictionary<EMap, TextureWrapper[]> Textures;
}

public record class TextureWrapper(string Name, Texture2D Texture);
