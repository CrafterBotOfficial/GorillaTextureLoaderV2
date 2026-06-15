using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GorillaNetworking;
using Jerald;

[assembly: AutoRegister]

namespace GorillaTextureLoader;

[AutoRegister]
public class MainPage : ListPage<TexturePackMeta>
{
    public static MainPage Instance;

    public override string PageName => "Textureloader";

    private Task loadTextureTask;

    public MainPage()
    {
        Instance = this;
        ItemsPerPage = 8;
    }

    public override string GetContent()
    {
        var metas = TextureController.Instance.PackMetas;
        if (metas is null || !metas.IsCompleted)
        {
            return "Texturepacks are loading...";
        }
        else if (Items.Count == 0) // should never happen
        {
            Items = [.. TextureController.Instance.PackMetas.Result];
        }

        var builder = new StringBuilder("<size=110%>GorillaTextureLoader");
        builder.AppendLine("<size=60%> By Crafterbot</size><color=#e3e3e3>");

        builder.Append("<size=80%>Currently Selected: [");
        builder.Append(TextureController.Instance.Current is null ? "-" : TextureController.Instance.Current.Name);
        builder.AppendLine("]</size>");

        builder.Append(GetListText());
        builder.AppendLine("</color>\n");
        builder.AppendLine(GetPageCountText());
        return builder.ToString();
    }

    public override void OnKeyPress(GorillaKeyboardBindings key)
    {
        if (key == GorillaKeyboardBindings.option1)
        {
            TextureController.Instance.UnloadPack();
            return;
        }
        base.OnKeyPress(key);
        if (key == GorillaKeyboardBindings.enter)
        {
            if (loadTextureTask is not null && !loadTextureTask.IsCompleted)
            {
                Main.Log("Texture is currently loading. Cannot load 2 at once", BepInEx.Logging.LogLevel.Warning);
                return;
            }
            var selected = TextureController.Instance.PackMetas.Result[SelectedIndex];
            loadTextureTask = TextureController.Instance.LoadPack(selected).ContinueWith(task => Main.Log(task.Exception, BepInEx.Logging.LogLevel.Error), System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
            SetText($"<align=center><size=110%><color=green>Loaded custom texturepack!</color></size></align>\n<size=90%>Press any key to continue</size>");
        }
    }
}
