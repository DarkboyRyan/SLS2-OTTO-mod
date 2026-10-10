using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>保存整场唯一一次入狱保护；触发后仍保留状态，重复打出不能补回机会。</summary>
[CustomID("OTTO_LAW_ABIDING_POWER")]
public sealed class LawAbidingPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    public bool Used { get; set; }
    public void Use() { Used = true; Flash(); }
    public override LocString Description => new("powers", Id.Entry + (Used ? ".usedDescription" : ".description"));
}

/// <summary>肯德基的层数表示当前最高倍率；到下次自己的回合开始移除。</summary>
[CustomID("OTTO_KFC_POWER")]
public sealed class KfcPower : OttoBuff
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == Owner.Side && participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}

/// <summary>1为力量模式，2为敏捷模式。切换直接改值，既不叠乘也不修改已有属性。</summary>
[CustomID("OTTO_MARS_MODE")]
public sealed class MarsModePower : OttoBuff
{
    public override LocString Description => new("powers", Id.Entry + (Amount == 2 ? ".dexterityDescription" : ".description"));
}

/// <summary>每个实际消耗格挡并完全挡住的敌方攻击段独立反击；非攻击伤害不触发。</summary>
[CustomID("OTTO_DOG_COUNTER")]
public sealed class DogCounterPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override async Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? source)
    {
        if (target != Owner || Owner.IsDead || dealer is null || dealer.Side == Owner.Side || !dealer.IsHittable ||
            !props.IsPoweredAttack() || result.BlockedDamage <= 0 || !result.WasFullyBlocked ||
            Zoo.Companion(Owner, 0) is not XuanDogPower dog) return;
        Flash();
        // 不调用完整伙伴行动，避免反击另外加格挡并造成递归。
        await CreatureCmd.Damage(ctx, dealer, dog.EffectAmount * 3, ValueProp.Unpowered, Owner);
    }
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == Owner.Side && participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}

/// <summary>只在伤害结算后、正式死亡前救命；直接Kill不经过本钩子，所以不能被复活绕过。</summary>
[CustomID("OTTO_LAST_XUAN")]
public sealed class LastXuanPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    private bool rescuing;
    public override async Task AfterDamageGiven(PlayerChoiceContext ctx, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? source)
    {
        if (target != Owner || !result.WasTargetKilled || Owner.CurrentHp > 0 || Owner.MaxHp <= 0 || rescuing ||
            Zoo.Companion(Owner, 0) is null || Zoo.IsImprisoned(Owner, 0)) return;
        rescuing = true;
        try
        {
            Flash();
            // 先恢复生命，再应用监狱状态，避免引擎拒绝给死亡单位施加状态。
            await CreatureCmd.SetCurrentHp(Owner, Math.Ceiling(Owner.MaxHp / 2m));
            await Zoo.Imprison(ctx, Owner, 0, source);
        }
        finally { rescuing = false; }
    }
}

/// <summary>每次攻击牌效果结算后只行动一次；重放产生的新一次出牌可以再触发。</summary>
[CustomID("OTTO_LOST_PARADISE")]
public sealed class LostParadisePower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Card.Owner.Creature == Owner && play.Card.Type == CardType.Attack &&
            Zoo.Companion(Owner, 2) is HuoXingBaoPower mars) await mars.ActivateEffect(ctx);
    }
}
