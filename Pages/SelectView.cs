using System.Text;
using System.Threading.Tasks;
using ComputerInterface.Behaviors.UI;
using ComputerInterface.Enumerations;
using ComputerInterface.Extensions;
using ComputerInterface.Interfaces;
using ComputerInterface.Models;

namespace GorillaTextureLoader.Pages;

public class SelectView : ComputerView
{
    public static SelectView Instance;

    public UIElementPageHandler<TexturePackMeta> pageHandler;
    public UISelectionHandler selectionHandler;

    private Task loadTextureTask;

    public SelectView()
    {
        Instance = this;

        pageHandler = new UIElementPageHandler<TexturePackMeta>(EKeyboardButton.Left, EKeyboardButton.Right);
        pageHandler.SetElements([]);
        pageHandler.EntriesPerPage = 8;

        selectionHandler = new UISelectionHandler(EKeyboardButton.Up, EKeyboardButton.Down, EKeyboardButton.Enter);
        selectionHandler.ConfigureSelectionIndicator("<color=#ed6540>> </color>", "", "  ", "");
        selectionHandler.OnSelected += (index) =>
        {
            var selected = TextureController.Instance.PackMetas.Result[index];
            loadTextureTask = TextureController.Instance.LoadPack(selected)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted || task.Exception is not null)
                    {
                        Main.Log($"Load pack task fail. {task.Exception ?? default}", BepInEx.Logging.LogLevel.Error);
                        var errorMessage = new StringBuilder("<color=red>Failed to load texturepack.</color>\n");
                        errorMessage.AppendLine(new string('=', 20));

                        errorMessage.AppendLine(task.Exception is null ? "No exception detected :/" : $"Error: <size=80%><color=red>{task.Exception.Message}</color></size>");
                        errorMessage.AppendLine(string.Empty);
                        errorMessage.AppendLine("<size=80%>Check LogOutput.txt for more details.</size>");

                        ShowView<InfoView>(errorMessage.ToString());

                        TextureController.Instance.UnloadPack();
                        TextureController.Instance.ClearCache();
                        return;
                    }

                    ShowView<InfoView>($"<align=center><size=110%><color=green>Loaded custom texturepack!</color></size></align>\n<size=90%>Press any key to continue</size>");
                }, TaskContinuationOptions.ExecuteSynchronously);
        };

        if (TextureController.Instance.PackMetas.IsCompleted)
        {
            pageHandler.SetElements(TextureController.Instance.PackMetas.Result);
            selectionHandler.MaxIndex = TextureController.Instance.PackMetas.Result.Length - 1;
        }
        else
            TextureController.Instance.PackMetas.ContinueWith(task =>
            {
                pageHandler.SetElements(task.Result);
                selectionHandler.MaxIndex = task.Result.Length - 1;
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    protected override string GetViewText()
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
        else if (TextureController.Instance.PackMetas.Result?.Length == 0)
        {
            return "No texturepacks found! Make sure you correctly installed the mod.";
        }

        pageHandler.MovePageToIndex(selectionHandler.CurrentSelectionIndex);

        var builder = new StringBuilder();
        builder.BeginCenter();
        builder.Append("<size=110%>GorillaTextureLoader");
        builder.AppendLine("<size=60%> By Crafterbot</size><color=#e3e3e3>");
        builder.EndAlign();

        builder.Append("<size=80%>Currently Selected: [");
        builder.Append(TextureController.Instance.Current is null ? "-" : TextureController.Instance.Current.Name);
        builder.AppendLine("]</size>");

        pageHandler.EnumerateElements((packMeta, relativeIndex) =>
        {
            int index = pageHandler.GetAbsoluteIndex(pageHandler.CurrentPage, relativeIndex);
            string color = index % 2 == 0 ? "white" : "#ffffff50";
            string text = selectionHandler.GetIndicatedText(index, $"<color={color}>{packMeta.Name}</color>");
            builder.AppendLine(text);
        });
        builder.AppendLine("</color>\n");
        pageHandler.AppendFooter(builder);
        if (UpdateChecker.UpdateAvailable)
            builder.Append(" - <color=yellow><b>A new update is available!</b></color>");

        return builder.ToString();
    }

    public override void OnButtonPressed(EKeyboardButton key)
    {
        if (pageHandler.HandleButtonPress(key))
        {
            UpdateViewScreen();
            return;
        }

        if (selectionHandler.HandleButtonPress(key))
        {
            UpdateViewScreen();
            return;
        }

        if (key == EKeyboardButton.Back)
        {
            ReturnToMainMenu();
            return;
        }

        if (key == EKeyboardButton.Option1)
        {
            TextureController.Instance.UnloadPack();
            return;
        }

        if (key == EKeyboardButton.Enter)
        {
            if (loadTextureTask is not null && !loadTextureTask.IsCompleted)
            {
                Main.Log("Texture is currently loading. \nPlease wait for the current task to complete.", BepInEx.Logging.LogLevel.Warning);
                return;
            }

            var selected = TextureController.Instance.PackMetas.Result[selectionHandler.CurrentSelectionIndex];
            if (selected == TextureController.Instance.Current)
            {
                Main.Log("TextureController current is selected", BepInEx.Logging.LogLevel.Warning);

                ShowView<InfoView>("<color=red>You cannot load the same texturepack twice.</color>\nPress any key to continue.");
                return;
            }

            ShowView<InfoView>("...");
        }
    }
}

public class PageEntry : IComputerViewEntry
{
    public string EntryName => "GorillaTextureLoader";
    public System.Type EntryComputerView => typeof(SelectView);
}
