using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace GorillaTextureLoader.Loader;

public interface ILoader
{
    public Task<TexturePackMeta[]> LoadAllMetadatas();
    public Task<LoadedPack> LoadPack(TexturePackMeta meta, FileStream fileStream, ZipArchive archive);
}
