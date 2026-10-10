using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
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

/// <summary>表 OTTO-001：基础打击；保留 Strike 标签以兼容原版相关遗物。</summary>
[CustomID("OTTO_STRIKE")]
public sealed class OttoStrike() : OttoCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/otto_strike.png";
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

/// <summary>表 OTTO-002：基础防御，初始牌组放入五张。</summary>
[CustomID("OTTO_DEFEND")]
public sealed class OttoDefend() : OttoCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    // 用户提供的防御塔原图；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/otto_defend.png";
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

/// <summary>表 OTTO-003：随机召唤一位伙伴；每次先移除旧伙伴，同名重抽也不叠加。</summary>
[CustomID("OTTO_CALL_FRIENDS")]
public sealed class CallFriends() : OttoCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    // 使用用户提供的伙伴合照；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/call_friends.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips;
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await Zoo.SummonRandom(ctx, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>表 OTTO-004：逐个计算实际伙伴数量；羁绊开启后最多享受三份加成。</summary>
[CustomID("OTTO_RUSH")]
public sealed class OttoRush() : OttoCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 使用用户提供的冲刺表情图；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/otto_rush.png";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CalculationBaseVar(9), new ExtraDamageVar(6),
         // 使用原版计算变量，让悬停伤害预览和实际伤害使用同一套公式。
         new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) =>
             card.IsMutable && card.Owner is not null ? Zoo.Count(card.Owner.Creature) : 0)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips;
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(play.Target!).Execute(ctx);
    }
    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(3);
}

/// <summary>表 OTTO-005：按文字顺序先获得敏捷，再获得格挡，因此本次格挡也受新敏捷影响。</summary>
[CustomID("OTTO_WHEELCHAIR")]
public sealed class Wheelchair() : OttoCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 用户提供的轮椅原图；普通版、升级版与放大预览共用此资源。
    public override string CustomPortraitPath => "res://OTTO/images/cards/wheelchair.png";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(1), new BlockVar(6, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() => DynamicVars.Dexterity.UpgradeValueBy(1);
}

/// <summary>表 OTTO-006：零费回复，消耗交给游戏关键词系统处理。</summary>
[CustomID("OTTO_CHIVE_POCKET")]
public sealed class ChivePocket() : OttoCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 用户提供的韭菜盒子原图；共用基类负责加载普通卡面与放大纹理。
    public override string CustomPortraitPath => "res://OTTO/images/cards/chive_pocket.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Heal", 2)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Heal"].UpgradeValueBy(2);
}

/// <summary>表 OTTO-007：效果栏明确写随机三次，故使用 RandomEnemy；每一击由原版引擎重新选敌。</summary>
[CustomID("OTTO_THREE_WOLVES")]
public sealed class ThreeWolves() : OttoCard(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    // 使用用户提供的三狼图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/three_wolves.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move), new RepeatVar(3)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this).TargetingRandomOpponents(CombatState!).Execute(ctx);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

/// <summary>表 OTTO-008：无伙伴时补召唤，全体伙伴立即行动，再按实际行动数量积攒古神值。</summary>
[CustomID("OTTO_KING_JIJI")]
public sealed class KingJiji() : AncientCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 使用用户提供的吉吉国王照片；卡面路径由共用基类同时用于手牌和放大预览。
    public override string CustomPortraitPath => "res://OTTO/images/cards/king_jiji.png";
    // 炫狗行动包含格挡；使用古神牌基类，使回收与 all in 能识别本牌。
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Ancient", 10)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(Zoo.SummaryHoverTip);
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var owner = Owner.Creature;
        // 沿用动物园的种子随机与入狱限制；已有伙伴时不替换，也不额外召唤。
        if (Zoo.Count(owner) == 0) await Zoo.SummonRandom(ctx, this);
        int acted = 0;
        for (int kind = 0; kind < 3; kind++)
        {
            if (owner.IsDead || CombatManager.Instance.IsOverOrEnding) break;
            if (Zoo.IsImprisoned(owner, kind)) continue;
            // 直接调用伙伴行动，继承当前强化、肯德基与火星包模式，不触发其他回合末效果。
            switch (Zoo.Companion(owner, kind))
            {
                case XuanDogPower dog: await dog.ActivateEffect(ctx); break;
                case ShanNiRuoPower pig: await pig.ActivateEffect(ctx); break;
                case HuoXingBaoPower mars: await mars.ActivateEffect(ctx); break;
                default: continue;
            }
            acted++;
        }
        // 所有行动结束后统一积攒，避免途中变身改变后续伙伴伤害；形态内由统一入口忽略积攒。
        if (acted > 0 && !owner.IsDead && !CombatManager.Instance.IsOverOrEnding)
            await AncientForm.Gain(ctx, this, acted * DynamicVars["Ancient"].BaseValue);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>表 OTTO-009：全场中毒；牺牲炫狗只免除本次对出牌者的施毒，不清除已有中毒。</summary>
[CustomID("OTTO_BLUE_CANDY")]
public sealed class BlueCandy() : OttoCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 使用用户提供的蓝色M豆图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/blue_candy.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(20)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<XuanDogPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        bool protectedByDog = Owner.Creature.HasPower<XuanDogPower>();
        if (protectedByDog) await PowerCmd.Remove<XuanDogPower>(Owner.Creature);
        // 固定目标快照，防止状态变化影响遍历；所有存活生物（含其他玩家）都在范围内。
        foreach (var target in CombatState!.Creatures.Where(c => !c.IsDead).ToArray())
        {
            if (protectedByDog && target == Owner.Creature) continue;
            await PowerCmd.Apply<PoisonPower>(ctx, target, DynamicVars.Poison.BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Poison.UpgradeValueBy(10);
}

/// <summary>表 OTTO-010：把每回合格挡量作为能力层数存储；重复打出可相加。</summary>
[CustomID("OTTO_ICE")]
public sealed class OttoIce() : OttoCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    // 使用用户提供的 WOC!冰！图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/otto_ice.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<OttoIcePower>(), HoverTipFactory.FromPower<ShanNiRuoPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        await PowerCmd.Apply<OttoIcePower>(ctx, Owner.Creature, DynamicVars["Amount"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

/// <summary>表 OTTO-011：复用黑暗镣铐的临时减力量，自动恢复且正确响应人工制品。</summary>
[CustomID("OTTO_MISSED_Q")]
public sealed class MissedQ() : OttoCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 使用用户提供的 Q都能Q歪来图片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/missed_q.png";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("StrengthLoss", 10), new PowerVar<WeakPower>(3), new PowerVar<VulnerablePower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<WeakPower>(),
         HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<HuoXingBaoPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<DarkShacklesPower>(ctx, play.Target!, DynamicVars["StrengthLoss"].BaseValue, Owner.Creature, this);
        if (!Owner.Creature.HasPower<HuoXingBaoPower>()) return;
        await PowerCmd.Apply<WeakPower>(ctx, play.Target!, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(ctx, play.Target!, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(2);
        DynamicVars.Vulnerable.UpgradeValueBy(2);
    }
}

/// <summary>表 OTTO-012：注册出牌监听能力，皮克斯由另一个状态记录，战斗结束统一兑现。</summary>
[CustomID("OTTO_PRO_PLAYER")]
public sealed class ProPlayer() : OttoCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 使用用户提供的职业选手照片；普通版、升级版与放大预览共用此卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/pro_player.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ProPlayerPower>(), HoverTipFactory.FromPower<PixPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<ProPlayerPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        // 新增的这一层能力也计入本张职业选手；旧层数已由出牌前钩子记入，不重复计算。
        await PowerCmd.Apply<PixPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
