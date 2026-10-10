using BaseLib.Utils.Attributes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

// 小强化独立叠层，换伙伴或入狱都不会清空；只随本次战斗结束而清理。
[CustomID("OTTO_DOG_BOOST")] public sealed class DogBoostPower : OttoBuff;
[CustomID("OTTO_PIG_BOOST")] public sealed class PigBoostPower : OttoBuff;
[CustomID("OTTO_MARS_BOOST")] public sealed class MarsBoostPower : OttoBuff;

/// <summary>入狱是召唤规则标记，不作为可被人工制品阻挡的战斗减益。</summary>
public abstract class ZooPrisonPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
}
[CustomID("OTTO_DOG_PRISON")] public sealed class DogPrisonPower : ZooPrisonPower;
[CustomID("OTTO_PIG_PRISON")] public sealed class PigPrisonPower : ZooPrisonPower;
[CustomID("OTTO_MARS_PRISON")] public sealed class MarsPrisonPower : ZooPrisonPower;

/// <summary>跟踪整条攻击命令，而非单次伤害；这样三连击的三段都会被挡住。</summary>
[CustomID("OTTO_PIG_GUARD")]
public sealed class PigGuardPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    private AttackCommand? pendingAttack;
    private bool intercepted;
    public override Task BeforeAttack(AttackCommand command)
    {
        if (pendingAttack is null && command.Attacker?.Side != Owner.Side && Owner.HasPower<ShanNiRuoPower>())
        { pendingAttack = command; intercepted = false; }
        return Task.CompletedTask;
    }
    private bool Covers(Creature? target, Creature? dealer) => target == Owner && pendingAttack is not null &&
        dealer == pendingAttack.Attacker && Owner.HasPower<ShanNiRuoPower>();
    public override Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? source)
    {
        if (Covers(target, dealer)) intercepted = true;
        return Task.CompletedTask;
    }
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? source) =>
        Covers(target, dealer) ? 0 : decimal.MaxValue;
    public override async Task AfterAttack(PlayerChoiceContext ctx, AttackCommand command)
    {
        if (command != pendingAttack) return;
        pendingAttack = null;
        // 敌人攻击了队友时不消耗保护；真正打到 OTTO 后才将山泥若送入监狱。
        if (!intercepted) return;
        await Zoo.Imprison(ctx, Owner, 1, null);
        await PowerCmd.Remove(this);
    }
}

/// <summary>全部现有战斗牌先随机一次；新生成与每次抽到的牌重新随机，使用同步种子流。</summary>
[CustomID("OTTO_WHY_POWER")]
public sealed class WhyPower : OttoBuff
{
    public void Randomize(CardModel card)
    {
        if (card.Owner != Owner.Player || card.EnergyCost.CostsX || card.EnergyCost.Canonical < 0 ||
            card.Keywords.Contains(CardKeyword.Unplayable)) return;
        card.EnergyCost.SetThisCombat(Owner.Player!.RunState.Rng.CombatEnergyCosts.NextInt(4));
        NCard.FindOnTable(card)?.PlayRandomizeCostAnim();
    }
    public override Task AfterApplied(Creature? applier, CardModel? source)
    {
        foreach (var card in Owner.Player!.PlayerCombatState!.AllCards.ToArray()) Randomize(card);
        HiddenIntentVisuals.Sync(CombatState);
        return Task.CompletedTask;
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.CombatState is { } state) HiddenIntentVisuals.Sync(state);
        return Task.CompletedTask;
    }
    public override decimal ModifyHandDraw(Player player, decimal count) => count + (player == Owner.Player ? Amount : 0);
    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    { Randomize(card); return Task.CompletedTask; }
    public override Task AfterCardEnteredCombat(CardModel card)
    { Randomize(card); return Task.CompletedTask; }
}

/// <summary>只隐藏当前玩家屏幕上的图标，不改敌人 AI 或队友画面，也不影响其人之道读取真实意图。</summary>
public static class HiddenIntentVisuals
{
    public static bool IsHidden(ICombatState? state) => LocalContext.GetMe(state)?.Creature.HasPower<WhyPower>() == true;
    public static void Sync(ICombatState state)
    {
        foreach (var enemy in state.Enemies)
            if (enemy.GetCreatureNode() is { } node) Apply(node);
    }
    public static void Apply(NCreature node)
    {
        if (node.Entity?.Monster is not null && node.IntentContainer is { } container)
            container.Visible = !IsHidden(node.Entity.CombatState);
    }
}
[HarmonyPatch(typeof(NCreature), nameof(NCreature.UpdateIntent))]
internal static class OttoHideIntentPatch
{
    [HarmonyPostfix] private static void Hide(NCreature __instance) => HiddenIntentVisuals.Apply(__instance);
}

/// <summary>在实际扣血阶段限制一轮累计损失，格挡与减伤正常先算；直接击杀仍由原版处理。</summary>
[CustomID("OTTO_HOU_GUOYU")]
public sealed class HouGuoyuPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    public decimal Spent { get; private set; }
    public override LocString Description
    {
        get
        {
            var text = new LocString("powers", Id.Entry + ".description");
            text.Add("Remaining", Math.Max(0, 10 - Spent));
            return text;
        }
    }
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? source) =>
        target == Owner ? Math.Min(amount, Math.Max(0, 10 - Spent)) : amount;
    public override Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? source)
    {
        if (target == Owner) Spent += Math.Max(0, result.UnblockedDamage);
        return Task.CompletedTask;
    }
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == Owner.Side && participants.Contains(Owner)) Spent = 0;
        return Task.CompletedTask;
    }
}

/// <summary>眩晕层数对应的敌方行动使用眩晕招式，结束后回到被打断的原招式；不让敌人暗中执行强化或召唤。</summary>
[CustomID("OTTO_KING_STUN")]
public sealed class KingStunPower : OttoBuff
{
    public override PowerType Type => PowerType.Debuff;
    private MoveState? stun;
    private MoveState? resume;
    public override Task AfterApplied(Creature? applier, CardModel? source)
    {
        var monster = Owner.Monster;
        if (monster?.NextMove is null || monster.MoveStateMachine is null) return Task.CompletedTask;
        resume = monster.NextMove;
        stun = new MoveState("OTTO_KING_STUN", SkipMove, new StunIntent()) { MustPerformOnceBeforeTransitioning = true };
        stun.FollowUpState = stun;
        // 注册返回路径，兼容敌人正在使用动态创建的招式（例如原版临时眩晕）。
        monster.MoveStateMachine.States.TryAdd(resume.Id, resume);
        monster.MoveStateMachine.States[stun.Id] = stun;
        monster.SetMoveImmediate(stun, forceTransition: true);
        return Task.CompletedTask;
    }
    private async Task SkipMove(IReadOnlyList<Creature> targets)
    {
        if (Amount <= 1 && stun is not null) stun.FollowUpState = resume;
        await PowerCmd.Decrement(this);
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        if (stun is not null) stun.FollowUpState = resume;
        return Task.CompletedTask;
    }
}
