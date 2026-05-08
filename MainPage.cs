using System;
using System.Linq;
using GorillaNetworking;
using Jerald;

[assembly: AutoRegister]
namespace GorillaTextureLoader;

[AutoRegister]
public class MainPage : ListPage
{
    public static Action Update;

    public override string PageName => "TextureLoader";

    public MainPage()
    {
        Update += UpdateContent;
    }

    public override string GetContent()
    {
        var instance = TextureController.Instance;
        if (!instance.PackMetas.IsCompleted)
            return "Loading...";

        string result = "GorillaTextureLoader v2";
        var metas = instance.PackMetas.Result;
        Items = metas.Select(x => x.Name);
        Initialize();

        return result;
    }

    public override void OnKeyPress(GorillaKeyboardBindings key)
    {
        base.OnKeyPress(key);
        if (key == GorillaKeyboardBindings.enter)
        {
            // var meta = TextureController.Instance.PackMetas.Result[];
            // TextureController.Instance.UnloadPack();
            // TextureController.Instance.LoadPack(meta);
            // Configuration.CurrentTexturePack.Value = meta.Id;
        }
        UpdateContent();
    }
}
