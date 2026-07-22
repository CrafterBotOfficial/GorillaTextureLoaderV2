#if DEBUG

using MonkeStatistics.UI;

namespace GorillaTextureLoader.Pages;

[MonkeStatistics.AutoRegister]
public class DebugWatchPage : IPage
{
    public string GetName() => "TextureLoader";

    public Content GetContent()
    {
        LocalWatchManager.Instance.UIManager.ReturnPage = LocalWatchManager.Instance.UIManager.MainPage;
        var builder = new ScrollPageBuilder();

        builder.AddText("AutoLoad: " + Configuration.EnableAutoLoad.Value);
        builder.AddText("Background Load: " + Configuration.EnableBackgroundTextureLoading.Value);
        builder.AddText("Current: " + Configuration.CurrentTexturePack.Value);

        builder.AddSpacing(1);

        // main menu
        builder.AddLine("Unload Pack", TextureController.Instance.UnloadPack);
        builder.AddLine("Clear Cache", TextureController.Instance.ClearCache);
        builder.AddLine("List Packs", () =>
        {
            foreach (var pack in TextureController.Instance.PackMetas.Result)
            {
                string msg = pack.CompiledGameVersion + " ";
                if (pack.LoadTask.IsValueCreated)
                    msg += pack.LoadTask.Value.Exception?.Message + " " + pack.LoadTask.Value.Result.ToString();
                Main.Log($"pack {pack.Id} {msg}", BepInEx.Logging.LogLevel.Message);
            }
        });

        builder.AddSpacing(2);

        foreach (var pack in TextureController.Instance.PackMetas.Result) // should all be laoded by now, otherwise block game thread
        {
            builder.AddLine(pack.Name, () =>
            {
                LocalWatchManager.Instance.UIManager.ReturnPage = this;
                LocalWatchManager.Instance.UIManager.SwitchPage(new LoadPackPage(pack));
            });
        }

        return builder.GetContent();
    }

    public class LoadPackPage(TexturePackMeta meta) : IPage
    {
        public string GetName() => meta.Id;

        public Content GetContent()
        {
            var pageBuilder = new PageBuilder();
            pageBuilder.AddLine($"Name: <size=60%>{meta.Name}</size>");
            pageBuilder.AddLine($"Author: <size=60%>{meta.Author}</size>");

            pageBuilder.AddSpacing(1);
            pageBuilder.AddLine("Load", () => TextureController.Instance.LoadPack(meta).ContinueWith(task => { Main.Log(task.Exception, BepInEx.Logging.LogLevel.Error); }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted));

            return pageBuilder.GetContent();
        }
    }
}

#endif
