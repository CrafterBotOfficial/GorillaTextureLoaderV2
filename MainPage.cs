using System.Linq;
using System.Text;
using GorillaNetworking;
using Jerald;

[assembly: AutoRegister]

namespace GorillaTextureLoader;

[AutoRegister]
public class MainPage : ListPage
{
    public static MainPage Instance;

    public override string PageName => "Textureloader";

    public MainPage()
    {
        Instance = this;
    }

    public override string GetContent()
    {
        var metas = TextureController.Instance.PackMetas;
        if (metas is null || !metas.IsCompleted)
        {
            return "Texturepacks are loading...";
        }
        else if (Items is null || Items.Count() == 0)
        {
            Items = metas.Result.Select(x => x.Name);
            return "Populating...";
        }

        var builder = new StringBuilder("GorillaTextureLoader");
        builder.AppendLine("");
        builder.Append(GetListText());
        builder.AppendLine(GetPageCountText());
        return builder.ToString();
    }

    public override void OnKeyPress(GorillaKeyboardBindings key)
    {
        base.OnKeyPress(key);
        if (key == GorillaKeyboardBindings.enter)
        {
            var selected = TextureController.Instance.PackMetas.Result[SelectedIndex];
            TextureController.Instance.LoadPack(selected).ContinueWith(task => Main.Log(task.Exception, BepInEx.Logging.LogLevel.Error), System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
