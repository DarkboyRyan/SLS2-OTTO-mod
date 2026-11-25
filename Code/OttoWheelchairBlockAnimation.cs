#if OTTO_WHEELCHAIR_TEST
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;

namespace OttoMod;

// GainBlock 本身不触发人物动画；只给本测试构建中的格挡技能补上推轮动作。
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class OttoWheelchairBlockAnimation
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result, CardPlay cardPlay) =>
        __result = Animate(__result, cardPlay);

    private static async Task Animate(Task original, CardPlay play)
    {
        await original;
        var card = play.Card;
        if (!play.IsFirstInSeries || NonInteractiveMode.IsActive ||
            card.Owner.Character is not OttoCharacter || card.Owner.Creature.IsDead ||
            card.Type != CardType.Skill || !card.GainsBlock) return;
        await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
    }
}
#endif
