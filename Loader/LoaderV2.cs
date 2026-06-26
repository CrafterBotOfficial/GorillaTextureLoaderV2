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
            WhitelistedPack = await httpClient.GetStringAsync(Paths.BASE_URL + "/verified.csv");
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
        var tasks = files.Select(path => Task.Run(() => LoadMetadata(path)));
        return await Task.WhenAll(tasks);
    }

    private TexturePackMeta LoadMetadata(string file)
    {
        Main.Log("Attempting to open " + file, BepInEx.Logging.LogLevel.Debug);
        try
        {
            var fileStream = File.Open(file, FileMode.Open);
            string hash = GetHash(fileStream);
            var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            var entry = zipArchive.GetEntry("package.json") ?? throw new Exception("No metadata found!");

            using var entryStream = entry.Open();
            using var streamReader = new StreamReader(entryStream);
            string raw = streamReader.ReadToEnd();

            var metadata = JsonConvert.DeserializeObject<TexturePackMeta>(raw);
            metadata.ZipFilePath = file;
            return metadata with
            {
                IsVerified = WhitelistedPack.Contains(hash),
                LoadTask = LoadPack(metadata, fileStream, zipArchive),
            };
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to read {file} {ex.Message} {ex.StackTrace}", BepInEx.Logging.LogLevel.Warning);
            return null;
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
                string atlasName = RemapManager.Instance.GetAtlasFromTextureName(sanitizedName);

                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream);
                using var binaryReader = new BinaryReader(memoryStream); // todo: only use binaryreader instead of unsafe casting (fix it weirdly reading to end fo stream always)

                // binaryReader.ReadBytes(4); // magic
                memoryStream.Position = 4;

                // https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
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
            foreach (var dds in temp)
            {
                var texture = new Texture2D(dds.Header.dwWidth, dds.Header.dwHeight, TextureFormat.BC7, false, false); // height should always euqla with
                texture.LoadRawTextureData(dds.Pixels);
                texture.filterMode = FilterMode.Point;
                texture.Apply(false, true);

                if (RemapManager.Instance.IsSingle(dds.SanitizedName))
                {
                    Main.Log("Single found " + dds.SanitizedName, BepInEx.Logging.LogLevel.Message);
                    singles.Add(dds.SanitizedName, texture);
                    continue;
                }

                // remap
                int sliceIndex = RemapManager.Instance.RemapTexture(dds.SanitizedName);
                Main.Log($"Mapped {dds.AtlasName} {sliceIndex} {texture.name}", BepInEx.Logging.LogLevel.Debug);
                if (!remaps.ContainsKey(dds.AtlasName)) remaps[dds.AtlasName] = [];
                remaps[dds.AtlasName][sliceIndex] = texture;
            }

            return new LoadedPack(remaps, singles);
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

public record class LoadedPack(Dictionary<string, Dictionary<int, Texture2D>> Remaps, Dictionary<string, Texture2D> Singles);
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
