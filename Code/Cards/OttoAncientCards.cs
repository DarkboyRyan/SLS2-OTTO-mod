using BaseLib.Utils.Attributes;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>古神牌用一栏概述积攒和形态规则，避免两栏重复展开。</summary>
public abstract class AncientCard(int cost, CardType type, CardRarity rarity, TargetType target) : OttoCard(cost, type, rarity, target)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [new HoverTip(new LocString("powers", "OTTO_ANCIENT_METER.title"),
            new LocString("powers", "OTTO_ANCIENT_METER.cardRulesDescription"))];
}

/// <summary>OTTO-013：伤害先结算，再获得古神值；升级只提高古神值。</summary>
[CustomID("OTTO_REASON")]
public sealed class SpokenReason() : AncientCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 使用用户提供的古神表情图；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/spoken_reason.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move), new DynamicVar("Ancient", 10)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
        await AncientForm.Gain(ctx, this, DynamicVars["Ancient"].BaseValue);
    }
    protected override void OnUpgrade() => DynamicVars["Ancient"].UpgradeValueBy(10);
}
/// <summary>OTTO-014：表中“防御”按技能牌处理，格挡照常受敏捷影响。</summary>
[CustomID("OTTO_REASON_GUARD")]
public sealed class SpokenReasonGuard() : AncientCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 使用用户提供的双马尾表情图；普通版、升级版及放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/spoken_reason_guard.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move), new DynamicVar("Ancient", 10)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await AncientForm.Gain(ctx, this, DynamicVars["Ancient"].BaseValue);
    }
    protected override void OnUpgrade() => DynamicVars["Ancient"].UpgradeValueBy(10);
}
/// <summary>OTTO-020：两段独立伤害，每段分别计算力量和敌人格挡。</summary>
[CustomID("OTTO_CHOP_AGAIN")]
public sealed class ChopAgain() : OttoCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 使用用户提供的我砍，我再砍图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/chop_again.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move), new RepeatVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).WithHitCount(DynamicVars.Repeat.IntValue).Execute(ctx);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
/// <summary>OTTO-021：必须真正消耗一张其他手牌，才会获得可叠加的炫狗强化。</summary>
[CustomID("OTTO_AUG")]
public sealed class Aug() : OttoCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 用户提供的 AUG 卡面，普通版和升级版共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/aug.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Boost", 3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DogBoostPower>()];
    public override bool ShouldPlay(CardModel card, AutoPlayType type) => card != this ||
        Owner.PlayerCombatState!.Hand.Cards.Any(c => c != this);
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var selected = (await CardSelectCmd.FromHand(ctx, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), c => c != this, this)).FirstOrDefault();
        if (selected is null) return;
        await CardCmd.Exhaust(ctx, selected);
        await PowerCmd.Apply<DogBoostPower>(ctx, Owner.Creature, DynamicVars["Boost"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Boost"].UpgradeValueBy(2);
}
/// <summary>OTTO-022：伤害固定三点，升级只增加山泥若的持续治疗加成。</summary>
[CustomID("OTTO_HEADER")]
public sealed class PerfectHeader() : OttoCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 用户提供的完美的头球马内卡面，普通版和升级版共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/perfect_header.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move), new DynamicVar("Boost", 2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PigBoostPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
        await PowerCmd.Apply<PigBoostPower>(ctx, Owner.Creature, DynamicVars["Boost"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Boost"].UpgradeValueBy(1);
}
/// <summary>OTTO-023：强化火星包与是否在场无关；升级移除本牌消耗。</summary>
[CustomID("OTTO_MA_MAI_PI")]
public sealed class MaMaiPi() : OttoCard(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 使用用户提供的你马卖皮图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/ma_mai_pi.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MarsBoostPower>(),
        HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<VulnerablePower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<WeakPower>(ctx, play.Target!, 1, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(ctx, play.Target!, 1, Owner.Creature, this);
        await PowerCmd.Apply<MarsBoostPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
/// <summary>OTTO-029：选择范围限本次真正抽到且仍在手中的牌；按抽牌顺序免费打出剩余牌。</summary>
[CustomID("OTTO_FLIP_CLOUDS")]
public sealed class FlipClouds() : OttoCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 用户提供的翻云覆雨卡面，普通版、升级版及放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/flip_clouds.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var drawn = (await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner)).Where(c => c.Pile?.Type == PileType.Hand).ToArray();
        if (drawn.Length == 0) return;
        var discarded = (await CardSelectCmd.FromHand(ctx, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), drawn.Contains, this)).FirstOrDefault();
        if (discarded is not null) await CardCmd.Discard(ctx, discarded);
        foreach (var card in drawn.Where(c => c != discarded))
        {
            if (card.Pile?.Type != PileType.Hand || Owner.Creature.IsDead) continue;
            Creature? target = null;
            if (card.TargetType == TargetType.AnyEnemy)
            {
                // 原版自动出牌不会调用 IsValidTarget；主动筛选可保留其人之道、斩杀牌等限制。
                var targets = CombatState!.GetOpponentsOf(Owner.Creature).Where(c => c.IsHittable && card.IsValidTarget(c)).ToArray();
                if (targets.Length == 0) { await CardCmd.Discard(ctx, card); continue; }
                target = Owner.RunState.Rng.CombatTargets.NextItem(targets);
            }
            // 自动打出不消耗能量；X 值沿用原版自动出牌的当前能量规则。
            await CardCmd.AutoPlay(ctx, card, target);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
/// <summary>需要伙伴的牌用原版 ShouldPlay 限制手动与自动出牌；实际效果也再次检查。</summary>
public abstract class RequiresCompanionCard(int cost, CardType type, TargetType target, int kind)
    : OttoCard(cost, type, CardRarity.Uncommon, target)
{
    protected bool HasCompanion => Zoo.Companion(Owner.Creature, kind) is not null;
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => card != this || HasCompanion;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips.Append(HoverTipFactory.FromPower<DogPrisonPower>());
}
/// <summary>OTTO-031：只建立一次待命保护；重复打出不能重复消耗同一伙伴。</summary>
[CustomID("OTTO_TENG_YANG")]
public sealed class TengYangWorld() : RequiresCompanionCard(1, CardType.Skill, TargetType.Self, 1)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/teng_yang.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (HasCompanion && !Owner.Creature.HasPower<PigGuardPower>())
            await PowerCmd.Apply<PigGuardPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
/// <summary>OTTO-032：火星包立即入狱，五力量五敏捷保留到本场战斗结束。</summary>
[CustomID("OTTO_DEEP_SEA_FISH")]
public sealed class DeepSeaFish() : RequiresCompanionCard(1, CardType.Skill, TargetType.Self, 2)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/deep_sea_fish.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!HasCompanion) return;
        await Zoo.Imprison(ctx, Owner.Creature, 2, this);
        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, 5, Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, 5, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
/// <summary>OTTO-033：抽牌加成可叠加；随机费用和隐藏意图由一个战斗状态统一管理。</summary>
[CustomID("OTTO_WHY")]
public sealed class WhyOhWhy() : OttoCard(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/why_oh_why.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WhyPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var existing = Owner.Creature.GetPower<WhyPower>();
        await PowerCmd.Apply<WhyPower>(ctx, Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
        // 已有状态叠层不调用 AfterApplied，重复打出时仍需将现有战斗牌重新随机。
        if (existing is not null)
            foreach (var card in Owner.PlayerCombatState!.AllCards.ToArray()) existing.Randomize(card);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
/// <summary>OTTO-039：严格比较当前生命；相等不可选，不跳过原版的特殊死亡保护。</summary>
[CustomID("OTTO_HUMILIATE")]
public sealed class HumiliateWeak() : OttoCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/humiliate_weak.png";
    public bool CanExecute(Creature? target) => target is { IsHittable: true } && target.Side != Owner.Creature.Side &&
        target.CurrentHp < Owner.Creature.CurrentHp;
    public override bool ShouldPlay(CardModel card, AutoPlayType type) => card != this || CombatState!.GetOpponentsOf(Owner.Creature).Any(CanExecute);
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    { if (CanExecute(play.Target)) await CreatureCmd.Kill(play.Target!); }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
internal static class OttoExecuteTargetPatch
{
    [HarmonyPostfix] private static void Check(CardModel __instance, Creature? target, ref bool __result)
    { if (__result && __instance is HumiliateWeak card) __result = card.CanExecute(target); }
}
/// <summary>OTTO-040：永久降低目标力量并增加自己的力量；负力量与人工制品沿用原版规则。</summary>
[CustomID("OTTO_HANDSOME")]
public sealed class HandsomeOtto() : OttoCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 使用用户提供的帅OTTO横幅图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/handsome_otto.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Strength", 2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<StrengthPower>(ctx, play.Target!, -DynamicVars["Strength"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Strength"].UpgradeValueBy(1);
}
/// <summary>OTTO-041：原版 UnblockedDamage 已经扣除了溢出伤害，直接求和即实际扣血。</summary>
[CustomID("OTTO_HA_BI_XIA")]
public sealed class HaBiXia() : AncientCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    // 使用用户提供的哈比下表情图；普通版和升级版共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/ha_bi_xia.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!);
        await attack.Execute(ctx);
        await AncientForm.Gain(ctx, this, attack.Results.SelectMany(r => r).Sum(r => Math.Max(0, r.UnblockedDamage)));
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5);
}
/// <summary>OTTO-042：只释放被关押伙伴，不凭空生成尚未入狱的伙伴。</summary>
[CustomID("OTTO_YOUR_YOUTH")]
public sealed class YourYouth() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 使用用户指定的“正义执行”图片，作为伙伴救援牌的卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/your_youth.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips;
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await Zoo.ReleaseAll(ctx, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
/// <summary>OTTO-043：按效果栏攻击全体；所得格挡等于实际扣血，不再次增加敏捷。</summary>
[CustomID("OTTO_RED_BOOTS")]
public sealed class KneelingRedBoots() : RequiresCompanionCard(0, CardType.Attack, TargetType.AllEnemies, 0)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/kneeling_red_boots.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!HasCompanion) return;
        await Zoo.Imprison(ctx, Owner.Creature, 0, this);
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!);
        await attack.Execute(ctx);
        decimal lost = attack.Results.SelectMany(r => r).Sum(r => Math.Max(0, r.UnblockedDamage));
        await CreatureCmd.GainBlock(Owner.Creature, lost, ValueProp.Unpowered, play);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5);
}
/// <summary>OTTO-050：不要求古神值，进入同一形态并清空已经积攒的值。</summary>
[CustomID("OTTO_ANCIENT_HAM")]
public sealed class AncientHam() : AncientCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/ancient_ham.png";
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => await AncientForm.Enter(ctx, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
/// <summary>OTTO-054：按设计表仍属于攻击牌，但不造成伤害；眩晕可以被人工制品阻挡。</summary>
[CustomID("OTTO_ADVANCING_KING")]
public sealed class AdvancingKing() : AncientCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/advancing_king.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Ancient", 30)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<KingStunPower>());
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<KingStunPower>(ctx, play.Target!, 1, Owner.Creature, this);
        await AncientForm.Gain(ctx, this, DynamicVars["Ancient"].BaseValue);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
/// <summary>OTTO-055：固定十点上限，不因重复打出而叠加，也不重置本轮已经损失的生命。</summary>
[CustomID("OTTO_NAMED_HOU")]
public sealed class NamedHouGuoyu() : OttoCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 用户提供的名为侯国玉卡面，普通版、升级版及放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/named_hou.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HouGuoyuPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<HouGuoyuPower>()) await PowerCmd.Apply<HouGuoyuPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
