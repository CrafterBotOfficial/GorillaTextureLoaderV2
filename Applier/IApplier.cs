namespace GorillaTextureLoader.Applier;

public interface IApplier
{
    public void Start(LoadedPack combined);
    public void Cleanup();
}
