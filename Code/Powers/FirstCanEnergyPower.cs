using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace OttoMod;

/// <summary>丰川翔子：能力生效后每回合首次手动打出易拉罐牌回能量，不要求手牌边缘；叠层不刷新次数。</summary>
[CustomID("OTTO_FIRST_CAN_ENERGY")]
public sealed class FirstCanEnergyPower : OttoBuff
{
    private bool triggeredThisTurn;
    public async Task Trigger()
    {
        if (triggeredThisTurn || Owner.IsDead) return;
        // 在异步回能量前消耗本轮机会，避免其他效果重入时重复回能量。
        triggeredThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player!);
    }
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == Owner.Side && participants.Contains(Owner)) triggeredThisTurn = false;
        return Task.CompletedTask;
    }
}
