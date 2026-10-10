using BaseLib.Utils.Attributes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>只读取敌人招式，不实际调用敌方行动，避免推进AI、召唤敌人或把增益送给敌人。</summary>
public static class OttoIntentCopy
{
    // 防御/强化图标不携带数值，必须读取对应招式；不能把所有 BuffIntent 都当成力量。
    // 活力只增强下一次攻击，与永久力量分开处理，并在本次复制攻击之后给予。
    public sealed record Effects(decimal Block = 0, decimal Strength = 0, decimal Ritual = 0,
        decimal Vigor = 0, decimal Thorns = 0, decimal Intangible = 0);

    public static Effects? SupportEffects(MonsterModel monster) => (monster, monster.NextMove.Id) switch
    {
        (Guardbot, "GUARD_MOVE") => new(Block: 15),
        (PunchConstruct, "READY_MOVE") => new(Block: 10),
        (CalcifiedCultist, "INCANTATION_MOVE") => new(Ritual: 2),
        (DampCultist, "INCANTATION_MOVE") => new(Ritual: ReadNumber(monster, "IncantationAmount")),
        (LivingShield, "SMASH_MOVE") => new(Strength: 3),
        (SlumberingBeetle, "ROLL_OUT_MOVE") => new(Strength: 2),
        (Axebot axe, "BOOT_UP_MOVE") => new(Block: ReadNumber(axe, "BootUpBlock"),
            Strength: ReadNumber(axe, "BootUpStrGain") * (2 - axe.StockAmount)),

        // 原版攻击＋强化招式。数值随进阶改变的招式读取当前模型，避免高进阶复制错误。
        (GremlinMerc, "HEHE_MOVE") => new(Strength: 2),
        (CeremonialBeast, "PLOW_MOVE") => new(Strength: ReadNumber(monster, "PlowStrength")),
        (CeremonialBeast, "CRUSH_MOVE") => new(Strength: ReadNumber(monster, "CrushStrength")),
        (CubexConstruct, "REPEATER_BLAST_MOVE" or "REPEATER_BLAST_MOVE_2") => new(Strength: 2),
        (DecimillipedeSegment, "BULK_MOVE") => new(Strength: ReadNumber(monster, "BulkStrength")),
        (Fogmog, "SWIPE_MOVE" or "SWIPE_RANDOM_MOVE") => new(Strength: 1),
        (GlobeHead, "GALVANIC_BURST") => new(Strength: 2),
        (Myte, "SUCK_MOVE") => new(Strength: ReadNumber(monster, "SuckStrength")),
        (SkulkingColony, "INERTIA_MOVE") => new(Strength: ReadNumber(monster, "InertiaStrengthGain")),
        (SludgeSpinner, "RAGE_MOVE") => new(Strength: 3),
        (SnappingJaxfruit, "ENERGY_ORB_MOVE") => new(Strength: 2),
        (TerrorEel, "THRASH_MOVE") => new(Vigor: 6),
        (TheAdversaryMkOne, "BARRAGE_MOVE") => new(Strength: 2),
        (TheAdversaryMkTwo, "BARRAGE_MOVE") => new(Strength: 3),
        (TheAdversaryMkThree, "BARRAGE_MOVE") => new(Strength: 4),

        // 两只怪分别显示攻击和强化时，普通强化那只也能单独选中，不受另一只的意图影响。
        (BowlbugNectar, "BUFF_MOVE") => new(Strength: ReadNumber(monster, "BuffStrengthGain")),
        (BruteRubyRaider, "ROAR_MOVE") => new(Strength: 3),
        (BygoneEffigy, "WAKE_MOVE") => new(Strength: 10),
        (Crusher, "ADAPT_MOVE") => new(Strength: ReadNumber(monster, "AdaptStrengthGain")),
        (CubexConstruct, "CHARGE_UP_MOVE") => new(Strength: 2),
        (Exoskeleton or FakeMerchantMonster or Queen, "ENRAGE_MOVE") => new(Strength: 2),
        (FlailKnight, "WAR_CHANT") => new(Strength: 3),
        (FrogKnight, "FOR_THE_QUEEN") => new(Strength: 5),
        (FuzzyWurmCrawler, "INHALE") => new(Strength: 7),
        (KinFollower, "POWER_DANCE_MOVE") => new(Strength: ReadNumber(monster, "DanceStrength")),
        (KinPriest, "RITUAL_MOVE") => new(Strength: ReadNumber(monster, "RitualStrength")),
        (LouseProgenitor, "CURL_AND_GROW_MOVE") => new(Block: ReadNumber(monster, "CurlBlock"), Strength: 5),
        (MechaKnight, "WINDUP_MOVE") => new(Block: 15, Strength: 5),
        (Nibbit, "HISS_MOVE") => new(Strength: ReadNumber(monster, "HissStrengthGain")),
        (Ovicopter, "NUTRITIONAL_PASTE_MOVE") => new(Strength: ReadNumber(monster, "NutritionalPasteStrengthAmount")),
        (PhantasmalGardener, "ENLARGE_MOVE") => new(Strength: ReadNumber(monster, "EnlargeStr")),
        (Rocket, "CHARGE_UP_MOVE") => new(Strength: ReadNumber(monster, "ChargeUpStrengthGain")),
        (ScrollOfBiting, "MORE_TEETH") => new(Strength: 2),
        (Seapunk, "BUBBLE_BURP_MOVE") => new(Block: ReadNumber(monster, "BubbleBlock"), Strength: ReadNumber(monster, "BubbleStr")),
        (SewerClam, "PRESSURIZE_MOVE") => new(Strength: 4),
        (SoulFysh, "FADE_MOVE") => new(Intangible: 2),
        (SpinyToad, "PROTRUDING_SPIKES_MOVE") => new(Thorns: 5),
        (TheInsatiable, "SALIVATE_MOVE") => new(Strength: ReadNumber(monster, "SalivateStrength")),
        (TheObscura, "SAIL_MOVE") => new(Strength: 3),
        (Toadpole, "SPIKEN_MOVE") => new(Thorns: ReadNumber(monster, "SpikenAmount")),
        (TurretOperator, "RELOAD_MOVE") => new(Strength: 1),
        (Vantom, "PREPARE_MOVE") => new(Strength: 2),

        // 同样补齐攻击＋格挡，避免仅因为多了一个防御图标就无法指定该敌人。
        (Aeonglass, "EBB_MOVE") => new(Block: ReadNumber(monster, "EbbBlock")),
        (AxeRubyRaider, "SWING_1" or "SWING_2") => new(Block: ReadNumber(monster, "SwingBlock")),
        (BowlbugEgg, "BITE_MOVE") => new(Block: ReadNumber(monster, "ProtectBlock")),
        (Crusher, "GUARDED_STRIKE_MOVE") => new(Block: 18),
        (InfestedPrism, "RADIATE_MOVE") => new(Block: ReadNumber(monster, "RadiateBlock")),
        // 活性火花会污染玩家手牌，属于不可直接转给OTTO的怪物专属强化；只复制攻击和格挡。
        (InfestedPrism, "PULSATE_MOVE") => new(Block: ReadNumber(monster, "PulsateBlock")),
        (LagavulinMatriarch, "SLASH2_MOVE") => new(Block: ReadNumber(monster, "Slash2Block")),
        (MagiKnight, "POWER_SHIELD_MOVE") => new(Block: ReadNumber(monster, "PowerShieldBlock")),
        (Nibbit, "SLICE_MOVE") => new(Block: ReadNumber(monster, "SliceBlock")),
        (SlitheringStrangler, "THWACK") => new(Block: 5),
        (TheObscura, "HARDENING_STRIKE_MOVE") => new(Block: ReadNumber(monster, "HardeningStrikeBlock")),
        _ => null
    };

    private static decimal ReadNumber(MonsterModel monster, string name) =>
        Convert.ToDecimal(AccessTools.Property(monster.GetType(), name).GetValue(monster));

    public static bool CanCopy(Creature? target)
    {
        if (target is null || !target.IsHittable || target.Monster is not { } monster) return false;
        var intents = monster.NextMove.Intents;
        if (intents.Count == 0) return false;
        if (intents.Any(i => i is not SingleAttackIntent and not MultiAttackIntent and not DefendIntent and not BuffIntent)) return false;
        // 只检查选中的这一只，不要求其他敌人的意图也能复制。
        // 攻击＋强化始终允许指定：已适配的增益给自己；按用户规则忽略无法转换的专属强化。
        // 攻击＋格挡仍需有准确格挡数值；召唤/减益等其他图标继续不可选。
        if (SupportEffects(monster) is not null) return true;
        if (intents.Any(i => i is DefendIntent)) return false;
        return intents.Any(i => i is AttackIntent) || !intents.Any(i => i is BuffIntent);
    }

    public static async Task Execute(PlayerChoiceContext ctx, CardModel card, Creature target, CardPlay play)
    {
        if (!CanCopy(target)) return;
        // 在伤害发生前拍下当前意图，避免攻击导致敌人变招后把另一个招式的强化拼接进来。
        var attacks = target.Monster!.NextMove.Intents.OfType<AttackIntent>()
            .Select(i => (Damage: i.DamageCalc!(), Hits: i.Repeats)).ToArray();
        var effects = SupportEffects(target.Monster);
        foreach (var attack in attacks)
            await DamageCmd.Attack(attack.Damage).FromCard(card).WithHitCount(attack.Hits).Targeting(target).Execute(ctx);
        if (effects is null) return;
        if (effects.Block > 0) await CreatureCmd.GainBlock(card.Owner.Creature, effects.Block, ValueProp.Move, play);
        if (effects.Strength != 0) await PowerCmd.Apply<StrengthPower>(ctx, card.Owner.Creature, effects.Strength, card.Owner.Creature, card);
        if (effects.Ritual > 0) await PowerCmd.Apply<RitualPower>(ctx, card.Owner.Creature, effects.Ritual, card.Owner.Creature, card);
        if (effects.Vigor > 0) await PowerCmd.Apply<VigorPower>(ctx, card.Owner.Creature, effects.Vigor, card.Owner.Creature, card);
        if (effects.Thorns > 0) await PowerCmd.Apply<ThornsPower>(ctx, card.Owner.Creature, effects.Thorns, card.Owner.Creature, card);
        if (effects.Intangible > 0) await PowerCmd.Apply<IntangiblePower>(ctx, card.Owner.Creature, effects.Intangible, card.Owner.Creature, card);
    }
}

/// <summary>OTTO-060：复制原招式的基础数值和次数，攻击/格挡使用OTTO自己的属性，敌人下次意图不变。</summary>
[CustomID("OTTO_THEIR_OWN_WAY")]
public sealed class TheirOwnWay() : OttoCard(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/their_own_way.png";
    // 有条件地复制敌人格挡，同样属于带有格挡效果的牌。
    public override bool GainsBlock => true;
    // 新版设计表添加消耗；保留之前对单个合法敌人的目标选择修复。
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Target is { } target) await OttoIntentCopy.Execute(ctx, this, target, play);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card != this || CombatState!.GetOpponentsOf(Owner.Creature).Any(OttoIntentCopy.CanCopy);
}

/// <summary>将可复制检查接入原版目标验证，使鼠标、手柄与实际出牌都拒绝不支持的意图。</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
internal static class OttoCopyTargetPatch
{
    [HarmonyPostfix]
    private static void FilterTarget(CardModel __instance, Creature? target, ref bool __result)
    {
        if (__result && __instance is TheirOwnWay) __result = OttoIntentCopy.CanCopy(target);
    }
}
