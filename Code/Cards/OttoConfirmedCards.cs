using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>OTTO-024：格挡给予自己，虚弱给予所选敌人；分别使用原版敏捷和减益结算。</summary>
[CustomID("OTTO_BRO_SORRY")]
public sealed class BroSorry() : OttoCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    // 使用用户提供的啊，哥我错了图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/bro_sorry.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move), new PowerVar<WeakPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<WeakPower>(ctx, play.Target!, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars["WeakPower"].UpgradeValueBy(1);
    }
}

/// <summary>OTTO-025：先抽一张再积攒古神值；形态内由统一入口忽略积攒，抽牌仍有效。</summary>
[CustomID("OTTO_BRO_MOVE")]
public sealed class BroMove() : AncientCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 使用用户提供的兄弟你动啊图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/bro_move.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new DynamicVar("Ancient", 20)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
        await AncientForm.Gain(ctx, this, DynamicVars["Ancient"].BaseValue);
    }
    protected override void OnUpgrade() => DynamicVars["Ancient"].UpgradeValueBy(10);
}

/// <summary>OTTO-026：在结算开始时确定段数，两段指向同一目标，每段正常接受古神翻倍和力量加成。</summary>
[CustomID("OTTO_OU_XI_GEI")]
public sealed class OuXiGei() : AncientCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 用户提供的欧西给卡面，普通版、升级版及放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/ou_xi_gei.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!)
            .WithHitCount(Owner.Creature.HasPower<AncientFormPower>() ? 2 : 1).Execute(ctx);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

/// <summary>OTTO-027：AncientCard 标记积攒、进入或利用古神形态的牌；回收牌自身不因此成为古神牌。</summary>
[CustomID("OTTO_AI_YA_MI_NO")]
public sealed class AiYaMiNo() : OttoCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 用户提供的哎呀米诺卡面，普通版、升级版及放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/ai_ya_mi_no.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [new HoverTip(new LocString("powers", "OTTO_ANCIENT_CARD.title"),
            new LocString("powers", "OTTO_ANCIENT_CARD.description"))];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        // 没有候选牌时原版选择器直接返回空，不影响已经获得的格挡。
        var selected = (await CardSelectCmd.FromCombatPile(ctx, PileType.Discard.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), c => c is AncientCard)).FirstOrDefault();
        if (selected is not null) await CardPileCmd.Add(selected, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

/// <summary>OTTO-030：额外行动只调用山泥若自己的效果，不派发全体回合结束事件。</summary>
[CustomID("OTTO_PIG_GO")]
public sealed class PigGo() : OttoCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 用户提供的猪鼻吧卡面，普通版、升级版及放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/pig_go.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ShanNiRuoPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
        if (Zoo.Companion(Owner.Creature, 1) is ShanNiRuoPower pig) await pig.ActivateEffect(ctx);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}

/// <summary>OTTO-034：每张实体牌独立保存保留折扣，不覆盖随机费用等其他改费来源。</summary>
[CustomID("OTTO_HOW_MUCH_LONGER")]
public sealed class HowMuchLonger() : OttoCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private int retainDiscount;
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/how_much_longer.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(18, ValueProp.Move)];
    public override Task AfterFlush(PlayerChoiceContext ctx, Player player,
        IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
    {
        // AfterFlush 给出本次真正保留的牌；抽牌堆、弃牌堆及队友的回合均不会误触发。
        if (player == Owner && retainedCards.Contains(this))
        {
            retainDiscount++;
            InvokeEnergyCostChanged();
        }
        return Task.CompletedTask;
    }
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = card == this ? Math.Max(0, originalCost - retainDiscount) : originalCost;
        return card == this && retainDiscount > 0;
    }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 能量已由游戏支付；仅清零本牌的折扣，保留所有其他本场/本回合费用修改。
        retainDiscount = 0;
        InvokeEnergyCostChanged();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(6);
}

/// <summary>OTTO-044：先抽牌，再可选地整理当前手牌；易拉罐资格使用出牌前的位置快照。</summary>
[CustomID("OTTO_ANON")]
public sealed class ChihayaAnon() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/chihaya_anon.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
        var selected = (await CardSelectCmd.FromHand(ctx, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1), null, this)).FirstOrDefault();
        if (selected is not null) OttoHandOrder.MoveLeft(selected, this);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

/// <summary>OTTO-045：每张能力增加一层回能量；是否已触发保存在同一个能力状态，不随叠层刷新。</summary>
[CustomID("OTTO_SAKIKO")]
public sealed class ToyokawaSakiko() : OttoCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/toyokawa_sakiko.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FirstCanEnergyPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await PowerCmd.Apply<FirstCanEnergyPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-046：原牌保持不变；创建本场复制品并继承升级、关键词和战斗修改。</summary>
[CustomID("OTTO_UIKA")]
public sealed class MisumiUika() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can, CardKeyword.Exhaust];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/misumi_uika.png";
    public static bool IsCopyTarget(CardModel card) => card.Type == CardType.Attack &&
        card.Keywords.Contains(OttoKeywords.Can) && !card.EnergyCost.CostsX && !card.HasStarCostX;
    public override bool ShouldPlay(CardModel card, AutoPlayType type) => card != this ||
        Owner.PlayerCombatState!.Hand.Cards.Any(IsCopyTarget);
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var selected = (await CardSelectCmd.FromHand(ctx, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsCopyTarget, this)).FirstOrDefault();
        if (selected is null) return;
        var copy = selected.CreateClone();
        copy.AddKeyword(OttoKeywords.Can);
        copy.AddKeyword(CardKeyword.Exhaust);
        // Bottom 对手牌表示最右侧。使用原版复制入堆流程，兼容复制相关遗物和满手溢出。
        await CardPileCmd.Add(copy, PileType.Hand, CardPilePosition.Bottom, clonedBy: this);
        // 入场可能被“为什么呀”随机费用，因此在入场后施加本回合免费，确保实际显示和支付一致。
        OttoFreeCostRules.MakeFreeThisTurn(copy);
        copy.InvokeEnergyCostChanged();
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-047：从当前抽牌堆无放回随机取实体牌；检索不是抽牌，不触发抽牌重掷或洗牌。</summary>
[CustomID("OTTO_KARASU")]
public sealed class Karasu() : OttoCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can];
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/karasu.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!).Execute(ctx);
        var eligible = PileType.Draw.GetPile(Owner).Cards.Where(c => c.Keywords.Contains(OttoKeywords.Can)).ToList();
        var selected = new List<CardModel>();
        while (selected.Count < 2 && eligible.Count > 0)
        {
            int index = Owner.RunState.Rng.CombatCardSelection.NextInt(eligible.Count);
            selected.Add(eligible[index]);
            eligible.RemoveAt(index);
        }
        await CardPileCmd.Add(selected, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}

/// <summary>OTTO-048：门槛只计算打出本牌之前的边缘触发，不能把自身易拉罐算进去。</summary>
[CustomID("OTTO_PHANTOM_HAZE")]
public sealed class PhantomHaze() : OttoCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/phantom_haze.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move), new DynamicVar("Threshold", 3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];
    protected override bool ShouldGlowGoldInternal => IsMutable && Owner is not null &&
        (Owner.Creature.GetPower<OttoCardRulesPower>()?.CanTriggersThisTurn ?? 0) >= DynamicVars["Threshold"].BaseValue;
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        bool qualified = OttoEdgeSnapshotPatch.PriorTriggers.Get(this) >= DynamicVars["Threshold"].BaseValue;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
        if (qualified) await PowerCmd.Apply<IntangiblePower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
        DynamicVars["Threshold"].UpgradeValueBy(-1);
    }
}

/// <summary>OTTO-049：按用户最终确认保持零费且不消耗；段数固定为本次出牌前触发次数加一。</summary>
[CustomID("OTTO_RANA")]
public sealed class KanameRana() : OttoCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/kaname_rana.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [OttoKeywords.Can];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!)
            .WithHitCount(1 + OttoEdgeSnapshotPatch.PriorTriggers.Get(this)).Execute(ctx);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}
