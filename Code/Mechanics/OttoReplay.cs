using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Utils;
using HarmonyLib;

namespace OttoMod;

/// <summary>追加结算用临时副本，不搬动原卡；调用真实自动出牌流程，正常派发攻击/技能钩子。</summary>
public static class OttoReplay
{
    internal static readonly SpireField<CardModel, bool> ExtraExecution = new(() => false);
    public static bool CanAutoPlay(CardModel card) => !card.Keywords.Contains(CardKeyword.Unplayable) &&
        card.ShouldPlay(card, AutoPlayType.Default) &&
        Hook.ShouldPlay(card.Owner.Creature.CombatState!, card, out _, AutoPlayType.Default);

    public static Creature? Target(CardModel card, Creature? preferred)
    {
        var state = card.Owner.Creature.CombatState!;
        if (card.TargetType == TargetType.AnyEnemy)
            return preferred is { IsHittable: true } && preferred.Side != card.Owner.Creature.Side && card.IsValidTarget(preferred) ? preferred :
                card.Owner.RunState.Rng.CombatTargets.NextItem(state.GetOpponentsOf(card.Owner.Creature).Where(c => c.IsHittable && card.IsValidTarget(c)));
        if (card.TargetType == TargetType.AnyAlly)
            return preferred is { IsAlive: true } && preferred.Side == card.Owner.Creature.Side && preferred != card.Owner.Creature && card.IsValidTarget(preferred) ? preferred :
                card.Owner.RunState.Rng.CombatTargets.NextItem(state.Allies.Where(c => c.IsPlayer && c.IsAlive && c != card.Owner.Creature && card.IsValidTarget(c)));
        return null;
    }

    public static async Task PlayCopy(PlayerChoiceContext ctx, CardModel original, Creature? preferred)
    {
        if (CombatManager.Instance.IsOverOrEnding || original.Owner.Creature.IsDead) return;
        var copy = original.CreateClone();
        // X值沿用原先支付的数量，追加本身不支付资源；免费标记不能把已捕获的X清成零。
        if (original.EnergyCost.CostsX) copy.EnergyCost.CapturedXValue = original.EnergyCost.CapturedXValue;
        copy.LastStarsSpent = original.LastStarsSpent;
        ExtraExecution.Set(copy, true);
        try
        {
            // 放入战斗牌堆后才拥有完整CombatState，条件牌（例如其人之道）也能正常判断。
            await CardPileCmd.Add(copy, PileType.Play);
            if (!CanAutoPlay(copy)) return;
            var target = Target(copy, preferred);
            if (copy.TargetType is TargetType.AnyEnemy or TargetType.AnyAlly && target is null) return;
            await CardCmd.AutoPlay(ctx, copy, target, skipXCapture: true);
        }
        finally
        {
            // 临时副本在结算后移出，原牌在手牌、弃牌堆或消耗堆中的位置均保持原样。
            if (copy.Pile is not null) await CardPileCmd.RemoveFromCombat(copy);
        }
    }
}

/// <summary>额外执行明确为一次；不把副本携带的原生重放次数再次乘入，避免两种重放指数膨胀。</summary>
[HarmonyPatch(typeof(CardModel), "GeneratePlayCount")]
internal static class OttoExtraExecutionCountPatch
{
    [HarmonyPrefix]
    private static bool Count(CardModel __instance, ref Task<int> __result)
    {
        if (!OttoReplay.ExtraExecution.Get(__instance)) return true;
        __result = Task.FromResult(1);
        return false;
    }
}
