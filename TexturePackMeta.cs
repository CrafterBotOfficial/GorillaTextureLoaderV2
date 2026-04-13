using Newtonsoft.Json;

namespace GorillaTextureLoader;

public record class TexturePackMeta(
    string Name,
    string Author,
    string PackVersion,
    string CompiledGameVersion
)
{
    [JsonIgnore] public bool IsVerified;
    [JsonIgnore] public string ZipFilePath;
}
