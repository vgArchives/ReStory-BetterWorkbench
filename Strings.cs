using System.Collections.Generic;
using UnityEngine;

namespace RestoryBenchOrganizer;

internal static class Strings
{
    private static readonly Dictionary<SystemLanguage, string> OrganizeByLanguage = new()
    {
        { SystemLanguage.English, "Organize" },
        { SystemLanguage.Russian, "Упорядочить" },
        { SystemLanguage.Portuguese, "Organizar" },
        { SystemLanguage.Spanish, "Organizar" },
        { SystemLanguage.French, "Organiser" },
        { SystemLanguage.German, "Ordnen" },
        { SystemLanguage.Japanese, "整理" },
        { SystemLanguage.Korean, "정리" },
        { SystemLanguage.ChineseSimplified, "整理" },
        { SystemLanguage.Chinese, "整理" }
    };

    private static readonly Dictionary<SystemLanguage, string> HighlightsByLanguage = new()
    {
        { SystemLanguage.English, "Highlights" },
        { SystemLanguage.Russian, "Подсветка" },
        { SystemLanguage.Portuguese, "Destaques" },
        { SystemLanguage.Spanish, "Resaltado" },
        { SystemLanguage.French, "Surbrillance" },
        { SystemLanguage.German, "Hervorhebung" },
        { SystemLanguage.Japanese, "ハイライト" },
        { SystemLanguage.Korean, "하이라이트" },
        { SystemLanguage.ChineseSimplified, "高亮" },
        { SystemLanguage.Chinese, "高亮" }
    };

    internal static string Organize { get; private set; } = OrganizeByLanguage[SystemLanguage.English];

    internal static string Highlights { get; private set; } = HighlightsByLanguage[SystemLanguage.English];

    internal static void SetLanguage(SystemLanguage language)
    {
        Organize = Translate(OrganizeByLanguage, language);
        Highlights = Translate(HighlightsByLanguage, language);
    }

    private static string Translate(Dictionary<SystemLanguage, string> byLanguage, SystemLanguage language) =>
        byLanguage.TryGetValue(language, out string translated) ? translated : byLanguage[SystemLanguage.English];
}
