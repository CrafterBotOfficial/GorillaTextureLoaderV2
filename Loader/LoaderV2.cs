using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace GorillaTextureLoader.Loader;

/// <summary>
/// Responcible for loading all v2 packs to mem
/// </summary>
public class LoaderV2 : ILoader
{
    public async Task<TexturePackMeta[]> LoadAllMetadatas()
    {
        Main.Log("Loading all v2 texture pack's metadata...");
        return [..await Task.Run(async () =>
        {
            var result = new List<TexturePackMeta>();
            foreach (string file in Directory.EnumerateFiles(TextureController.Instance.TexturePackPath, TextureController.TEXTURE_PACK_FILE_PREFIX, SearchOption.AllDirectories))
            {
                try
                {
                    Main.Log("Attempting to open " + file, BepInEx.Logging.LogLevel.Debug);
                    using var fileStream = File.Open(file, FileMode.Open);
                    using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                    var entry = zipArchive.GetEntry("package.json") ?? throw new Exception("No metadata found!");
                    using var entryStream = entry.Open();
                    string raw = new StreamReader(entryStream).ReadToEnd();

                    var metadata = JsonConvert.DeserializeObject<TexturePackMeta>(raw);
                    result.Add(metadata with
                    {
                        IsVerified = await IsVerified(GetHash(fileStream)),
                        ZipFilePath = file,
                    });
                    Main.Log("Done.", BepInEx.Logging.LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Main.Log($"Failed to read {file} {ex.Message} {ex.StackTrace}", BepInEx.Logging.LogLevel.Warning);
                }
            }
            return result;
        })];
    }

    // Todo: rework to not make nightmare
    public (TexturePackMeta, Dictionary<string, Texture2DArray>) LoadPack(TexturePackMeta meta)
    {
        Main.Log("Attempting to load pack to memory", BepInEx.Logging.LogLevel.Message);
        var result = new Dictionary<string, Dictionary<int, Texture2D>>();
        using var archive = ZipFile.OpenRead(meta.ZipFilePath);
        foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".png") && entry.Name.StartsWith("slice_")))
        {
            string atlasName = Path.GetDirectoryName(entry.FullName);
            // Main.Log(entry.FullName + " to " + atlasName);
            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            entryStream.CopyTo(memoryStream);

            var texture = new Texture2D(0, 0);
            texture.LoadImage(memoryStream.ToArray());
            int index = int.Parse(entry.Name.RemoveStart("slice_").RemoveEnd(".png"));

            if (result.TryGetValue(atlasName, out var dict)) dict.Add(index, texture);
            else result.Add(atlasName, new Dictionary<int, Texture2D> { { index, texture } });

            // Main.Log("Finished");
        }

        var textureArrays = new Dictionary<string, Texture2D[]>();
        for (int i = 0; i < result.Count; i++)
        {
            var orderedArray = result.ElementAt(i).Value.OrderBy(x => x.Key).Select(x => x.Value);
            textureArrays[result.ElementAt(i).Key] = [.. orderedArray];
        }

        // map to texture2darray
        var combinedArrays = new Dictionary<string, Texture2DArray>(textureArrays.Count);
        foreach (var pair in textureArrays)
        {
            Main.Log("Mapping to comine");
            combinedArrays[pair.Key] = CreateTexture2DArray(pair.Value);
        }

        return (meta, combinedArrays);
    }

    private string GetHash(FileStream stream)
    {
        using var hashAlgorithm = SHA256.Create();
        var hashBytes = hashAlgorithm.ComputeHash(stream);
        return BitConverter.ToString(hashBytes);
    }

    // Todo
    // To prevent cheating by making wall hacks
    private async Task<bool> IsVerified(string hash)
    {
        Main.Log($"Checking {hash} against whitelist", BepInEx.Logging.LogLevel.Debug);
        return true;
    }

    public static Texture2DArray CreateTexture2DArray(Texture2D[] textures)
    {
        var slice0 = textures[0];
        var textureArray = new Texture2DArray(
            slice0.width,
            slice0.height,
            textures.Length,
            slice0.format,
            false,
            false
        );

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i].width != slice0.width || textures[i].height != slice0.height)
            {
                Main.Log("Invalid textrurepack", BepInEx.Logging.LogLevel.Warning);
                continue;
            }

            Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
        }

        textureArray.filterMode = FilterMode.Point;
        textureArray.anisoLevel = 0;
        textureArray.mipMapBias = 0f;
        textureArray.Apply(false, false);

        return textureArray;
    }
}

