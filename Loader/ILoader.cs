using System.Threading.Tasks;

namespace GorillaTextureLoader.Loader;

public interface ILoader
{
    public Task<TexturePackMeta[]> LoadAllMetadatas();
    public LoadedPack LoadPack(TexturePackMeta meta);
}
