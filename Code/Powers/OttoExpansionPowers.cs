using BaseLib.Utils.Attributes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>唢呐的代价：当前回合不禁攻击，下次自己的回合开始才生效，并于该回合末移除。</summary>
[CustomID("OTTO_NO_ATTACK_NEXT_TURN")]
public sealed class NoAttackNextTurnPower : OttoBuff
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    // 原版 Power 不单独保存战斗中途字段；这些标记与本次战斗状态一起创建和清理。
    public bool Active { get; set; }
    public bool Pending { get; set; } = true;
    // 随实际时机切换提示；已生效时不继续误写成“下回合”。
    public override LocString Description => !Active ? base.Description :
        new LocString("powers", Id.Entry + (Pending ? ".activeAndPendingDescription" : ".activeDescription"));
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        !Active || card.Owner.Creature != Owner || card.Type != CardType.Attack;
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == Owner.Side && participants.Contains(Owner))
        {
            Active = Pending;
            Pending = false;
        }
        return Task.CompletedTask;
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Active && side == Owner.Side && participants.Contains(Owner))
        {
            Active = false;
            // 在禁攻击回合再次吹唢呐，新的代价必须保留到再下一回合。
            if (!Pending) await PowerCmd.Remove(this);
        }
    }
}

/// <summary>白银：锁住本回合之后的所有出牌（含自动打出）；结束回合按钮不受影响。</summary>
[CustomID("OTTO_NO_MORE_CARDS")]
public sealed class NoMoreCardsPower : OttoBuff
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => card.Owner.Creature != Owner;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side && participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}

/// <summary>卡比兽减伤覆盖敌方行动，到下次自己的回合开始移除；每次伤害独立扣减，不会减成负数。</summary>
[CustomID("OTTO_FLAT_GUARD")]
public sealed class FlatGuardPower : OttoBuff
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == Owner.Side && participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}

/// <summary>在原版力量、易伤和无实体全部计算后扣减，避免“减2”被易伤放大；预览与实际结算一致。</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class OttoFlatGuardPatch
{
    [HarmonyPostfix]
    private static void Reduce(Creature? target, ModifyDamageHookType modifyDamageHookType,
        ref decimal __result, ref IEnumerable<AbstractModel> modifiers)
    {
        if (!modifyDamageHookType.HasFlag(ModifyDamageHookType.Cap) || __result <= 0 ||
            target?.GetPower<FlatGuardPower>() is not { } guard) return;
        __result = Math.Max(0, __result - guard.Amount);
        modifiers = modifiers.Append(guard).Distinct().ToArray();
    }
}

/// <summary>羁绊只改变同时存在的伙伴上限，重复打出不复制伙伴或重复它们的回合效果。</summary>
[CustomID("OTTO_ZOO_BOND")]
public sealed class ZooBondPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>强化状态独立于伙伴存在，因此替换、牺牲后重新召回仍保留强化，数值不因重复打出而叠加。</summary>
[CustomID("OTTO_DOG_TRAINING")]
public sealed class DogTrainingPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
}
[CustomID("OTTO_MARS_TRAINING")]
public sealed class MarsTrainingPower : OttoBuff
{
    // 火星包强化是固定升级为三力量三敏捷，不按能力牌张数继续增加。
    public override PowerStackType StackType => PowerStackType.Single;
}
[CustomID("OTTO_PIG_TRAINING")]
public sealed class PigTrainingPower : OttoBuff
{
    // 山泥若强化是固定四回血并对全体敌人施加四中毒。
    public override PowerStackType StackType => PowerStackType.Single;
}
