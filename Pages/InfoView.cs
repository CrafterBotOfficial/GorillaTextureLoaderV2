using ComputerInterface.Enumerations;
using ComputerInterface.Models;

namespace GorillaTextureLoader.Pages;

public class InfoView : ComputerView
{
    private string text = "...";

    public override void OnViewShown(object[] arguments)
    {
        if (arguments.Length != 1 || arguments[0] is not string viewText)
        {
            Main.Log("Impossible error occured. No info for view", BepInEx.Logging.LogLevel.Error);
            ReturnToMainMenu();
            return;
        }

        text = viewText;
    }

    protected override string GetViewText() => text;

    public override void OnButtonPressed(EKeyboardButton pressedButton)
    {
        ShowView<SelectView>();
    }
}
