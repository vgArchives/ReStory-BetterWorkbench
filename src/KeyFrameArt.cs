using UnityEngine;

namespace ReStoryBetterWorkbench;

internal static class KeyFrameArt
{
    private const string ResourceName = "ReStoryBetterWorkbench.Assets.KeyFrame.png";
    private const string ArtName = "ReStoryKeyFrame";
    private const string Fallback = "key names show without a frame";

    private const float PixelsPerUnit = 800f;

    private static readonly Vector4 Border = new(50f, 50f, 50f, 30f);

    private static Sprite _frame;
    private static bool _hasTriedToLoad;

    internal static Sprite Frame()
    {
        if (_hasTriedToLoad)
            return _frame;

        _hasTriedToLoad = true;
        _frame = EmbeddedArt.LoadSprite(ResourceName, ArtName, PixelsPerUnit, Border, Fallback);

        return _frame;
    }
}
