using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>古神值只属于本场战斗；进入时清空，形态内获得古神值的效果直接忽略。</summary>
public static class AncientForm
{
    // 门槛记录在本场战斗的状态中，新战斗自然回到100，读档/重放不依赖静态计数。
    public static int RequiredValue(Creature owner) => owner.HasPower<NanamiPower>() ? 100 : owner.GetPower<AncientThresholdPower>()?.Amount ?? 100;
    public static async Task Gain(PlayerChoiceContext ctx, CardModel source, decimal amount)
    {
        var owner = source.Owner.Creature;
        if (owner.IsDead || owner.HasPower<AncientFormPower>() || amount <= 0) return;
        if (owner.GetPowerAmount<AncientMeterPower>() + amount >= RequiredValue(owner))
            await Enter(ctx, source);
        else
            await PowerCmd.Apply<AncientMeterPower>(ctx, owner, amount, owner, source);
    }

    public static async Task Enter(PlayerChoiceContext ctx, CardModel source)
    {
        var owner = source.Owner.Creature;
        if (owner.IsDead) return;
        if (owner.GetPower<AncientMeterPower>() is { } meter) await PowerCmd.Remove(meter);
        // 直接进入也计一次新变身；形态中刷新不增加门槛，也不叠加伤害倍率。
        int duration = 1 + owner.GetPowerAmount<AminosPower>();
        if (owner.GetPower<AncientFormPower>() is { } form) form.SetAmount(duration);
        else
        {
            await PowerCmd.Apply<AncientThresholdPower>(ctx, owner,
                owner.HasPower<AncientThresholdPower>() ? 25 : 125, owner, source);
            await PowerCmd.Apply<AncientFormPower>(ctx, owner, duration, owner, source);
        }
        AncientFormVisuals.Sync(owner);
    }

    public static async Task Exit(Creature owner)
    {
        if (owner.GetPower<AncientFormPower>() is { } form) await PowerCmd.Remove(form);
    }
}

[CustomID("OTTO_ANCIENT_METER")]
public sealed class AncientMeterPower : OttoBuff
{
    // 使用用户提供的古神值图标，战斗状态栏与悬停大图共用；显示尺寸由原版UI控制。
    public override string CustomPackedIconPath => "res://OTTO/images/powers/ancient_meter.png";
    public override string CustomBigIconPath => CustomPackedIconPath;

    public override LocString Description
    {
        get
        {
            if (!IsMutable) return new LocString("powers", "OTTO_ANCIENT_METER.rulesDescription");
            var text = base.Description;
            text.Add("Threshold", IsMutable ? AncientForm.RequiredValue(Owner) : 100);
            return text;
        }
    }
}

[CustomID("OTTO_ANCIENT_THRESHOLD")]
public sealed class AncientThresholdPower : OttoBuff
{
    public override LocString Description => IsMutable && Owner.HasPower<NanamiPower>()
        ? new LocString("powers", "OTTO_NANAMI_POWER.description") : base.Description;
}

/// <summary>基础持续当前回合及紧接着的敌方行动；下次自己的回合开始退出，阿米诺斯仍可延长。</summary>
[CustomID("OTTO_ANCIENT_FORM")]
public sealed class AncientFormPower : OttoBuff
{
    public override string CustomPackedIconPath => "res://OTTO/images/character/otto_ancient_body.tres";
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        AncientFormVisuals.Sync(Owner);
        AncientSpeech.Sync(Owner);
        return Task.CompletedTask;
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        AncientFormVisuals.Sync(oldOwner);
        AncientSpeech.Clear(oldOwner);
        return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room)
    {
        AncientFormVisuals.Clear(Owner);
        AncientSpeech.Clear(Owner);
        return Task.CompletedTask;
    }
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == Owner.Side && participants.Contains(Owner)) await PowerCmd.Decrement(this);
    }
    // 免疫伤害在正式伤害计算阶段处理，不覆盖死亡判定，因此沙虫等直接击杀仍然有效。
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? source) =>
        target == Owner ? 0 : decimal.MaxValue;
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? source) =>
        target == Owner ? 0 : amount;
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? source) =>
        dealer == Owner && target != Owner ? 2 : 1;
}
