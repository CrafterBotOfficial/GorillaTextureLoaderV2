using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
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
    private static string WhitelistedPack;

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

    public Dictionary<string, Dictionary<int, Texture2D>> LoadPack(TexturePackMeta meta)
    {
        Main.Log("Attempting to load pack to memory", BepInEx.Logging.LogLevel.Message);
        var result = new Dictionary<string, Dictionary<int, Texture2D>>();
        using var archive = ZipFile.OpenRead(meta.ZipFilePath);
        foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".png")))
        {
            string atlasName = Path.GetDirectoryName(entry.FullName);
            // Main.Log(entry.FullName + " to " + atlasName);
            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            entryStream.CopyTo(memoryStream);

            var texture = new Texture2D(0, 0);
            texture.LoadImage(memoryStream.ToArray());
            texture.Compress(false);
            texture.filterMode = FilterMode.Point;
            texture.Apply(false, true);

            // texture.Apply()
            // // todo: add automated resizing of badly made textures

            // remap
            string sanitizedName = entry.Name.RemoveStart("slice_").RemoveEnd(".png");
            int sliceIndex = meta.ForceNew ? int.Parse(sanitizedName) : RemapManager.Instance.RemapTexture(sanitizedName);
            if (!result.ContainsKey(atlasName)) result[atlasName] = [];
            Main.Log($"Mapped {atlasName} {sliceIndex} {texture.name}");
            result[atlasName][sliceIndex] = texture;
        }

        return result;
    }

    private string GetHash(FileStream stream)
    {
        stream.Position = 0;
        using var hashAlgorithm = SHA256.Create();
        var hashBytes = hashAlgorithm.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private async Task<bool> IsVerified(string hash)
    {
        if (WhitelistedPack is null || WhitelistedPack.Length == 0)
        {
            try
            {
                using var httpClient = new HttpClient();
                WhitelistedPack = await httpClient.GetStringAsync("https://git.crafterbot.com/Crafterbot/GorillaTextureLoader/raw/branch/v2/verified.csv");
            }
            catch (Exception ex)
            {
                Main.Log($"Failed to grab verified list. Exception: {ex}");
                return false;
            }
        }

        return WhitelistedPack.Contains(hash);
    }
}
