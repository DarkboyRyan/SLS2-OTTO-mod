using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>动物园的唯一规则入口。伙伴用游戏状态保存，不另外建立易失的全局字典。</summary>
public static class Zoo
{
    public static ZooCompanionPower? Current(Creature creature) => creature.Powers.OfType<ZooCompanionPower>().FirstOrDefault();
    public static IEnumerable<ZooCompanionPower> All(Creature creature) => creature.Powers.OfType<ZooCompanionPower>();
    public static int Count(Creature creature) => All(creature).Count();
    public static IEnumerable<IHoverTip> HoverTips =>
        [HoverTipFactory.FromPower<XuanDogPower>(), HoverTipFactory.FromPower<ShanNiRuoPower>(), HoverTipFactory.FromPower<HuoXingBaoPower>()];

    // 指挥全体伙伴的牌用一栏概述，保留各伙伴自身的完整状态提示。
    public static IHoverTip SummaryHoverTip => new HoverTip(
        new LocString("powers", "OTTO_ZOO_SUMMARY.title"),
        new LocString("powers", "OTTO_ZOO_SUMMARY.description"));

    public static async Task SummonRandom(PlayerChoiceContext ctx, CardModel source)
    {
        // 使用种子随机流，存档重放和联机客户端将得到相同结果；允许再次抽到同一伙伴。
        var eligible = Enumerable.Range(0, 3).Where(k => !IsImprisoned(source.Owner.Creature, k)).ToArray();
        if (eligible.Length > 0)
            await Summon(ctx, source, source.Owner.RunState.Rng.CombatCardGeneration.NextItem(eligible));
    }

    public static async Task Summon(PlayerChoiceContext ctx, CardModel source, int kind, bool keepOthers = false, bool playAudio = true)
    {
        var owner = source.Owner.Creature;
        if (kind is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(kind));
        // 普通召唤不释放入狱伙伴，也不能先把在场的其他伙伴误删。
        if (IsImprisoned(owner, kind)) return;
        // 羁绊生效后保留其他种类；召唤同种仍先移除旧实例，避免重复回合效果。
        foreach (var old in All(owner).Where(p => (!keepOthers && !owner.HasPower<ZooBondPower>()) ||
            (kind == 0 && p is XuanDogPower) || (kind == 1 && p is ShanNiRuoPower) ||
            (kind == 2 && p is HuoXingBaoPower)).ToArray())
            await PowerCmd.Remove(old);
        switch (kind)
        {
            case 0: await PowerCmd.Apply<XuanDogPower>(ctx, owner, 1, owner, source); break;
            case 1: await PowerCmd.Apply<ShanNiRuoPower>(ctx, owner, 1, owner, source); break;
            case 2: await PowerCmd.Apply<HuoXingBaoPower>(ctx, owner, 1, owner, source); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        if (playAudio && Companion(owner, kind) is not null) ZooSummonAudio.Enqueue(owner, kind);
    }

    public static bool IsImprisoned(Creature owner, int kind) => kind switch
    {
        0 => owner.HasPower<DogPrisonPower>(), 1 => owner.HasPower<PigPrisonPower>(),
        2 => owner.HasPower<MarsPrisonPower>(), _ => false
    };
    public static ZooCompanionPower? Companion(Creature owner, int kind) => All(owner).FirstOrDefault(p =>
        kind == 0 && p is XuanDogPower || kind == 1 && p is ShanNiRuoPower || kind == 2 && p is HuoXingBaoPower);

    /// <summary>入狱只移走伙伴，不移除独立保存的小强化与能力强化。</summary>
    public static async Task Imprison(PlayerChoiceContext ctx, Creature owner, int kind, CardModel? source)
    {
        if (Companion(owner, kind) is not { } companion) return;
        // 山泥若首次入狱保护共用同一入口，挡伤、原子弹等来源均只消耗同一次机会。
        if (kind == 1 && owner.GetPower<LawAbidingPower>() is { Used: false } protection)
        {
            protection.Use();
            for (int i = 0; i < 3 && !owner.IsDead; i++)
                if (Companion(owner, 1) is ShanNiRuoPower pig) await pig.ActivateEffect(ctx);
            return;
        }
        await PowerCmd.Remove(companion);
        switch (kind)
        {
            case 0: await PowerCmd.Apply<DogPrisonPower>(ctx, owner, 1, owner, source); break;
            case 1: await PowerCmd.Apply<PigPrisonPower>(ctx, owner, 1, owner, source); break;
            case 2: await PowerCmd.Apply<MarsPrisonPower>(ctx, owner, 1, owner, source); break;
        }
    }
    public static async Task ReleaseAll(PlayerChoiceContext ctx, CardModel source)
    {
        var owner = source.Owner.Creature;
        var released = Enumerable.Range(0, 3).Where(k => IsImprisoned(owner, k)).ToArray();
        foreach (var prison in owner.Powers.OfType<ZooPrisonPower>().ToArray()) await PowerCmd.Remove(prison);
        // 救援是单伙伴上限的例外：本次全部获救伙伴与现有伙伴同时在场。
        foreach (int kind in released) await Summon(ctx, source, kind, keepOthers: true);
    }
}

/// <summary>伙伴共用显示/清理逻辑；没有独立生命值，不成为敌人的可攻击目标。</summary>
public abstract class ZooCompanionPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public abstract string SpriteKey { get; }
    public override string CustomPackedIconPath => $"res://OTTO/images/companions/{SpriteKey}_icon.tres";
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ZooVisuals.Sync(Owner);
        return Task.CompletedTask;
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        ZooVisuals.Sync(oldOwner);
        return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room)
    {
        ZooVisuals.Clear(Owner);
        return Task.CompletedTask;
    }
    public override Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == Owner && !wasRemovalPrevented) ZooVisuals.Clear(Owner);
        return Task.CompletedTask;
    }
    protected bool IsOwnersTurn(CombatSide side, IEnumerable<Creature> participants) =>
        side == Owner.Side && participants.Contains(Owner) && !Owner.IsDead;
    // 每次悬停读取当前强化数值，避免替换伙伴后出现过期描述。
    protected LocString EffectDescription(string suffix, int value)
    {
        var text = new LocString("powers", Id.Entry + suffix);
        text.Add("Value", value);
        return text;
    }
    protected void ShowActivation()
    {
        Flash();
        ZooVisuals.Activate(Owner, SpriteKey);
    }
}

/// <summary>炫狗：回合结束获得四格挡，再随机对敌造成四点非攻击伤害。</summary>
[CustomID("OTTO_XUAN_DOG")]
public sealed class XuanDogPower : ZooCompanionPower
{
    public override string SpriteKey => "xuan_dog";
    // 状态悬停显示当前强化后的实际效果，避免卡面与伙伴图标仍显示旧数值。
    public int EffectAmount => (IsMutable && Owner.HasPower<DogTrainingPower>() ? 10 : 4) +
        (IsMutable ? Owner.GetPowerAmount<DogBoostPower>() : 0);
    public override LocString Description => EffectDescription(".scalingDescription", EffectAmount);
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!IsOwnersTurn(side, participants)) return;
        await ActivateEffect(ctx);
    }

    /// <summary>定向指挥与自然行动共用结算；返回本次是否击杀，供连续行动追加次数。</summary>
    public async Task<bool> ActivateEffect(PlayerChoiceContext ctx, Creature? target = null)
    {
        if (Owner.IsDead || Zoo.Companion(Owner, 0) != this) return false;
        ShowActivation();
        // 伙伴使用固定伤害/格挡；强化状态独立保存，换走再召回仍为10/10。
        int amount = EffectAmount;
        await CreatureCmd.GainBlock(Owner, amount, ValueProp.Unpowered, null);
        var enemies = CombatState.GetOpponentsOf(Owner).Where(c => c.IsHittable).ToArray();
        if (enemies.Length == 0) return false;
        var enemy = target is { IsHittable: true } && enemies.Contains(target) ? target :
            CombatState.RunState.Rng.CombatTargets.NextItem(enemies)!;
        // 游戏要求能杀死敌人的回合末效果放在 BeforeSideTurnEnd，以保证胜利结算顺序。
        var results = await CreatureCmd.Damage(ctx, enemy, amount, ValueProp.Unpowered, Owner);
        return results.Any(r => r.Receiver == enemy && r.WasTargetKilled);
    }
}

/// <summary>山泥若：仅在所属玩家本回合结束时恢复两血，不在敌方或他人额外回合重复结算。</summary>
[CustomID("OTTO_SHAN_NI_RUO")]
public sealed class ShanNiRuoPower : ZooCompanionPower
{
    public override string SpriteKey => "shan_ni_ruo";
    public int HealAmount => (IsMutable && Owner.HasPower<PigTrainingPower>() ? 4 : 2) +
        (IsMutable ? Owner.GetPowerAmount<PigBoostPower>() : 0);
    public override LocString Description => EffectDescription(IsMutable && Owner.HasPower<PigTrainingPower>() ? ".scalingTrainedDescription" : ".scalingDescription", HealAmount);
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!IsOwnersTurn(side, participants)) return;
        await ActivateEffect(ctx);
    }

    /// <summary>自然回合末与“猪逼吧”共用效果，保证所有治疗强化和群体中毒保持一致。</summary>
    public async Task ActivateEffect(PlayerChoiceContext ctx)
    {
        if (Owner.IsDead || Zoo.Companion(Owner, 1) != this || Zoo.IsImprisoned(Owner, 1)) return;
        ShowActivation();
        bool trained = Owner.HasPower<PigTrainingPower>();
        if (Owner.GetPower<KfcPower>() is { } kfc)
        {
            // 使用理论治疗量，满血仍有完整伤害，古神翻倍也照常生效。
            var targets = CombatState.GetOpponentsOf(Owner).Where(c => c.IsHittable).ToArray();
            if (targets.Length > 0)
                await CreatureCmd.Damage(ctx, targets, HealAmount * kfc.Amount, ValueProp.Unpowered, Owner);
        }
        else await CreatureCmd.Heal(Owner, HealAmount);
        if (trained)
            foreach (var enemy in CombatState.GetOpponentsOf(Owner).Where(c => !c.IsDead).ToArray())
                await PowerCmd.Apply<PoisonPower>(ctx, enemy, 4, Owner, null);
    }
}

/// <summary>火星包：每回合末增加一力量和一敏捷，替换伙伴不会撤销已经获得的属性。</summary>
[CustomID("OTTO_HUO_XING_BAO")]
public sealed class HuoXingBaoPower : ZooCompanionPower
{
    public override string SpriteKey => "huo_xing_bao";
    public int EffectAmount => (IsMutable && Owner.HasPower<MarsTrainingPower>() ? 3 : 1) +
        (IsMutable ? Owner.GetPowerAmount<MarsBoostPower>() : 0);
    public override LocString Description
    {
        get
        {
            int mode = IsMutable ? Owner.GetPowerAmount<MarsModePower>() : 0;
            return EffectDescription(mode == 1 ? ".strengthDescription" : mode == 2 ? ".dexterityDescription" : ".scalingDescription",
                mode == 0 ? EffectAmount : EffectAmount * 2);
        }
    }
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!IsOwnersTurn(side, participants)) return;
        await ActivateEffect(ctx);
    }

    /// <summary>自然行动、攻击联动和立即行动均使用当前模式；换走或入狱不清空模式。</summary>
    public async Task ActivateEffect(PlayerChoiceContext ctx)
    {
        if (Owner.IsDead || Zoo.Companion(Owner, 2) != this) return;
        ShowActivation();
        int amount = EffectAmount;
        int mode = Owner.GetPowerAmount<MarsModePower>();
        if (mode != 2) await PowerCmd.Apply<StrengthPower>(ctx, Owner, mode == 1 ? amount * 2 : amount, Owner, null);
        if (mode != 1) await PowerCmd.Apply<DexterityPower>(ctx, Owner, mode == 2 ? amount * 2 : amount, Owner, null);
    }
}
