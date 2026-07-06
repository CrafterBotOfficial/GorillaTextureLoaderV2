using System.Text;
using System.Threading.Tasks;
using GorillaNetworking;
using Jerald;

[assembly: AutoRegister]

namespace GorillaTextureLoader.Pages;

[AutoRegister]
public class SelectPage : ListPage<TexturePackMeta>
{
    public static SelectPage Instance;

    public override string PageName => "Textureloader";

    private Task loadTextureTask;

    public SelectPage()
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
        else if (metas.IsFaulted)
        {
            Main.Log("Metas IS faileted", BepInEx.Logging.LogLevel.Fatal);
            return $"Failed to load texturepacks. \nMake sure the mod is correctly installed.\n\n<size=65%><color=red>{metas.Exception.Message}</color></size>";
        }
        else if (Items?.Count == 0)
        {
            return "No texturepacks found! Make sure you correctly installed the mod.";
        }

        var builder = new StringBuilder("<size=110%>GorillaTextureLoader");
        builder.AppendLine("<size=60%> By Crafterbot</size><color=#e3e3e3>");


        builder.Append("<size=80%>Currently Selected: [");
        builder.Append(TextureController.Instance.Current is null ? "-" : TextureController.Instance.Current.Name);
        builder.AppendLine("]</size>");

        builder.Append(GetListText());
        builder.AppendLine("</color>\n");
        builder.Append(GetPageCountText());
        if (UpdateChecker.UpdateAvailable)
            builder.Append(" - <color=yellow><b>A new update is available!</b></color>");

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
                Main.Log("Texture is currently loading. \nPlease wait for the current task to complete.", BepInEx.Logging.LogLevel.Warning);
                return;
            }

            var selected = TextureController.Instance.PackMetas.Result[SelectedIndex];
            if (selected == TextureController.Instance.Current)
            {
                Main.Log("TextureController current is selected", BepInEx.Logging.LogLevel.Warning);
                SetText("<color=red>You cannot load the same texturepack twice.</color>\nPress any key to continue.");
                return;
            }

            SetText("...");

            loadTextureTask = TextureController.Instance.LoadPack(selected)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted || task.Exception is not null)
                    {
                        Main.Log($"Load pack task fail. {task.Exception ?? default}", BepInEx.Logging.LogLevel.Error);
                        var errorMessage = new StringBuilder("<color=red>Failed to load texturepack.</color>\n");
                        errorMessage.AppendLine(new string('=', PREFERRED_WIDTH));

                        errorMessage.AppendLine(task.Exception is null ? "No exception detected :/" : $"Error: <size=80%><color=red>{task.Exception.Message}</color></size>");
                        errorMessage.AppendLine(string.Empty);
                        errorMessage.AppendLine("<size=80%>Check LogOutput.txt for more details.</size>");

                        SetText(errorMessage.ToString());
                        return;
                    }

                    SetText($"<align=center><size=110%><color=green>Loaded custom texturepack!</color></size></align>\n<size=90%>Press any key to continue</size>");
                }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
