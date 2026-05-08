using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaTextureLoader.Loader;

public interface ILoader
{
    public Task<TexturePackMeta[]> LoadAllMetadatas();
    public Dictionary<string, Dictionary<int, Texture2D>> LoadPack(TexturePackMeta meta);
}
