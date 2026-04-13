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
    private Dictionary<TexturePackMeta, Dictionary<string, Texture2DArray>> cache = new();

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

    public (TexturePackMeta, Dictionary<string, Dictionary<int, Texture2D>>) LoadPack(TexturePackMeta meta)
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

            // var texture = LoadTextureDXT(memoryStream.ToArray(), TextureFormat.DXT5);
            var texture = new Texture2D(0, 0);
            texture.LoadImage(memoryStream.ToArray());
            texture.Compress(true);
            texture.filterMode = FilterMode.Point;
            int index = int.Parse(entry.Name.RemoveStart("slice_").RemoveEnd(".png"));

            if (result.TryGetValue(atlasName, out var dict)) dict.Add(index, texture);
            else result.Add(atlasName, new Dictionary<int, Texture2D> { { index, texture } });

            // Main.Log("Finished");
        }

        return (meta, result);
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

    // https://discussions.unity.com/t/can-you-load-dds-textures-during-runtime/84192/2
    public static Texture2D LoadTextureDXT(byte[] ddsBytes, TextureFormat textureFormat)
    {
        if (textureFormat != TextureFormat.DXT1 && textureFormat != TextureFormat.DXT5)
            throw new Exception("Invalid TextureFormat. Only DXT1 and DXT5 formats are supported by this method.");

        byte ddsSizeCheck = ddsBytes[4];
        if (ddsSizeCheck != 124)
            throw new Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

        int height = ddsBytes[13] * 256 + ddsBytes[12];
        int width = ddsBytes[17] * 256 + ddsBytes[16];

        int DDS_HEADER_SIZE = 128;
        byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
        Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

        Texture2D texture = new Texture2D(width, height, textureFormat, false);
        texture.LoadRawTextureData(dxtBytes);
        texture.Apply();

        return (texture);
    }
}
