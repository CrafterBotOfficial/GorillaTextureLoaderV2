using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using GorillaTextureLoader.Map;
using Newtonsoft.Json;
using UnityEngine;

namespace GorillaTextureLoader.Loader;

public class LegacyLoader : ILoader
{
    public TexturePack[] GetTexturePacks(string dir)
    {
        List<TexturePack> result = [];
        var packs = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".texture"));
        foreach (var path in packs)
        {
            try
            {
                using var reader = File.OpenRead(path);
                using var archive = new ZipArchive(reader);

                var package = GetPackage(archive.GetEntry("package.json"));
                result.Add(new(path, package.Name, package.Description, package.IsVerified));
            }
            catch (Exception ex)
            {
                Main.Log($"Failed to read texturepack {path} {ex}");
            }
        }

        return result.ToArray();
    }

    public void LoadTextures(EMap map, TexturePack pack)
    {
        using var reader = File.OpenRead(pack.Path);
        using var archive = new ZipArchive(reader);
        var textures = new Dictionary<string, Texture2D>()
        {
            { "atlas", new(0, 0) },
            { "ground", new(0, 0) },
            { "treestumproom", new(0, 0) },
            { "treestump", new(0, 0) },
        };

        foreach (var entry in archive.Entries)
        {
            if (!textures.TryGetValue(Path.GetFileNameWithoutExtension(entry.Name), out var texture))
                continue;
            Main.Log($"Loading {entry.Name}");

            using var entryStream = entry.Open();
            using var memoryReader = new MemoryStream();
            entryStream.CopyTo(memoryReader);
            texture.LoadImage(memoryReader.ToArray());
            texture.filterMode = FilterMode.Point;
        }

        pack.Textures = new()
        {
            { EMap.Forest, textures.Select(x => new KeyValuePair<string, Texture2D>(x.Key, x.Value)).ToDictionary(t => t.Key, t => t.Value) }
        };
        Main.Log(pack.Textures is not null);
    }

    private Package GetPackage(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        return JsonConvert.DeserializeObject<Package>(reader.ReadToEnd());
    }

    private class Package
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsVerified { get; set; }
    }
}
