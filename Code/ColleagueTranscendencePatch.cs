using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace OttoMod;

/// <summary>复用古老之牙的选项、替换、强化继承及附魔继承流程。</summary>
[HarmonyPatch(typeof(ArchaicTooth), "get_TranscendenceUpgrades")]
internal static class ColleagueTranscendencePatch
{
    [HarmonyPostfix]
    private static void Postfix(Dictionary<ModelId, CardModel> __result) =>
        __result[ModelDb.Card<CallFriends>().Id] = ModelDb.Card<ColleagueBenefactor>();
}
