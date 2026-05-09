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

            var remaps = RemapManager.Instance.GetRemapsWithAtlas();
            foreach (var atlasMap in remaps)
            {
                Main.Log($"{atlasMap.Key}", BepInEx.Logging.LogLevel.Message);

                // todo: split FindMaterialByTextureName into 2 methjods
                if (TextureController.GetTextureCache().FindTextureByName(atlasMap.Key).FirstOrDefault() is not Texture2DArray atlas) continue;

                foreach (var map in atlasMap.Value)
                {
                    var renderTexture = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                    var temp = new Texture2D(atlas.width, atlas.height, atlas.format, 0, false);
                    Graphics.CopyTexture(atlas, map.Value, 0, temp, 0, 0);
                    Graphics.Blit(temp, renderTexture);

                    var readback = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
                    RenderTexture.active = renderTexture;
                    readback.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
                    readback.Apply();
                    RenderTexture.ReleaseTemporary(renderTexture);

                    string filename = Path.Combine(outputDirectory, $"{map.Key}.png");
                    Graphics.CopyTexture(atlas, map.Value, 0, temp, 0, 0);
                    byte[] bytes = readback.EncodeToPNG();
                    File.WriteAllBytes(filename, bytes);

                    temp.Destroy();
                    readback.Destroy();
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

    private string GetDirectory()
    {
        string latestVersion = RemapManager.Instance.GetLatestVersion();
        return string.Format(EXTRACT_DIRECTORY, Path.Join(BepInEx.Paths.PluginPath, "GorillaTextureLoader"), latestVersion);
    }
}
