#if DEBUG

using System.IO;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

public static class DumpTextures
{
    public static void DoDump()
    {
        const string OUTPUT = "./dump";
        if (Directory.Exists(OUTPUT)) return;

        var materials = GameObject.FindObjectsByType<Material>(sortMode: FindObjectsSortMode.None)
            .Where(x => x.HasTexture("_BaseMap_Atlas"));

        Directory.CreateDirectory(OUTPUT);

        foreach (var material in materials)
        {
            if (material.GetTexture("_BaseMap_Atlas") is not Texture2DArray texture) continue;
            string directoryName = Path.Join(OUTPUT, texture.name);
            if (Directory.Exists(directoryName)) continue;
            Main.Log("Dumping " + material.name);

            Directory.CreateDirectory(directoryName);

            for (int i = 0; i < texture.depth; i++)
            {
                Main.Log($"Slice {i}/{texture.depth}");

                var renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                var temp = new Texture2D(texture.width, texture.height, texture.format, 0, false);
                Graphics.CopyTexture(texture, i, 0, temp, 0, 0);
                Graphics.Blit(temp, renderTexture);

                var readback = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
                RenderTexture.active = renderTexture;
                readback.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                readback.Apply();
                RenderTexture.ReleaseTemporary(renderTexture);

                string filename = Path.Combine(directoryName, $"slice_{i}.png");
                Graphics.CopyTexture(texture, i, 0, temp, 0, 0);
                byte[] bytes = readback.EncodeToPNG();
                File.WriteAllBytes(filename, bytes);
            }
        }
        Main.Log("Finished dump", BepInEx.Logging.LogLevel.Message);
        Application.Quit();
    }
}

#endif
