using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTextureLoader.Pages;

[Jerald.AutoRegister]
public class TextureBrowserPage : Jerald.ListPage<TexturePackMeta>
{
    public override string PageName => "Packs";

    private PageState state = PageState.Browser;
    private TexturePackMeta meta;

    private readonly Lazy<Task> remotePacks;
    private Task downloadTask;

    public TextureBrowserPage()
    {
        remotePacks = new Lazy<Task>(() => FetchOnlinePacks().ContinueWith(async _ =>
        {
            await Task.Yield();
            UpdateContent();
        }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously));
    }

    public override string GetContent() => state switch
    {
        PageState.Browser => BrowserPage(),
        PageState.Confirmation => ConfirmationPage(),
    };

    private string BrowserPage()
    {
        if (!Configuration.HasConsented.Value)
        {
            return "By pressing enter you consent to downloading files from crafterbot.com.";
        }

        if (!remotePacks.Value.IsCompleted)
        {
            return "Fetching texturepacks...";
        }

        if (remotePacks.Value.IsFaulted)
        {
            return $"A error occured. \n<size=70%>{remotePacks.Value.Exception.Message}</size>";
        }

        var builder = new StringBuilder("<size=110%>Texture Browser</size>\n");
        builder.AppendLine($"<size=60%>Count: {Items.Count}</size><color=#e3e3e3>"); // todo add to paths
        builder.AppendLine(GetListText());
        builder.Append("</color>");
        builder.Append(GetPageCountText());

        return builder.ToString();
    }

    public string ConfirmationPage()
    {
        if (meta is null)
        {
            state = PageState.Browser;
            return "Meta is not yet selected. Impossible state.";
        }

        var builder = new StringBuilder();
        builder.Append("<size=110%>").Append(meta.Name).AppendLine("</size>\n");
        builder.Append("Author: ").AppendLine(meta.Author);

        builder.AppendLine("Press <b>y</b> to download this texturepack.");

        return builder.ToString();
    }

    public override void OnKeyPress(GorillaKeyboardBindings key)
    {
        if (!Configuration.HasConsented.Value)
        {
            if (key == GorillaKeyboardBindings.enter)
            {
                Configuration.HasConsented.Value = true;
                UpdateContent();
            }
            return;
        }

        switch (state)
        {
            case PageState.Browser:
                if (key != GorillaKeyboardBindings.enter) break;
                meta = Items[SelectedIndex];
                state = PageState.Confirmation;
                UpdateContent();
                break;
            case PageState.Confirmation:
                if (key == GorillaKeyboardBindings.Y)
                {
                    Main.Log("Download confirmed");
                    if (downloadTask is not null && !downloadTask.IsCompleted)
                    {
                        Main.Log("A download is already inprogerss. Impossible state", LogLevel.Warning);
                        UpdateContent();
                        break;
                    }
                    downloadTask = DownloadPack();
                }
                else
                {
                    state = PageState.Browser;
                    meta = null;
                    UpdateContent();
                }
                break;
        }
        base.OnKeyPress(key);
    }

    private async Task DownloadPack()
    {
        string path = "";
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("Downloading...\n");
            builder.AppendLine("Progress:");

            using var client = new HttpClient();
            using var response = await client.GetAsync("https://files.crafterbot.com/StockPhotos.pack", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            path = Path.Combine(Paths.TexturePackDirectory, meta.Name + ".pack");
            if (File.Exists(path))
            {
                SetText("This pack already exists.");
                state = PageState.Browser;
                return;
            }
            // if (!path.ToLower().EndsWith(".pack"))
            // {
            //     Main.Log("Manifest json incorrect.", LogLevel.Warning);
            //     downloadTask = null;
            //     SetText("Disallowed file extension.");
            //     return;
            // }

            long length = response.Content.Headers.ContentLength.Value; // fail if no len

            var downloadStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(path);

            var buffer = new byte[8192];
            int totalRead = 0;
            int read;

            const int width = PREFERRED_WIDTH - 15;
            while ((read = await downloadStream.ReadAsync(buffer)) > 0)
            {
                totalRead += read;
                await fileStream.WriteAsync(buffer, 0, read);

                int filled = Math.Clamp((int)(totalRead * width / length), 0, PREFERRED_WIDTH);

                builder.Clear();
                builder.AppendLine("Downloading... ");
                builder
                    .Append("<mspace=24px>[")
                    .Append(new string('=', filled))
                    .Append(new string(' ', width - filled))
                    .Append("]</mspace>");

                SetText(builder.ToString());
            }

            SetText("Verifying...");
            fileStream.Position = 0;
            string hash = Utilities.GetHash(fileStream);
            fileStream.Close();
            IsZipArchive(path);

            state = PageState.Browser;
            SetText("Finished!\nPress any key to continue");
        }
        catch (Exception ex)
        {
            Main.Log(ex, LogLevel.Error);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private async Task FetchOnlinePacks()
    {
        Main.Log("Fetching remote texturepacks...", LogLevel.Message);
        try
        {
            using var client = new HttpClient();
            string result = await client.GetStringAsync(Paths.BASE_URL + "/OnlinePacks.json");
            Items = [.. Newtonsoft.Json.JsonConvert.DeserializeObject<TexturePackMeta[]>(result)];
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to get online packs {ex}", LogLevel.Error);
            SetText($"Couldn't get online texturepacks. \n {ex}");
            throw ex;
        }
    }

    // copied from google ai overview
    public static bool IsZipArchive(string filePath)
    {
        if (!File.Exists(filePath)) return false;

        if (new FileInfo(filePath).Length < 4) return false;

        byte[] buffer = new byte[4];
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fs.Read(buffer, 0, 4);
        }

        return buffer[0] == 0x50 && buffer[1] == 0x4B &&
               buffer[2] == 0x03 && buffer[3] == 0x04;
    }

    public enum PageState
    {
        Browser,
        Confirmation,
    }
}
