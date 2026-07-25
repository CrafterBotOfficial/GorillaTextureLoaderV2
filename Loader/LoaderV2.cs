using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
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
        try
        {
            using var httpClient = new HttpClient();
            WhitelistedPack = await httpClient.GetStringAsync(Paths.BASE_URL + "verified.csv");
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to grab verified list. Exception: {ex}");
            WhitelistedPack = string.Empty;
        }

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
        Main.Log("Attempting to open " + file, BepInEx.Logging.LogLevel.Debug);

        try
        {
            var fileStream = File.OpenRead(file);
            string hash = GetHash(fileStream);
            var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            var entry = zipArchive.GetEntry("package.json") ?? throw new Exception("No metadata found!");

            using var entryStream = entry.Open();
            using var streamReader = new StreamReader(entryStream);
            string raw = await streamReader.ReadToEndAsync();

            var metadata = JsonConvert.DeserializeObject<TexturePackMeta>(raw);
            metadata.ZipFilePath = file;
            var result = metadata with
            {
                IsVerified = WhitelistedPack.Contains(hash),
                LoadTask = new Lazy<Task<LoadedPack>>(() => LoadPack(metadata, fileStream, zipArchive)),
            };

            if (Configuration.EnableBackgroundTextureLoading.Value)
                _ = result.LoadTask.Value;
            return result;
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to load metadata {file} {ex}");
            return new TexturePackMeta(Path.GetFileNameWithoutExtension(file), "", "", "", null) with
            {
                ErrorMessage = ex.Message,
            };
        }
    }

    public async Task<LoadedPack> LoadPack(TexturePackMeta meta, FileStream fileStream, ZipArchive archive)
    {
        try
        {
            Main.Log("Attempting to load pack to memory", BepInEx.Logging.LogLevel.Message);
            var temp = new List<TempDDsArrayItem>();
            var singles = new Dictionary<string, Texture2D>();
            var remaps = new Dictionary<string, Dictionary<int, Texture2D>>();
            foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".dds")))
            {
                string sanitizedName = entry.Name.RemoveStart("slice_").RemoveEnd(".dds");
                var (found, atlasName) = RemapManager.Instance.GetAtlasFromTextureName(sanitizedName);
                if (!found)
                {
                    Main.Log("Skipping " + entry.FullName, BepInEx.Logging.LogLevel.Warning);
                    continue;
                }

                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream);
                using var binaryReader = new BinaryReader(memoryStream); // todo: only use binaryreader instead of unsafe casting (fix it weirdly reading to end fo stream always)

                // binaryReader.ReadBytes(4); // magic
                memoryStream.Position = 4;

                DDS_HEADER header;
                unsafe
                {
                    var bytes = binaryReader.ReadBytes(sizeof(DDS_HEADER));
                    fixed (byte* ptr = bytes)
                    {
                        header = *(DDS_HEADER*)ptr;
                    }

                    memoryStream.Position = 4 + sizeof(DDS_HEADER) + 20; // dxt10 has 20 extra bytes in head
                }

                var pixelData = binaryReader.ReadBytes((int)(memoryStream.Length - memoryStream.Position));
                temp.Add(new(header, sanitizedName, atlasName, pixelData));
            }

            await Awaitable.MainThreadAsync();
            bool hasMipMaps = false;
            foreach (var dds in temp)
            {
                if (!hasMipMaps && dds.Header.dwMipMapCount > 1) hasMipMaps = true;
                try
                {
                    // Main.Log($"LoaderV2 {dds.Header.dwWidth}  {dds.Header.dwHeight}");
                    var texture = new Texture2D(dds.Header.dwWidth, dds.Header.dwHeight, TextureFormat.BC7, dds.Header.dwMipMapCount > 1, false); // height should always euqla width
                    texture.LoadRawTextureData(dds.Pixels);
                    texture.filterMode = FilterMode.Point;
                    texture.Apply(false, true);

                    if (RemapManager.Instance.IsSingle(dds.SanitizedName))
                    {
                        // Main.Log("Single found " + dds.SanitizedName, BepInEx.Logging.LogLevel.Debug);
                        singles.Add(dds.SanitizedName, texture);
                        continue;
                    }

                    // remap
                    int sliceIndex = RemapManager.Instance.RemapTexture(dds.SanitizedName);
                    if (!remaps.ContainsKey(dds.AtlasName)) remaps[dds.AtlasName] = [];
                    remaps[dds.AtlasName][sliceIndex] = texture;
                }
                catch (Exception ex)
                {
                    Main.Log($"Malformed texture {meta.Name} {dds.SanitizedName}. Please recompile the pack and ensure all textures are multiples of 4", BepInEx.Logging.LogLevel.Warning);
                    Main.Log(ex, BepInEx.Logging.LogLevel.Error);
                }
            }

            return new LoadedPack(hasMipMaps, remaps, singles);
        }
        finally
        {
            fileStream.Close();
            archive.Dispose();
        }
    }

    private string GetHash(FileStream stream)
    {
        using var hashAlgorithm = SHA256.Create();
        var hashBytes = hashAlgorithm.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

record struct TempDDsArrayItem(DDS_HEADER Header, string SanitizedName, string AtlasName, byte[] Pixels);

// copied from https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dds-header

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct DDS_PIXELFORMAT
{
    public Int32 dwSize;
    public Int32 dwFlags;
    public Int32 dwFourCC;
    public Int32 dwRGBBitCount;
    public Int32 dwRBitMask;
    public Int32 dwGBitMask;
    public Int32 dwBBitMask;
    public Int32 dwABitMask;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
unsafe struct DDS_HEADER
{
    public Int32 dwSize;
    public Int32 dwFlags;
    public Int32 dwHeight;
    public Int32 dwWidth;
    public Int32 dwPitchOrLinearSize;
    public Int32 dwDepth;
    public Int32 dwMipMapCount;
    public fixed Int32 dwReserved1[11];
    public DDS_PIXELFORMAT ddspf;
    public Int32 dwCaps;
    public Int32 dwCaps2;
    public Int32 dwCaps3;
    public Int32 dwCaps4;
    public Int32 dwReserved2;
};
