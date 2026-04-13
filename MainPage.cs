using System;
using GorillaNetworking;
using Jerald;
using UnityEngine;

[assembly: AutoRegister]
namespace GorillaTextureLoader;

[AutoRegister]
public class MainPage : Page
{
    public static Action Update;

    public override string PageName => "TextureLoader";

    private int index;

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
        for (int i = 0; i < metas.Length; i++)
            result += $"\n[{(instance.Current == metas[i] ? 'x' : ' ')}] {metas[i].Name} {(i == index ? " <--" : "")}";

        return result;
    }

    public override void OnKeyPress(GorillaKeyboardBindings key)
    {
        if (key == GorillaKeyboardBindings.enter)
        {
            TextureController.Instance.LoadPack(TextureController.Instance.PackMetas.Result[index]);
        }
        else
        {
            index += key == GorillaKeyboardBindings.W ? -1 : key == GorillaKeyboardBindings.S ? 1 : 0;
            index = Mathf.Clamp(index, 0, TextureController.Instance.PackMetas.Result.Length - 1);
        }
        UpdateContent();
    }
}
