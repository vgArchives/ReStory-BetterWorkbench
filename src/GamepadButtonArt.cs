using System.Collections.Generic;
using UnityEngine;

namespace ReStoryBetterWorkbench;

internal static class GamepadButtonArt
{
    private const string Fallback = "the button name shows in a key frame instead";

    private const float PixelsPerUnit = 17f;

    private static readonly Dictionary<BenchGamepadButton, string> ArtNames = new()
    {
        { BenchGamepadButton.LeftStickClick, "Left_Stick_Click" },
        { BenchGamepadButton.RightStickClick, "Right_Stick_Click" }
    };

    private static readonly Dictionary<BenchGamepadButton, Sprite> IconsByButton = new();

    internal static Sprite Icon(BenchGamepadButton button)
    {
        if (IconsByButton.TryGetValue(button, out Sprite cachedIcon))
            return cachedIcon;

        Sprite icon = ArtNames.TryGetValue(button, out string artName) ? Load(artName) : null;

        IconsByButton[button] = icon;
        return icon;
    }

    private static Sprite Load(string artName)
    {
        string resourceName = $"ReStoryBetterWorkbench.Assets.{artName}.png";

        return EmbeddedArt.LoadSprite(resourceName, artName, PixelsPerUnit, Vector4.zero, Fallback);
    }
}
