namespace GorillaTextureLoader.Applier;

public interface IApplier
{
    public void Start(Loader.LoadedPack combined);
    public void Cleanup();
}
