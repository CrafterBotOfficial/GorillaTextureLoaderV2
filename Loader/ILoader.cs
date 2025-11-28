using GorillaTextureLoader.Map;

namespace GorillaTextureLoader.Loader;

public interface ILoader
{
    public TexturePack[] GetTexturePacks(string dir);

    public void LoadTextures(EMap map, TexturePack pack);
}
