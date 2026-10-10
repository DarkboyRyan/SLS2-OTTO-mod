using BaseLib.Patches.Content;
using BaseLib.Utils;
using BaseLib.Utils.Attributes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace OttoMod;

/// <summary>正式关键词会随战斗中的卡牌复制，也能显示在其他职业被尊尼获加赋予属性的牌上。</summary>
public static class OttoKeywords
{
    [CustomEnum("OTTO_CAN"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Can;
    [CustomEnum("OTTO_HAND"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Hand;
    [CustomEnum("OTTO_DELAYED"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Delayed;
}

/// <summary>每位玩家各自的战斗记录；隐藏技术性状态，字段仅随本场战斗状态存在，不写入永久牌组。</summary>
[CustomID("OTTO_CARD_RULES")]
public sealed class OttoCardRulesPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
    public int CansPlayed { get; set; }
    public int HandsPlayed { get; set; }
    // 牛魔记录整场“打出的罐”；这里单独记录本回合真正从手牌边缘触发的罐。
    public int CanTriggersThisTurn { get; private set; }
    public int ManualPlaysThisTurn { get; private set; }
    public CardModel? LastManualCard { get; private set; }
    public Creature? LastManualTarget { get; private set; }

    public Task RecordCanTrigger()
    {
        CanTriggersThisTurn++;
        return Task.CompletedTask;
    }

    public static async Task<OttoCardRulesPower> Ensure(CardModel card)
    {
        var creature = card.Owner.Creature;
        if (creature.GetPower<OttoCardRulesPower>() is { } existing) return existing;
        await PowerCmd.Apply<OttoCardRulesPower>(new ThrowingPlayerChoiceContext(), creature, 1, creature, card);
        return creature.GetPower<OttoCardRulesPower>()!;
    }

    public async Task RecordPlay(CardPlay play)
    {
        // 原版重放仍可能携带IsAutoPlay=false；只有一组出牌的第一段计为一次手动操作。
        if (!play.IsAutoPlay && play.IsFirstInSeries)
        {
            ManualPlaysThisTurn++;
            if (play.Card.Type is CardType.Attack or CardType.Skill && !play.Card.EnergyCost.CostsX && !play.Card.HasStarCostX)
            { LastManualCard = play.Card; LastManualTarget = play.Target; }
        }
        // 在卡牌效果前记数：即使本张牌击杀最后一个敌人也不会漏计。
        if (play.Card.Keywords.Contains(OttoKeywords.Can))
        {
            CansPlayed++;
            if (Owner.GetPower<FreeCanPower>() is { } free) await PowerCmd.Remove(free);
        }
        if (play.Card is OneHand) HandsPlayed |= 1;
        if (play.Card is AminoHand) HandsPlayed |= 2;
        RefreshCosts();
    }

    public void ResetCans()
    {
        // 牛魔共享计数，任一张打出后所有牛魔一起恢复原费用。
        CansPlayed = 0;
        RefreshCosts();
    }

    private void RefreshCosts()
    {
        foreach (var card in Owner.Player!.PlayerCombatState!.AllPiles.SelectMany(p => p.Cards))
            if (card is BullDemon || card.Keywords.Contains(OttoKeywords.Can)) card.InvokeEnergyCostChanged();
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner || card is not BullDemon || CansPlayed == 0) return false;
        modifiedCost = Math.Max(0, originalCost - CansPlayed);
        return true;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card.Owner.Creature != Owner || !card.Keywords.Contains(OttoKeywords.Delayed);

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner || !OttoFreeCostRules.ThisTurn.Get(card)) return false;
        // 本回合免费在全局费用层兜底：复制牌同回合再次抽到并被随机改费时，显示也仍为零。
        modifiedCost = 0;
        return true;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side != Owner.Side || !participants.Contains(Owner)) return Task.CompletedTask;
        HandsPlayed = 0;
        CanTriggersThisTurn = 0;
        ManualPlaysThisTurn = 0;
        LastManualCard = null;
        LastManualTarget = null;
        // 只解锁哇沃本轮抽到的牌；它们获得的保留在本场战斗中继续存在。
        foreach (var card in Owner.Player!.PlayerCombatState!.AllPiles.SelectMany(p => p.Cards))
            card.RemoveKeyword(OttoKeywords.Delayed);
        return Task.CompletedTask;
    }
}

/// <summary>下张易拉罐零费：牌在手中时即显示零费，打出时消耗；艾克儿自身会在效果中重新给予。</summary>
[CustomID("OTTO_FREE_CAN")]
public sealed class FreeCanPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner || !card.Keywords.Contains(OttoKeywords.Can)) return false;
        modifiedCost = 0;
        return true;
    }
    public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner || !card.Keywords.Contains(OttoKeywords.Can)) return false;
        modifiedCost = 0;
        return true;
    }
}

/// <summary>大旋风跨职业生成牌的整回合免费标记；随战斗复制继承，回合清理时移除。</summary>
public static class OttoFreeCostRules
{
    internal static readonly SpireField<CardModel, bool> ThisTurn = new SpireField<CardModel, bool>(() => false).CopyOnClone();
    public static void MakeFreeThisTurn(CardModel card)
    {
        ThisTurn.Set(card, true);
        card.EnergyCost.SetThisTurn(0);
        card.SetStarCostThisTurn(0);
    }
    internal static bool IsFree(CardModel card) => ThisTurn.Get(card) ||
        (card.IsMutable && card.Owner is not null && card.Owner.Creature.HasPower<FreeCanPower>() && card.Keywords.Contains(OttoKeywords.Can));
}

/// <summary>原版X费会绕过普通减费；明确让免费X牌实际花费0，X也按实际支付的0结算。</summary>
[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.GetAmountToSpend))]
internal static class OttoFreeXEnergyPatch
{
    [HarmonyPostfix]
    private static void Free(CardModel ____card, ref int __result)
    {
        if (OttoFreeCostRules.IsFree(____card)) __result = 0;
    }
}

/// <summary>跨职业零费包含星星，避免OTTO没有星星而无法打出生成牌；同时处理星星X费。</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetStarCostWithModifiers))]
internal static class OttoFreeStarsPatch
{
    [HarmonyPostfix]
    private static void Free(CardModel __instance, ref int __result)
    {
        if (__result > 0 && OttoFreeCostRules.IsFree(__instance)) __result = 0;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.EndOfTurnCleanup))]
internal static class OttoFreeTurnCleanupPatch
{
    // 只清除大旋风当前回合标记；艾克儿的“下一罐免费”由独立状态保留到真正出牌。
    [HarmonyPostfix]
    private static void Clear(CardModel __instance) => OttoFreeCostRules.ThisTurn.Set(__instance, false);
}

/// <summary>手牌边缘位置必须在原版把卡放进出牌区之前获取，不能在抽牌或弃牌后倒推。</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class OttoEdgeSnapshotPatch
{
    internal static readonly SpireField<CardModel, bool> WasAtEdge = new(() => false);
    // 不随复制继承：每次真正开始打出时重新捕获，排除本牌效果后自己的触发。
    internal static readonly SpireField<CardModel, int> PriorTriggers = new(() => 0);
    internal static readonly SpireField<CardModel, bool> IsManual = new(() => false);
    [HarmonyPrefix]
    private static void BeforePlay(CardModel __instance, bool isAutoPlay)
    {
        var hand = __instance.Owner.PlayerCombatState?.Hand.Cards;
        IsManual.Set(__instance, !isAutoPlay);
        PriorTriggers.Set(__instance, __instance.Owner.Creature.GetPower<OttoCardRulesPower>()?.CanTriggersThisTurn ?? 0);
        WasAtEdge.Set(__instance, !isAutoPlay && hand is { Count: > 0 } &&
            (__instance == hand[0] || __instance == hand[^1]));
    }
}

/// <summary>等待原版出牌前钩子完成后处理新机制；仅为 OTTO 卡或带易拉罐属性的牌建立记录。</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class OttoBeforePlayPatch
{
    [HarmonyPostfix]
    private static void AfterOriginal(ref Task __result, CardPlay cardPlay) => __result = Record(__result, cardPlay);
    private static async Task Record(Task original, CardPlay play)
    {
        await original;
        if (play.Card is not OttoCard && !play.Card.Keywords.Contains(OttoKeywords.Can) &&
            play.Card.Owner.Character is not OttoCharacter && !play.Card.Owner.Creature.HasPower<OttoCardRulesPower>()) return;
        var rules = await OttoCardRulesPower.Ensure(play.Card);
        await rules.RecordPlay(play);
    }
}

/// <summary>易拉罐在牌自身效果结算后抽牌；自动出牌不属于从手牌边缘打出。</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
internal static class OttoAfterPlayPatch
{
    [HarmonyPostfix]
    private static void AfterOriginal(ref Task __result, PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        __result = Draw(__result, choiceContext, cardPlay);
    private static async Task Draw(Task original, PlayerChoiceContext ctx, CardPlay play)
    {
        await original;
        // 翔子只要求手动打出易拉罐牌，不要求位于手牌边缘；重放不重复触发。
        if (!play.IsAutoPlay && play.IsFirstInSeries && play.Card.Keywords.Contains(OttoKeywords.Can) &&
            play.Card.Owner.Creature.GetPower<FirstCanEnergyPower>() is { } energy)
            await energy.Trigger();
        if (!play.IsAutoPlay && play.IsFirstInSeries && play.Card.Keywords.Contains(OttoKeywords.Can) && OttoEdgeSnapshotPatch.WasAtEdge.Get(play.Card))
        {
            // 达成边缘条件即为触发；即使禁抽或满手而没有抽到牌，触发本身仍计数。
            await (await OttoCardRulesPower.Ensure(play.Card)).RecordCanTrigger();
            await CardPileCmd.Draw(ctx, 1, play.Card.Owner);
            if (play.Card.Owner.Creature.GetPower<OttoLaughterPower>() is { } laughter)
                await laughter.Repeat(ctx, play);
        }
    }
}
