using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace OttoMod;

[CustomID("OTTO_LAUGHTER")]
public sealed class OttoLaughterPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    public async Task Repeat(PlayerChoiceContext ctx, CardPlay play)
    {
        Flash();
        await OttoReplay.PlayCopy(ctx, play.Card, play.Target);
    }
}

/// <summary>回合末快照最后一张手动攻击/技能，下一回合正常抽牌结束后再执行。</summary>
[CustomID("OTTO_DA_SI_MA")]
public sealed class DaSiMaPower : OttoBuff
{
    public override PowerStackType StackType => PowerStackType.Single;
    private CardModel? pending;
    private Creature? target;
    public override Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner)) return Task.CompletedTask;
        var rules = Owner.GetPower<OttoCardRulesPower>();
        pending = rules?.LastManualCard?.CreateClone();
        target = rules?.LastManualTarget;
        return Task.CompletedTask;
    }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner.Player || pending is null) return;
        var card = pending; var previousTarget = target;
        pending = null; target = null;
        Flash();
        await OttoReplay.PlayCopy(ctx, card, previousTarget);
    }
}
