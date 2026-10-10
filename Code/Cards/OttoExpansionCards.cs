using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>OTTO-015：只标记本次实际抽到的牌；原有手牌照常可用，满手或禁抽按原版处理。</summary>
[CustomID("OTTO_WAWO")]
public sealed class Wawo() : OttoCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 用户称为“哇偶”的表情图，对应本牌“哇沃！”；普通与升级版共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/wawo.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Retain), HoverTipFactory.FromKeyword(OttoKeywords.Delayed)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await OttoCardRulesPower.Ensure(this);
        foreach (var drawn in await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner))
        {
            drawn.AddKeyword(CardKeyword.Retain);
            drawn.AddKeyword(OttoKeywords.Delayed);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

/// <summary>两种不同名字的手互相触发；同名第二张不会触发。基础数值翻倍后再计算力量或敏捷。</summary>
public abstract class OttoHandCard(CardType type, TargetType target) : OttoCard(1, type, CardRarity.Common, target)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Hand];
    protected bool HasOtherHand => IsMutable && Owner is not null &&
        ((Owner.Creature.GetPower<OttoCardRulesPower>()?.HandsPlayed ?? 0) & (this is OneHand ? 2 : 1)) != 0;
    protected override bool ShouldGlowGoldInternal => HasOtherHand;
}

/// <summary>OTTO-016：用户确认原表的“伤害翻倍”是笔误，本牌翻倍的是格挡。</summary>
[CustomID("OTTO_ONE_HAND")]
public sealed class OneHand() : OttoHandCard(CardType.Skill, TargetType.Self)
{
    // 偶内的手使用用户原图，普通版、升级版和放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/one_hand.png";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CalculationBaseVar(8), new CalculationExtraVar(8),
         new CalculatedBlockVar(ValueProp.Move).WithMultiplier((card, _) => ((OneHand)card).HasOtherHand ? 1 : 0)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, ((CalculatedBlockVar)DynamicVars["CalculatedBlock"]).Calculate(null), ValueProp.Move, play);
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4);
        DynamicVars.CalculationExtra.UpgradeValueBy(4);
    }
}

/// <summary>OTTO-017：在出过偶内的手后，基础伤害变为16（升级24）。</summary>
[CustomID("OTTO_AMINO_HAND")]
public sealed class AminoHand() : OttoHandCard(CardType.Attack, TargetType.AnyEnemy)
{
    // 阿米诺手使用左右镜像版本，与偶内的手形成方向相反的一对卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/amino_hand.png";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CalculationBaseVar(8), new ExtraDamageVar(8),
         new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) => ((AminoHand)card).HasOtherHand ? 1 : 0)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(play.Target!).Execute(ctx);
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4);
        DynamicVars.ExtraDamage.UpgradeValueBy(4);
    }
}

/// <summary>OTTO-018：立刻获得3/5能量；下一次自己的回合禁用攻击，技能和能力仍可使用。</summary>
[CustomID("OTTO_SUONA")]
public sealed class Suona() : OttoCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 使用用户提供的吹唢呐图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/suona.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<NoAttackNextTurnPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await PowerCmd.Apply<NoAttackNextTurnPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        // 人工制品可能挡下这项负面状态；此时只保留能量收益，不访问不存在的状态。
        if (Owner.Creature.GetPower<NoAttackNextTurnPower>() is { } penalty) penalty.Pending = true;
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(2);
}

/// <summary>OTTO-019：费用由共享计数动态计算，本牌打出后全体牛魔一起恢复原费用。</summary>
[CustomID("OTTO_BULL_DEMON")]
public sealed class BullDemon() : OttoCard(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 使用用户提供的牛魔图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/bull_demon.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(OttoKeywords.Can)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        (await OttoCardRulesPower.Ensure(this)).ResetCans();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5);
}

/// <summary>OTTO-028：按表保留5费和120/150伤害，不自行调整平衡。</summary>
[CustomID("OTTO_BACK_ELEPHANT")]
public sealed class BackElephant() : OttoCard(5, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 用户提供的踩背大象卡面，普通版、升级版及放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/back_elephant.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(120, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(30);
}

/// <summary>OTTO-035：本轮不能继续打牌，下轮能量重置之后才额外获得3点，避免奖励被重置吞掉。</summary>
[CustomID("OTTO_SILVER")]
public sealed class SilverTerminal() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 用户提供的白银段位图，替换本牌占位插画。
    public override string CustomPortraitPath => "res://OTTO/images/cards/silver_terminal.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<NoMoreCardsPower>(), HoverTipFactory.FromPower<EnergyNextTurnPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<NoMoreCardsPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        await PowerCmd.Apply<EnergyNextTurnPower>(ctx, Owner.Creature, 3, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-036：16格挡不随升级变化；减伤2→3，覆盖敌方回合，到下次自己回合开始结束。</summary>
[CustomID("OTTO_VOID_SNORLAX")]
public sealed class VoidSnorlax() : OttoCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/void_snorlax.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(16, ValueProp.Move), new DynamicVar("Reduction", 2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FlatGuardPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<FlatGuardPower>(ctx, Owner.Creature, DynamicVars["Reduction"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Reduction"].UpgradeValueBy(1);
}

/// <summary>OTTO-037：先攻击再赋予下张易拉罐零费；若已有零费，当前牌消费旧效果后获得新的。</summary>
[CustomID("OTTO_SPONGE_EKKO")]
public sealed class SpongeEkko() : OttoCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/sponge_ekko.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FreeCanPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
        await PowerCmd.Apply<FreeCanPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5);
}

/// <summary>OTTO-038：只丢弃本次实际抽到的非易拉罐；原有手牌保留。表中升级前后相同，照表实现。</summary>
[CustomID("OTTO_VTUBER_ESSAY")]
public sealed class VtuberEssay() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/vtuber_essay.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var drawn = (await CardPileCmd.Draw(ctx, 8, Owner)).ToArray();
        // 固定这次要弃的牌，后续弃牌、自动出牌和易拉罐抽牌不再改变这批目标。
        var toDiscard = drawn.Where(c => c.Pile?.Type == PileType.Hand && !c.Keywords.Contains(OttoKeywords.Can)).ToArray();
        await CardCmd.Discard(ctx, toDiscard);
    }
}

/// <summary>OTTO-051：X由原版支付记录决定，兼容X费用遗物；跨全部角色的常规攻击池生成，允许重复。</summary>
[CustomID("OTTO_HALILU_TORNADO")]
public sealed class HaliluTornado() : OttoCard(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/halilu_tornado.png";
    protected override bool HasEnergyCostX => true;
    public static IEnumerable<CardModel> AttackPool(CardModel source) => ModelDb.AllCharacterCardPools
        .SelectMany(pool => pool.GetUnlockedCards(source.Owner.UnlockState, source.Owner.RunState.CardMultiplayerConstraint))
        .Where(card => card.Type == CardType.Attack && card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
        .DistinctBy(card => card.Id);
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var pool = AttackPool(this).ToArray();
        if (pool.Length == 0) return;
        var generated = new List<CardModel>();
        for (int i = 0; i < ResolveEnergyXValue(); i++)
        {
            var card = CombatState!.CreateCard(pool[Owner.RunState.Rng.CombatCardGeneration.NextInt(pool.Length)], Owner);
            if (IsUpgraded) card.UpgradeInternal();
            // 整回合0费，包含星星和X费；不在首次打出后撤销，再次抽到仍免费。
            OttoFreeCostRules.MakeFreeThisTurn(card);
            generated.Add(card);
        }
        await CardPileCmd.AddGeneratedCardsToCombat(generated, PileType.Hand, Owner);
    }
}

/// <summary>OTTO-052：触发当前持有的对应遗物2/3次，初始版每次1层，升级版每次2层，无遗物时无收益。</summary>
[CustomID("OTTO_NONEXISTENT")]
public sealed class NonexistentThing() : OttoCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    // 使用用户提供的照片，普通版、升级版及放大预览共用该卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/nonexistent_thing.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        RelicModel? relic = Owner.GetRelic<OttoUpgradedTalisman>() ?? (RelicModel?)Owner.GetRelic<OttoTalisman>();
        if (relic is null || relic.IsMelted) return;
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            relic.Flash();
            await PowerCmd.Apply<IntangiblePower>(ctx, Owner.Creature, relic.DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1);
}

/// <summary>OTTO-053：立刻召齐三种伙伴，开启本场战斗的三伙伴共存；不产生同名重复伙伴。</summary>
[CustomID("OTTO_ZOO_BOND_CARD")]
public sealed class ZooBond() : OttoCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/zoo_bond.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips.Append(HoverTipFactory.FromPower<ZooBondPower>());
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<ZooBondPower>())
            await PowerCmd.Apply<ZooBondPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        var summoned = new List<int>();
        for (int kind = 0; kind < 3; kind++)
        {
            await Zoo.Summon(ctx, this, kind, playAudio: false);
            if (!Zoo.IsImprisoned(Owner.Creature, kind) && Zoo.Companion(Owner.Creature, kind) is not null)
                summoned.Add(kind);
        }
        ZooSummonAudio.PlayTogether(Owner.Creature, summoned);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>三张专属强化能力：先记录永久到战斗结束的强化，再召唤对应伙伴；升级只减1费。</summary>
public abstract class ZooTrainingCard<TPower>(int kind) : OttoCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    where TPower : PowerModel
{
    protected override CardModel PortraitSource => ModelDb.Card<DemonForm>();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips.Append(HoverTipFactory.FromPower<TPower>());
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Single控制显示方式，不能依靠它阻止原版数值增加；已经强化时不再施加。
        if (!Owner.Creature.HasPower<TPower>())
            await PowerCmd.Apply<TPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        await Zoo.Summon(ctx, this, kind);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-056：炫狗固定强化为10伤害和10格挡；按确认规则，升级后仍可换伙伴。</summary>
[CustomID("OTTO_SECOND_STREAM_GOAT")]
public sealed class SecondStreamGoat() : ZooTrainingCard<DogTrainingPower>(0)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/second_stream_goat.png";
}
/// <summary>OTTO-057：火星包固定强化为回合末3力量3敏捷。</summary>
[CustomID("OTTO_COMEBACK_DIRECTOR")]
public sealed class ComebackDirector() : ZooTrainingCard<MarsTrainingPower>(2)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/comeback_director.png";
}
/// <summary>OTTO-058：山泥若固定强化为回合末回复4生命并对全体敌人施加4中毒。</summary>
[CustomID("OTTO_ETERNAL_GOD")]
public sealed class EternalGod() : ZooTrainingCard<PigTrainingPower>(1)
{
    // 用户提供的永远的神卡面，普通版、升级版及放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/eternal_god.png";
}

/// <summary>OTTO-059：仅修改当前手牌的战斗实例，永久牌组和之后抽到的牌不受影响。</summary>
[CustomID("OTTO_JOHNNIE_WALKER")]
public sealed class JohnnieWalker() : OttoCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    // 使用用户提供的尊尼获加图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/johnnie_walker.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(OttoKeywords.Can)];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        foreach (var card in Owner.PlayerCombatState!.Hand.Cards)
            if (card != this) card.AddKeyword(OttoKeywords.Can);
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
