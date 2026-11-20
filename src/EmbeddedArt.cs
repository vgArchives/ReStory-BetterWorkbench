using System.IO;
using System.Reflection;
using UnityEngine;

namespace ReStoryBetterWorkbench;

internal static class EmbeddedArt
{
    internal static Sprite LoadSprite(string resourceName, string artName, float pixelsPerUnit, Vector4 border,
        string fallback)
    {
        byte[] png = ReadResource(resourceName);

        if (png == null)
        {
            Log.Warning($"Art {resourceName} is missing; {fallback}.");
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
        {
            name = artName,
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        if (!texture.LoadImage(png))
        {
            Log.Warning($"Art {resourceName} could not be decoded; {fallback}.");
            Object.Destroy(texture);
            return null;
        }

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);

        sprite.name = artName;
        sprite.hideFlags = HideFlags.HideAndDontSave;

        Log.Debug($"Art {artName} loaded at {texture.width}x{texture.height}.");
        return sprite;
    }

    private static byte[] ReadResource(string resourceName)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

        if (stream == null)
            return null;

        using MemoryStream buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }
}
