using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaTextureLoader;

public class ExtractTemplate : MonoBehaviour
{
    private const string EXTRACT_DIRECTORY = "{0}/templates/{1}";

    private void Start()
    {
        TryExtract().ContinueWith(task =>
        {
            Main.Log(task.Exception, BepInEx.Logging.LogLevel.Error);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task TryExtract()
    {
        string outputDirectory = await GetDirectory();

        if (Directory.Exists(outputDirectory)) return;

        Main.Log("Extracting template files");

        try
        {
            Directory.CreateDirectory(outputDirectory);

            var remaps = await RemapManager.Instance.GetRemapsWithAtlas();
            // var atlases = GameObject.FindObjectsByType<Material>(sortMode: FindObjectsSortMode.None).Where(material => remaps.ContainsKey(material.name)).Select(material => material.GetTexture("_BaseMap_Atlas") as Texture2DArray);
            foreach (var atlasMap in remaps)
            {
                Main.Log($"{atlasMap.Key}", BepInEx.Logging.LogLevel.Message);

                // todo: split FindMaterialByTextureName into 2 methjods
                if (TextureController.Instance.FindMaterialByTextureName(atlasMap.Key).First().GetTexture("_BaseMap_Atlas") is not Texture2DArray atlas) continue;

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

    private async Task<string> GetDirectory()
    {
        string latestVersion = await RemapManager.Instance.GetLatestVersion();
        return string.Format(EXTRACT_DIRECTORY, Path.Join(BepInEx.Paths.PluginPath, "GorillaTextureLoader"), latestVersion);
    }
}
