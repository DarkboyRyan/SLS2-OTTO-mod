using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace OttoMod;

/// <summary>普通能力共用图标占位与叠层方式；数值由游戏序列化，离开战斗自动清除。</summary>
public abstract class OttoBuff : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => ModelDb.Power<StrengthPower>().PackedIconPath;
    public override string CustomBigIconPath => ModelDb.Power<StrengthPower>().ResolvedBigIconPath;
}

/// <summary>冰：玩家回合开始、格挡清空之后获得固定格挡；山泥若在场时同量回血。</summary>
[CustomID("OTTO_ICE_POWER")]
public sealed class OttoIcePower : OttoBuff
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        if (Owner.HasPower<ShanNiRuoPower>()) await CreatureCmd.Heal(Owner, Amount);
    }
}

/// <summary>职业选手：每打出一张自己的牌就累积皮克斯；其他玩家出牌不会触发。</summary>
[CustomID("OTTO_PRO_PLAYER_POWER")]
public sealed class ProPlayerPower : OttoBuff
{
    public override async Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card.Owner.Creature != Owner || Owner.IsDead) return;
        // 在牌已确认打出、效果尚未开始时记数；击杀最后一个敌人后游戏会停止出牌后钩子。
        // 此能力无需玩家选择，使用游戏提供的无选择上下文，仍走正常状态命令。
        await PowerCmd.Apply<PixPower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, play.Card);
    }
}

/// <summary>皮克斯：把计数作为独立状态显示，在游戏清理战斗状态之前一次性转为生命上限。</summary>
[CustomID("OTTO_PIX")]
public sealed class PixPower : OttoBuff
{
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        int earned = Amount;
        if (earned <= 0 || Owner.IsDead) return;
        // 先清零，避免同一结算钩子被重复触发时重复兑现。GainMaxHp 同时增加等量当前生命。
        SetAmount(0);
        await CreatureCmd.GainMaxHp(Owner, earned);
    }
}
