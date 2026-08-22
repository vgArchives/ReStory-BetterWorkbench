using System.Collections.Generic;
using UnityEngine;

namespace ReStoryBetterWorkbench;

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

    private static readonly Dictionary<SystemLanguage, string> ServicesByLanguage = new()
    {
        { SystemLanguage.English, "Services" },
        { SystemLanguage.Russian, "Услуги" },
        { SystemLanguage.Portuguese, "Serviços" },
        { SystemLanguage.Spanish, "Servicios" },
        { SystemLanguage.French, "Services" },
        { SystemLanguage.German, "Leistungen" },
        { SystemLanguage.Japanese, "サービス" },
        { SystemLanguage.Korean, "서비스" },
        { SystemLanguage.ChineseSimplified, "服务" },
        { SystemLanguage.Chinese, "服务" }
    };

    private static readonly Dictionary<SystemLanguage, string> DaysByLanguage = new()
    {
        { SystemLanguage.English, "Days" },
        { SystemLanguage.Russian, "Дней" },
        { SystemLanguage.Portuguese, "Dias" },
        { SystemLanguage.Spanish, "Días" },
        { SystemLanguage.French, "Jours" },
        { SystemLanguage.German, "Tage" },
        { SystemLanguage.Japanese, "日数" },
        { SystemLanguage.Korean, "일수" },
        { SystemLanguage.ChineseSimplified, "天数" },
        { SystemLanguage.Chinese, "天数" }
    };

    internal static string Organize { get; private set; } = OrganizeByLanguage[SystemLanguage.English];

    internal static string Highlights { get; private set; } = HighlightsByLanguage[SystemLanguage.English];

    internal static string Services { get; private set; } = ServicesByLanguage[SystemLanguage.English];

    internal static string Days { get; private set; } = DaysByLanguage[SystemLanguage.English];

    internal static void SetLanguage(SystemLanguage language)
    {
        Organize = Translate(OrganizeByLanguage, language);
        Highlights = Translate(HighlightsByLanguage, language);
        Services = Translate(ServicesByLanguage, language);
        Days = Translate(DaysByLanguage, language);
    }

    private static string Translate(Dictionary<SystemLanguage, string> byLanguage, SystemLanguage language) =>
        byLanguage.TryGetValue(language, out string translated) ? translated : byLanguage[SystemLanguage.English];
}
