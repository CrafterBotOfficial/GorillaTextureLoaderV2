using System.Threading.Tasks;

namespace GorillaTextureLoader.Loader;

public interface ILoader
{
    public Task<TexturePackMeta[]> LoadAllMetadatas();
    public Task<LoadedPack> LoadPack(TexturePackMeta meta);
}
