using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace OttoMod;

/// <summary>呼朋唤友的进阶技能；只指挥刚召唤的伙伴，不改变现有召唤规则。</summary>
[CustomID("OTTO_COLLEAGUE_BENEFACTOR")]
public sealed class ColleagueBenefactor() : OttoCard(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override string CustomPortraitPath => "res://OTTO/images/cards/colleague_benefactor.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(5)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Zoo.HoverTips;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var owner = Owner.Creature;
        if (owner.IsDead) return;
        var eligible = Enumerable.Range(0, 3).Where(k => !Zoo.IsImprisoned(owner, k)).ToArray();
        if (eligible.Length == 0) return;
        await CreatureCmd.TriggerAnim(owner, "Cast", Owner.Character.CastAnimDelay);
        int kind = Owner.RunState.Rng.CombatCardGeneration.NextItem(eligible);
        await Zoo.Summon(ctx, this, kind);
        var companion = Zoo.Companion(owner, kind);
        if (companion == null) return;
        for (int i = 0; i < DynamicVars.Repeat.IntValue && !owner.IsDead; i++)
        {
            switch (companion)
            {
                case XuanDogPower dog: await dog.ActivateEffect(ctx); break;
                case ShanNiRuoPower pig: await pig.ActivateEffect(ctx); break;
                case HuoXingBaoPower mars: await mars.ActivateEffect(ctx); break;
            }
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
