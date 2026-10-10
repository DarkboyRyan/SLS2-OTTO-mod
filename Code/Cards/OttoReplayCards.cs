using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace OttoMod;

/// <summary>OTTO-074：是否触发由统一易拉罐入口判断，避免新卡重复抽牌或递归重放。</summary>
[CustomID("OTTO_LAUGHTER_CARD")]
public sealed class OttoLaughter() : OttoCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/otto_laughter.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<OttoLaughterPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<OttoLaughterPower>()) await PowerCmd.Apply<OttoLaughterPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>OTTO-082：同名能力不叠加；记录器也会记录本回合在能力生效之前打出的牌。</summary>
[CustomID("OTTO_DA_SI_MA_CARD")]
public sealed class DaSiMa() : OttoCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 用户提供的卡面，普通版、升级版与放大预览共用。
    public override string CustomPortraitPath => "res://OTTO/images/cards/da_si_ma.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DaSiMaPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!Owner.Creature.HasPower<DaSiMaPower>()) await PowerCmd.Apply<DaSiMaPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
