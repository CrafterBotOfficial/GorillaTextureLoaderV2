using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace GorillaTextureLoader;

public record class TexturePackMeta(
    string Name,
    string Author,
    string PackVersion,
    string CompiledGameVersion,
    Lazy<Task<LoadedPack>> LoadTask
)
{
    [JsonIgnore] public string Id = $"{Name}.{Author}";
    [JsonIgnore] public bool IsVerified;
    [JsonIgnore] public string ZipFilePath;

    [JsonIgnore] public string ErrorMessage;

    public override string ToString()
    {
        if (!ErrorMessage.IsNullOrEmpty())
            return $"{Name} <color=red>Error: <size=65%>{ErrorMessage}</size></color>";
        return Name;
    }
}

public record class LoadedPack(bool HasMipMaps, Dictionary<string, Dictionary<int, Texture2D>> Remaps, Dictionary<string, Texture2D> Singles)
{
    public bool IsResized()
    {
        try
        {
            foreach (var map in Remaps)
            {
                string[] str = map.Key.RemoveStart("TexArrayAtlas_").RemoveEnd("_BC7_AllScenes").Split('x');
                int expectedWidth = int.Parse(str[0]);
                int expectedHeight = int.Parse(str[1]);
                foreach (var texture in map.Value)
                {
                    if (texture.Value.width != expectedWidth || texture.Value.height != expectedHeight)
                        return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Main.Log($"Error while checking resize status {ex}", BepInEx.Logging.LogLevel.Error);
            return true; // none resized ones willalso work just slower
        }
    }
}
