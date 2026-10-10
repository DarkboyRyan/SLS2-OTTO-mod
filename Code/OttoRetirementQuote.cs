using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.addons.mega_text;

namespace OttoMod;

[HarmonyPatch]
internal static class OttoRetirementQuote
{
    private static string Quote => new LocString("characters", "OTTO_CHARACTER.abandonQuote").GetFormattedText();

    [HarmonyPatch(typeof(NRunHistory), nameof(NRunHistory.GetDeathQuote))]
    [HarmonyPrefix]
    private static bool GetQuote(ModelId characterId, GameOverType gameOverType, ref string __result)
    {
        if (characterId != ModelDb.Character<OttoCharacter>().Id || gameOverType != GameOverType.AbandonedRun) return true;
        __result = Quote;
        return false;
    }

    // 历史页面独立生成结局语句，也同步替换 OTTO 的放弃语句。
    [HarmonyPatch(typeof(NRunHistory), "LoadDeathQuote")]
    [HarmonyPostfix]
    private static void HistoryQuote(RunHistory history, ModelId characterId, MegaRichTextLabel ____deathQuoteLabel)
    {
        if (!history.Win && history.WasAbandoned && characterId == ModelDb.Character<OttoCharacter>().Id)
            ____deathQuoteLabel.Text = Quote;
    }
}
