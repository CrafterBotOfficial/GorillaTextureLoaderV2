using System.Text;
using ComputerInterface.Enumerations;
using ComputerInterface.Extensions;
using ComputerInterface.Models;

namespace GorillaTextureLoader.Views;

public class InfoView : ComputerView
{
    private string header = "...";
    private string text = "...";
    private string headerColor = "green";

    public override void OnViewShown(object[] arguments)
    {
        if (arguments.Length < 2 || arguments[0] is not string viewHeader || arguments[1] is not string viewText)
        {
            Main.Log("Impossible error occured. No info for view", BepInEx.Logging.LogLevel.Error);
            ReturnToMainMenu();
            return;
        }

        header = viewHeader;
        text = viewText;
        if (arguments.Length == 3 && arguments[2] is string color) 
            headerColor = color;
        else headerColor = "green";
    }

    protected override string GetViewText() => new StringBuilder()
        .BeginCenter()
        .Append("<color=").Append(headerColor).Append('>')
        .AppendSize(header, 110)
        .AppendLine()
        .EndColor()
        .AppendLine(new string('=', ScreenWidth / 2))
        .AppendLines(1)
        .EndAlign()
        .AppendSize(text, 80)
        .ToString();

    public override void OnButtonPressed(EKeyboardButton pressedButton)
    {
        ShowView<SelectView>();
    }
}
