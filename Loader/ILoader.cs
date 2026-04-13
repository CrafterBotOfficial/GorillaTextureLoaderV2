using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaTextureLoader.Loader;

public interface ILoader
{
    public Task<TexturePackMeta[]> LoadAllMetadatas();
    public (TexturePackMeta, Dictionary<string, Texture2DArray>) LoadPack(TexturePackMeta meta);
}
