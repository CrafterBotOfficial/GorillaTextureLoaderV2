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
        var files = Directory.EnumerateFiles(
            Paths.TexturePackDirectory,
            Paths.TEXTURE_PACK_FILE_SUFFIX,
            SearchOption.AllDirectories);
        var tasks = files.Select(LoadMetadata);
        return await Task.WhenAll(tasks);
    }

    private async Task<TexturePackMeta> LoadMetadata(string file)
    {
        try
        {
            Main.Log("Attempting to open " + file, BepInEx.Logging.LogLevel.Debug);
            using var fileStream = File.Open(file, FileMode.Open);
            string hash = GetHash(fileStream);
            using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            var entry = zipArchive.GetEntry("package.json") ?? throw new Exception("No metadata found!");

            using var entryStream = entry.Open();
            using var streamReader = new StreamReader(entryStream);
            string raw = streamReader.ReadToEnd();

            var metadata = JsonConvert.DeserializeObject<TexturePackMeta>(raw);
            return metadata with
            {
                IsVerified = await IsVerified(hash),
                ZipFilePath = file,
            };
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to read {file} {ex.Message} {ex.StackTrace}", BepInEx.Logging.LogLevel.Warning);
            return null;
        }
    }

    public LoadedPack LoadPack(TexturePackMeta meta)
    {
        Main.Log("Attempting to load pack to memory", BepInEx.Logging.LogLevel.Message);
        var singles = new Dictionary<string, Texture2D>();
        var result = new Dictionary<string, Dictionary<int, Texture2D>>();
        using var archive = ZipFile.OpenRead(meta.ZipFilePath);
        foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".png")))
        {
            string sanitizedName = entry.Name.RemoveStart("slice_").RemoveEnd(".png");
            string atlasName = RemapManager.Instance.GetAtlasFromTextureName(sanitizedName);

            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            entryStream.CopyTo(memoryStream);

            var texture = new Texture2D(0, 0);
            texture.LoadImage(memoryStream.ToArray());
            texture.Compress(false);
            texture.filterMode = FilterMode.Point;
            texture.Apply(false, true);

            // // todo: add automated resizing of badly made textures
            if (RemapManager.Instance.IsSingle(sanitizedName)) {
                Main.Log("Single found " + sanitizedName, BepInEx.Logging.LogLevel.Message);
                singles.Add(sanitizedName, texture);
                continue;
            }

            // remap
            int sliceIndex = meta.ForceNew ? int.Parse(sanitizedName) : RemapManager.Instance.RemapTexture(sanitizedName);
            if (!result.ContainsKey(atlasName)) result[atlasName] = [];
            Main.Log($"Mapped {atlasName} {sliceIndex} {texture.name}");
            result[atlasName][sliceIndex] = texture;
        }

        return new LoadedPack() with {
            Remaps = result,
            Singles = singles,
        };
    }

    private string GetHash(FileStream stream)
    {
        using var hashAlgorithm = SHA256.Create();
        var hashBytes = hashAlgorithm.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private async Task<bool> IsVerified(string hash) // todo: prevent race condition
    {
        if (WhitelistedPack is null || WhitelistedPack.Length == 0)
        {
            try
            {
                using var httpClient = new HttpClient();
                WhitelistedPack = await httpClient.GetStringAsync(Paths.BASE_URL + "/verified.csv");
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

public record struct LoadedPack(Dictionary<string, Dictionary<int, Texture2D>> Remaps, Dictionary<string, Texture2D> Singles);
