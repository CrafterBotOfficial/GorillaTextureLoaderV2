using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

public class ExtractTemplate : MonoBehaviour
{
    private const string EXTRACT_DIRECTORY = "{0}/templates/{1}";

    private void Start()
    {
        TryExtract();
    }

    private void TryExtract()
    {
        string outputDirectory = GetDirectory();

        if (Directory.Exists(outputDirectory)) return;

        Main.Log("Extracting template files");

        try
        {
            Directory.CreateDirectory(outputDirectory);

            // var cache = TextureController.GetTextureCache();
            var cache = new TextureCache(false); // throw away cache so errors here cant break the rest of the mod
            var remaps = RemapManager.Instance.GetRemapAndSinglessWithAtlas();
            foreach (var atlasMap in remaps)
            {
                if (cache.FindTextureByName(atlasMap.Key).FirstOrDefault() is not Texture2DArray atlas)
                {
                    var originalName = atlasMap.Value.First().Key;
                    var original = cache.FindTextureByName(originalName, Paths.MAIN_KEY).First() as Texture2D;
                    string filename = Path.Combine(outputDirectory, $"{atlasMap.Key}.png");
                    DumpTexture(original, TextureFormat.ARGB32, 0, filename);
                    continue;
                }

                foreach (var map in atlasMap.Value)
                {
                    string filename = Path.Combine(outputDirectory, $"{map.Key}.png");
                    DumpTexture(atlas, atlas.format, map.Value, filename);
                }
            }
        }
        catch (Exception ex)
        {
            Main.Log($"Error occured while dumping textures {ex}");
            if (outputDirectory.Contains("GorillaTextureLoader/templates/"))
                Directory.Delete(outputDirectory, true);
        }
    }

    private void DumpTexture(Texture atlas, TextureFormat format, int index, string output)
    {
        var renderTexture = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var readback = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
        var temp = new Texture2D(atlas.width, atlas.height, format, 0, false);
        try
        {
            Graphics.CopyTexture(atlas, index, 0, temp, 0, 0);
            Graphics.Blit(temp, renderTexture);

            RenderTexture.active = renderTexture;
            readback.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
            readback.Apply();

            var pixels = readback.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = pixels[i].gamma; // fixes dark textres https://discussions.unity.com/t/exporting-png-from-custom-render-texture-in-hdrp-results-in-darker-image/812166/13
            }

            var image = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false);
            image.SetPixels(pixels);
            image.Apply();

            byte[] bytes = image.EncodeToPNG();
            File.WriteAllBytes(output, bytes);
            Destroy(image);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(renderTexture);
            temp.Destroy();
            readback.Destroy();
        }
    }

    private string GetDirectory()
    {
        string latestVersion = RemapManager.Instance.GetLatestVersion();
        return string.Format(EXTRACT_DIRECTORY, Path.Join(BepInEx.Paths.PluginPath, "GorillaTextureLoader"), latestVersion);
    }
}
