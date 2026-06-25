using System.Threading.Tasks;
using GorillaTextureLoader.Loader;
using Newtonsoft.Json;

namespace GorillaTextureLoader;

public record class TexturePackMeta(
    string Name,
    string Author,
    string PackVersion,
    string CompiledGameVersion,
    Task<LoadedPack> LoadTask,
    bool ForceNew // todo: rename
)
{
    [JsonIgnore] public string Id = $"{Name}.{Author}";
    [JsonIgnore] public bool IsVerified;
    [JsonIgnore] public string ZipFilePath;

    public override string ToString()
    {
        return Name;
    }
}
