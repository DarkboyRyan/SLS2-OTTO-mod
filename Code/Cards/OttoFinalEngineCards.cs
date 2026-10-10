using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace OttoMod;

/// <summary>OTTO-071：六位成员各生成一张；升级版本生成升级成员；费用只在本回合被覆盖。</summary>
[CustomID("OTTO_GIRL_BAND_FAN")]
public sealed class GirlBandFan() : OttoCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/girl_band_fan.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await OttoCardRulesPower.Ensure(this);
        foreach (var member in GirlBand.Members)
        {
            var card = CombatState!.CreateCard(member, Owner);
            if (IsUpgraded && card.IsUpgradable) card.UpgradeInternal();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
            OttoFreeCostRules.MakeFreeThisTurn(card); card.InvokeEnergyCostChanged();
        }
    }
    protected override void OnUpgrade() { }
}

/// <summary>OTTO-072：只快照本次抽牌堆，不递归加入后来生成/洗回的牌，也不执行其他all in。</summary>
[CustomID("OTTO_ALL_IN")]
public sealed class AllIn() : AncientCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/all_in.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var cards = PileType.Draw.GetPile(Owner).Cards.Where(c => c is AncientCard && c is not AllIn).ToArray();
        foreach (var card in cards)
        {
            if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead) break;
            // 前面的牌可能抽走后面的候选；仍打出同一张实体，但已经被消耗/自动打出的候选不重复执行。
            if (card.Pile?.Type is not (PileType.Draw or PileType.Hand) || !OttoReplay.CanAutoPlay(card)) continue;
            var target = OttoReplay.Target(card, null);
            if (card.TargetType is TargetType.AnyEnemy or TargetType.AnyAlly && target is null) continue;
            await CardCmd.AutoPlay(ctx, card, target);
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-073：能力层数同时影响未来进入与当前剩余时间，刷新形态也使用统一持续时间。</summary>
[CustomID("OTTO_AMINOS")]
public sealed class Aminos() : AncientCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/aminos.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<AminosPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        if (Owner.Creature.GetPower<AncientFormPower>() is { } form) form.SetAmount(form.Amount + 1);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
[CustomID("OTTO_AMINOS_POWER")]
public sealed class AminosPower : OttoBuff;

/// <summary>OTTO-075：用原版同一张牌的重放流程，保留X值并避免重复支付费用。</summary>
[CustomID("OTTO_SUONA_POWER_CARD")]
public sealed class SuonaPowerCard() : OttoCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 能力牌唢呐使用用户提供的乐器图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/suona_power.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<SuonaPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<SuonaPower>()) await PowerCmd.Apply<SuonaPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); AddKeyword(CardKeyword.Innate); }
}
[CustomID("OTTO_SUONA_POWER")]
public sealed class SuonaPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int count)
    {
        // 引擎先计算重复次数，之后才进入出牌前钩子，所以这里看此前已打出的2张。
        return card.Owner.Creature == Owner && OttoEdgeSnapshotPatch.IsManual.Get(card) &&
            Owner.GetPower<OttoCardRulesPower>()?.ManualPlaysThisTurn == 2 ? count + 1 : count;
    }
}

/// <summary>OTTO-078：固定的是进入门槛，保留清空、不积攒和已有形态持续时间的通用规则。</summary>
[CustomID("OTTO_NANAMI")]
public sealed class Nanami() : AncientCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/nanami.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<NanamiPower>()) await PowerCmd.Apply<NanamiPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        // 原门槛可能已经超过100；降低门槛后，已有100点古神值立即满足进入条件。
        if (!Owner.Creature.HasPower<AncientFormPower>() && Owner.Creature.GetPowerAmount<AncientMeterPower>() >= 100)
            await AncientForm.Enter(ctx, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
[CustomID("OTTO_NANAMI_POWER")]
public sealed class NanamiPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
}
