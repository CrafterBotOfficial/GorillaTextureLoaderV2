using System;
using System.Threading.Tasks;
using GorillaTextureLoader.Loader;
using Newtonsoft.Json;

namespace GorillaTextureLoader;

public record class TexturePackMeta(
    string Name,
    string Author,
    string PackVersion,
    string CompiledGameVersion,
    Lazy<Task<LoadedPack>> LoadTask
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
