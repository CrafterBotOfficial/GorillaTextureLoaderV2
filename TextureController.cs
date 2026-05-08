using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GorillaTextureLoader.Loader;
using UnityEngine;

namespace GorillaTextureLoader;

// Singleton class to control all texture related operations
// The actual texture applier is a different thing, although both share a bunch of methods
public class TextureController
{
    private static Lazy<TextureController> instance = new Lazy<TextureController>(() => new TextureController());
    public static TextureController Instance => instance.Value;

    public const string TEXTURE_PACK_FILE_PREFIX = "*.pack";

    public string TexturePackPath;

    public Task<TexturePackMeta[]> PackMetas;
    public TexturePackMeta Current;

    private readonly ILoader loaderV2 = new LoaderV2();
    private TexturePackMeta[] metadatas;

    private readonly Dictionary<string, Texture2DArray> cachedGameTextures = [];
    private readonly Dictionary<string, Material[]> cachedMaterials = [];

    public void Initialize()
    {
        TexturePackPath = Path.Combine(BepInEx.Paths.PluginPath, "GorillaTextureLoader", "packs");
        try { Directory.CreateDirectory(TexturePackPath); } catch { }

        new GameObject().AddComponent<ExtractTemplate>();
        NetworkSystem.Instance.OnJoinedRoomEvent += () =>
        {
            if (!InModdedRoom()) UnloadPack();
        }; // do not load if unable to stop unmodded

        PackMetas = LoadAllPackMetasAsync();
        PackMetas.ContinueWith(_ =>
        {
            if (Jerald.PageManager.Instance.GetPage() is MainPage) MainPage.Update(); // ensure page updates when all packs are loaded

            if (GorillaTagger.Instance.offlineVRRig is not null) AutoLoadPack();
            else GorillaTagger.OnPlayerSpawned(AutoLoadPack);
        });
    }

    private void AutoLoadPack()
    {
        if (PackMetas.Result.FirstOrDefault(x => x.Id == Configuration.CurrentTexturePack.Value) is TexturePackMeta meta && meta.IsVerified)
        {
            LoadPack(meta);
        }
    }

    public void LoadPack(TexturePackMeta meta)
    {
        UnloadPack();
        if (!meta.IsVerified)
        {
            Main.Log("Pack univerified");
            if (!InModdedRoom())
            {
                Main.Log("Pack not allowed in unmodded rooms", BepInEx.Logging.LogLevel.Warning);
                return;
            }
        }

#if DEBUG
        var watch = Stopwatch.StartNew();
#endif
        var ok = loaderV2.LoadPack(meta);//.ContinueWith(task => // todo: verify works
        // {
            // if (task.IsFaulted)
            // {
            //     Main.Log($"Failed to load pack {meta.Name} {task.Exception}");
            //     return;
            // }

            // todo: add to remote server + offline for names->key
            Main.Log($"Applying texture {(meta.ForceNew ? "New" : "Slice")}");
            var applier = new TextureApplier(cachedGameTextures, cachedMaterials);
            applier.Start(meta, ok.Result);

#if DEBUG
            watch.Stop();
            Main.Log($"Finished in {watch.Elapsed.Seconds} {watch.Elapsed.Milliseconds}ms");
#endif
            Current = meta;
        // });
    }

    public void UnloadPack()
    {
        if (Current is null)
            return;
        Main.Log("Reset");
        Current = null;
        foreach (var texturePair in cachedGameTextures)
        {
            var materials = FindMaterialByTextureName(texturePair.Key);
            foreach (var material in materials)
                material.SetTexture("_BaseMap_Atlas", texturePair.Value);
        }
    }


    public Texture2DArray CreateTextureArray(Texture2D[] textures)
    {
        var slice0 = textures[0];
        Main.Log($"Creating array with {textures.Length} slices {slice0.width}x{slice0.height} pixels", BepInEx.Logging.LogLevel.Debug);
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
                Main.Log("Invalid textrurepack", BepInEx.Logging.LogLevel.Error);
                break;
            }
            Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
        }

        textureArray.filterMode = FilterMode.Point;
        textureArray.Apply(false, true);
        return textureArray;
    }

    public void CacheGameTextures(string textureName, Texture2DArray atlas)
    {
        if (!cachedGameTextures.ContainsKey(textureName))
        {
            Main.Log("Saving default textures");
            var cache = new Texture2DArray(atlas.width, atlas.height, atlas.depth, atlas.format, 0, false)
            {
                filterMode = atlas.filterMode,
                anisoLevel = atlas.anisoLevel,
                wrapMode = atlas.wrapMode,
            };

            Graphics.CopyTexture(atlas, cache);
            cachedGameTextures.Add(textureName, cache);
        }
    }

    // note: cant just edit shared materials due to pitground mat
    public Material[] FindMaterialByTextureName(string name)
    {
        if (cachedMaterials.TryGetValue(name, out var cached))
            return cached;

        const string KEY = "_BaseMap_Atlas";
        var materials = GameObject.FindObjectsByType<MeshRenderer>(sortMode: FindObjectsSortMode.InstanceID)
            .Where(x => x.sharedMaterial is not null && x.sharedMaterial.HasTexture(KEY))
            .Where(x => x.sharedMaterial.GetTexture(KEY)?.name == name)
            .Select(x => x.sharedMaterial)
            .ToArray();

        cachedMaterials.Add(name, materials);
        return materials;
    }

    // Todo: Add legacy loader, if reasonably possible
    public async Task<TexturePackMeta[]> LoadAllPackMetasAsync()
    {
        if (metadatas is not null) return metadatas;
        var result = await loaderV2.LoadAllMetadatas();
        metadatas = result;
        return result;
    }

    // To allow verified packs to work without utilla
    private bool InModdedRoom()
    {
        var networkSystem = NetworkSystem.Instance;
        return !networkSystem.InRoom || networkSystem.GameModeString.StartsWith("MODDED_");
    }
}
