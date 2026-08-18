using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ReStoryBetterWorkbench;

internal static class UpdateCheck
{
    private const string GitHubLatestReleaseApi =
        "https://api.github.com/repos/vgArchives/ReStory-BetterWorkbench/releases/latest";

    private const string NexusModPage = "https://www.nexusmods.com/restorychillelectronicrepairs/mods/42";

    // Fixed baseline for the self-check cases, deliberately not PluginVersion: releasing must not move it.
    private const string SelfCheckVersion = "1.2.0";

    private const int RequestTimeoutSeconds = 10;

#pragma warning disable CS0649
    [Serializable]
    private class LatestRelease
    {
        public string tag_name;
    }
#pragma warning restore CS0649

    internal static IEnumerator Run(string currentVersion)
    {
        using UnityWebRequest request = UnityWebRequest.Get(GitHubLatestReleaseApi);

        request.SetRequestHeader("User-Agent", BetterWorkbenchPlugin.PluginName);

        request.timeout = RequestTimeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Log.Debug($"Update check failed: {request.error}");

            yield break;
        }

        if (!TryGetNewer(request.downloadHandler.text, currentVersion, out Version latestVersion))
            yield break;

        Log.Warning($"Update available: v{latestVersion} (installed: v{currentVersion}).");
        Log.Warning($"Get it at {NexusModPage}");
    }

    internal static bool SelfCheck()
    {
        (string CaseName, string ReleaseJson, bool IsExpectedNewer)[] cases =
        {
            ("newer release", Release("v1.3.0"), true),
            ("newer patch release", Release("v1.2.1"), true),
            ("same release", Release("v1.2.0"), false),
            ("older release", Release("v1.1.0"), false),
            ("tag without the v prefix", Release("1.3.0"), true),
            ("tag with a build number", Release("v1.2.0.1"), true),
            ("pre-release tag", Release("v1.3.0-beta"), false),
            ("rate limit reply", "{\"message\":\"API rate limit exceeded\"}", false),
            ("error page instead of json", "<html>502 Bad Gateway</html>", false),
            ("empty body", string.Empty, false)
        };

        bool isValid = true;

        foreach ((string caseName, string releaseJson, bool isExpectedNewer) in cases)
        {
            isValid &= Expect(caseName, releaseJson, isExpectedNewer);
        }

        return isValid;
    }

    private static string Release(string tag) => $"{{\"tag_name\":\"{tag}\"}}";

    private static bool Expect(string caseName, string releaseJson, bool isExpectedNewer)
    {
        bool isNewer = TryGetNewer(releaseJson, SelfCheckVersion, out _);

        if (isNewer == isExpectedNewer)
            return true;

        Log.Error($"Self-check FAILED: {caseName} reported newer={isNewer}, expected {isExpectedNewer}.");

        return false;
    }

    private static bool TryGetNewer(string releaseJson, string currentVersion, out Version latestVersion)
    {
        string latestReleaseTag;

        try
        {
            latestReleaseTag = JsonUtility.FromJson<LatestRelease>(releaseJson)?.tag_name;
        }
        catch (ArgumentException)
        {
            latestVersion = null;

            return false;
        }

        return Version.TryParse(latestReleaseTag?.TrimStart('v', 'V'), out latestVersion)
               && latestVersion > new Version(currentVersion);
    }
}
