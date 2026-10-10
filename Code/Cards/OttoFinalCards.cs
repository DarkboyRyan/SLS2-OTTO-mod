using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>OTTO-061：先确定形态分支；本张积攒触发变身后，不追加另一个分支。</summary>
[CustomID("OTTO_MO_MO_MI_NO")]
public sealed class MoMoMiNo() : AncientCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/mo_mo_mi_no.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move), new DynamicVar("Ancient", 20), new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Owner.Creature.HasPower<AncientFormPower>()) await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
            await AncientForm.Gain(ctx, this, DynamicVars["Ancient"].BaseValue);
        }
    }
    protected override void OnUpgrade()
    { DynamicVars.Block.UpgradeValueBy(3); DynamicVars["Ancient"].UpgradeValueBy(10); DynamicVars.Cards.UpgradeValueBy(1); }
}

/// <summary>OTTO-062：古神分支先易伤再攻击；普通分支在伤害后积攒。</summary>
[CustomID("OTTO_AO_LI_AN_FEI")]
public sealed class AoLiAnFei() : AncientCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 使用用户提供的奥利安费图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/ao_li_an_fei.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move), new DynamicVar("Ancient", 25)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VulnerablePower>());
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        bool inForm = Owner.Creature.HasPower<AncientFormPower>();
        if (inForm) await PowerCmd.Apply<VulnerablePower>(ctx, play.Target!, 2, Owner.Creature, this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
        if (!inForm) await AncientForm.Gain(ctx, this, DynamicVars["Ancient"].BaseValue);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); DynamicVars["Ancient"].UpgradeValueBy(10); }
}

/// <summary>OTTO-063：按真实击杀增加动作次数；目标死亡后由伙伴的统一入口重新选敌人。</summary>
[CustomID("OTTO_GONG_GONG")]
public sealed class GongGongAgain() : OttoCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/gong_gong_again.png";
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips;
    public override bool ShouldPlay(CardModel card, AutoPlayType type) => card != this || !Zoo.IsImprisoned(Owner.Creature, 0);
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Zoo.IsImprisoned(Owner.Creature, 0)) return;
        if (Zoo.Companion(Owner.Creature, 0) is null) await Zoo.Summon(ctx, this, 0);
        int actions = DynamicVars.Repeat.IntValue;
        for (int i = 0; i < actions && !Owner.Creature.IsDead; i++)
        {
            if (Zoo.Companion(Owner.Creature, 0) is not XuanDogPower dog || !CombatState!.HittableEnemies.Any()) break;
            if (await dog.ActivateEffect(ctx, play.Target)) actions++;
        }
    }
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1);
}

[CustomID("OTTO_LAW_ABIDING")]
public sealed class LawAbiding() : OttoCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/law_abiding.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips.Append(HoverTipFactory.FromPower<LawAbidingPower>());
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<LawAbidingPower>()) await PowerCmd.Apply<LawAbidingPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        await Zoo.Summon(ctx, this, 1);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-065：重复使用取最高倍率；立即行动也使用刚设置的转伤效果。</summary>
[CustomID("OTTO_KFC")]
public sealed class Kfc() : RequiresCompanionCard(1, CardType.Skill, TargetType.Self, 1)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/kfc.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Multiplier", 4)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!HasCompanion) return;
        if (Owner.Creature.GetPower<KfcPower>() is { } power) power.SetAmount(Math.Max(power.Amount, DynamicVars["Multiplier"].IntValue));
        else await PowerCmd.Apply<KfcPower>(ctx, Owner.Creature, DynamicVars["Multiplier"].BaseValue, Owner.Creature, this);
        if (Zoo.Companion(Owner.Creature, 1) is ShanNiRuoPower pig) await pig.ActivateEffect(ctx);
    }
    protected override void OnUpgrade() => DynamicVars["Multiplier"].UpgradeValueBy(2);
}

[CustomID("OTTO_BIG_BLADE")]
public sealed class BigBlade() : OttoCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/big_blade.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips.Append(HoverTipFactory.FromPower<DogCounterPower>());
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Zoo.Summon(ctx, this, 0);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        if (!Owner.Creature.HasPower<DogCounterPower>()) await PowerCmd.Apply<DogCounterPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-067：段数只取一次快照，负敏捷不扣减基础一段，敏捷本身不被消耗。</summary>
[CustomID("OTTO_WAVE_CRASH")]
public sealed class WaveCrash() : OttoCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/wave_crash.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int hits = 1 + (Owner.Creature.HasPower<HuoXingBaoPower>() ? Math.Max(0, Owner.Creature.GetPowerAmount<DexterityPower>()) : 0);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!).WithHitCount(hits).Execute(ctx);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1);
}

/// <summary>OTTO-068：使用原版二选一界面；模式保存在独立状态中，伙伴替换不会丢失。</summary>
[CustomID("OTTO_I_AM_CHINESE")]
public sealed class IAmChinese() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/i_am_chinese.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Zoo.Summon(ctx, this, 2);
        var strength = ModelDb.Card<MarsStrengthChoice>().ToMutable(); strength.Owner = Owner;
        var dexterity = ModelDb.Card<MarsDexterityChoice>().ToMutable(); dexterity.Owner = Owner;
        var chosen = await CardSelectCmd.FromChooseACardScreen(ctx, [strength, dexterity], Owner);
        if (chosen is null) return;
        int mode = chosen is MarsDexterityChoice ? 2 : 1;
        if (Owner.Creature.GetPower<MarsModePower>() is { } power) power.SetAmount(mode);
        else await PowerCmd.Apply<MarsModePower>(ctx, Owner.Creature, mode, Owner.Creature, this);
        if (IsUpgraded && Zoo.Companion(Owner.Creature, 2) is HuoXingBaoPower mars) await mars.ActivateEffect(ctx);
    }
    protected override void OnUpgrade() { }
}

/// <summary>只用于选项界面，不加入OTTO奖励池，也不计入86张设计卡。</summary>
public abstract class MarsChoice() : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false, autoAdd: false)
{
    public override CardPoolModel Pool => ModelDb.CardPool<OttoCardPool>();
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override string PortraitPath => ModelDb.Card<DemonForm>().PortraitPath;
    public override string BetaPortraitPath => PortraitPath;
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Task.CompletedTask;
}
[CustomID("OTTO_MARS_STRENGTH_CHOICE")] public sealed class MarsStrengthChoice : MarsChoice;
[CustomID("OTTO_MARS_DEXTERITY_CHOICE")] public sealed class MarsDexterityChoice : MarsChoice;

/// <summary>OTTO-069：只认本次攻击的击杀结果；返手发生于出牌区清理前，因此不再被移入弃牌堆。</summary>
[CustomID("OTTO_KOREAN_HAND")]
public sealed class KoreanHand() : AncientCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/korean_hand.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!);
        await attack.Execute(ctx);
        if (!attack.Results.SelectMany(r => r).Any(r => r.Receiver == play.Target && r.WasTargetKilled)) return;
        await AncientForm.Gain(ctx, this, 50);
        await CardPileCmd.Add(this, PileType.Hand);
        await OttoCardRulesPower.Ensure(this);
        OttoFreeCostRules.MakeFreeThisTurn(this);
        InvokeEnergyCostChanged();
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8);
}

[CustomID("OTTO_OLD_REASON")]
public sealed class OldReason() : AncientCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    // 用户提供的说的老理卡面，普通版、升级版及放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/old_reason.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Owner.Creature.HasPower<AncientFormPower>())
        { await PlayerCmd.GainEnergy(3, Owner); await CardPileCmd.Draw(ctx, 3, Owner); }
        else await AncientForm.Gain(ctx, this, Owner.Creature.GetPowerAmount<AncientMeterPower>());
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[CustomID("OTTO_LAST_XUAN_CARD")]
public sealed class LastXuan() : OttoCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/last_xuan.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips.Append(HoverTipFactory.FromPower<LastXuanPower>());
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<LastXuanPower>()) await PowerCmd.Apply<LastXuanPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        await Zoo.Summon(ctx, this, 0);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-077：满血也按理论治疗量计算；只造成伤害，结算后通过统一入口入狱。</summary>
[CustomID("OTTO_ATOMIC_BOMB")]
public sealed class AtomicBomb() : OttoCard(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/atomic_bomb.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Multiplier", 20)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips;
    public override bool ShouldPlay(CardModel card, AutoPlayType type) => card != this || Zoo.Companion(Owner.Creature, 1) is ShanNiRuoPower;
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Zoo.Companion(Owner.Creature, 1) is not ShanNiRuoPower pig) return;
        await DamageCmd.Attack(pig.HealAmount * DynamicVars["Multiplier"].BaseValue).FromCard(this).TargetingAllOpponents(CombatState!).Execute(ctx);
        await Zoo.Imprison(ctx, Owner.Creature, 1, this);
    }
    protected override void OnUpgrade() => DynamicVars["Multiplier"].UpgradeValueBy(10);
}

[CustomID("OTTO_LOST_PARADISE_CARD")]
public sealed class LostParadise() : OttoCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/lost_paradise.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips.Append(HoverTipFactory.FromPower<LostParadisePower>());
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<LostParadisePower>()) await PowerCmd.Apply<LostParadisePower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        await Zoo.Summon(ctx, this, 2);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-080：先快照四种负面并移除，再逐种给敌人施加双倍；人工制品按原版规则处理。</summary>
[CustomID("OTTO_CCB")]
public sealed class Ccb() : OttoCard(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/ccb.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var effects = Owner.Creature.Powers.Where(p => p is WeakPower or VulnerablePower or FrailPower or PoisonPower)
            .Select(p => (Power: p, Amount: p.Amount)).ToArray();
        foreach (var effect in effects) await PowerCmd.Remove(effect.Power);
        foreach (var target in CombatState!.GetOpponentsOf(Owner.Creature).Where(c => !c.IsDead).ToArray())
            foreach (var effect in effects)
                switch (effect.Power)
                {
                    case WeakPower: await PowerCmd.Apply<WeakPower>(ctx, target, effect.Amount * 2, Owner.Creature, this); break;
                    case VulnerablePower: await PowerCmd.Apply<VulnerablePower>(ctx, target, effect.Amount * 2, Owner.Creature, this); break;
                    case FrailPower: await PowerCmd.Apply<FrailPower>(ctx, target, effect.Amount * 2, Owner.Creature, this); break;
                    case PoisonPower: await PowerCmd.Apply<PoisonPower>(ctx, target, effect.Amount * 2, Owner.Creature, this); break;
                }
        await CardPileCmd.Draw(ctx, 2, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-081：操作战斗抽牌堆中的实体牌；升级只影响本场副本，不修改永久牌组。</summary>
[CustomID("OTTO_AN_NAI")]
public sealed class AnNaiMaster() : OttoCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/an_nai_master.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var selected = (await CardSelectCmd.FromCombatPile(ctx, PileType.Draw.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 2), c => !c.EnergyCost.CostsX && !c.HasStarCostX)).ToArray();
        foreach (var card in selected) if (card.IsUpgradable) CardCmd.Upgrade(card);
        await CardPileCmd.Add(selected, PileType.Hand);
        foreach (var card in selected) { OttoFreeCostRules.MakeFreeThisTurn(card); card.InvokeEnergyCostChanged(); }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-083：剩余轮数固定为出牌瞬间数值，所有段数享受形态加成，最后才退出。</summary>
[CustomID("OTTO_JI_BAI")]
public sealed class JiBai() : AncientCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/ji_bai.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(30, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int hits = 1 + Owner.Creature.GetPowerAmount<AncientFormPower>();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).WithHitCount(hits).Execute(ctx);
        await AncientForm.Exit(Owner.Creature);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10);
}

/// <summary>OTTO-084：先一次性选择并消耗，再逐张给格挡和抽牌；本牌之后自身消耗不计入收益。</summary>
[CustomID("OTTO_SOYO")]
public sealed class NagasakiSoyo() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can, CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/nagasaki_soyo.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var selected = (await CardSelectCmd.FromHand(ctx, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, Owner.PlayerCombatState!.Hand.Cards.Count), c => c != this, this)).ToArray();
        int exhausted = 0;
        foreach (var card in selected)
        {
            if (card.Pile?.Type != PileType.Hand) continue;
            await CardCmd.Exhaust(ctx, card);
            if (card.Pile?.Type == PileType.Exhaust) exhausted++;
        }
        for (int i = 0; i < exhausted; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
            await CardPileCmd.Draw(ctx, 1, Owner);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}

public static class GirlBand
{
    // 明确列出成员，不依赖卡名中是否包含official；避免误把其他主播牌加入套装。
    public static bool Contains(CardModel card) => card is ChihayaAnon or ToyokawaSakiko or MisumiUika or KanameRana or NagasakiSoyo or TakamatsuTomori;
    public static IEnumerable<CardModel> Members => [ModelDb.Card<ChihayaAnon>(), ModelDb.Card<ToyokawaSakiko>(),
        ModelDb.Card<MisumiUika>(), ModelDb.Card<KanameRana>(), ModelDb.Card<NagasakiSoyo>(), ModelDb.Card<TakamatsuTomori>()];
}

/// <summary>OTTO-085：从弃牌堆回收2/3张牌，每张少女乐队牌回复1能量，不修改费用或生成复制品。</summary>
[CustomID("OTTO_TOMORI")]
public sealed class TakamatsuTomori() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can, CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/takamatsu_tomori.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var selected = (await CardSelectCmd.FromCombatPile(ctx, PileType.Discard.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, DynamicVars.Cards.IntValue))).ToArray();
        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            // 按选中的乐队成员逐张结算；普通牌只回收，原费用和升级状态保持不变。
            if (GirlBand.Contains(card)) await PlayerCmd.GainEnergy(1, Owner);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
