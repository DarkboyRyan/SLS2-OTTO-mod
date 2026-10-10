using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace OttoMod;

/// <summary>OTTO-086：先古稀有度与ITomeCard指定达弗入口；关闭随机生成和变换入口。</summary>
[CustomID("OTTO_V5")]
public sealed class V5Otto() : OttoCard(3, CardType.Power, CardRarity.Ancient, TargetType.Self), ITomeCard
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    // 使用用户提供的 V5 队服照片，普通版与升级版共用这张先古牌卡面。
    public override string CustomPortraitPath => "res://OTTO/images/cards/v5_otto.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<V5OttoPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<V5OttoPower>(ctx, Owner.Creature, 3, Owner.Creature, this);
        if (IsUpgraded) Owner.Creature.GetPower<V5OttoPower>()!.UpgradedPlays += 3;
    }
    protected override void OnUpgrade() { }
}

/// <summary>使用全职业真实格挡标记，直接在出牌区执行，既不抽牌也不占用手牌位置。</summary>
[CustomID("OTTO_V5_POWER")]
public sealed class V5OttoPower : OttoBuff
{
    public int UpgradedPlays { get; set; }
    public override LocString Description
    {
        get { var text = base.Description; text.Add("Upgraded", UpgradedPlays); return text; }
    }
    public IEnumerable<CardModel> Candidates() => ModelDb.AllCharacterCardPools
        .SelectMany(p => p.GetUnlockedCards(Owner.Player!.UnlockState, Owner.Player.RunState.CardMultiplayerConstraint))
        .Where(c => c.GainsBlock && c.CanBeGeneratedInCombat && c.Type is CardType.Attack or CardType.Skill or CardType.Power)
        .DistinctBy(c => c.Id).OrderBy(c => c.Id.Entry, StringComparer.Ordinal);

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead) return;
        Flash();
        for (int i = 0; i < Amount && !CombatManager.Instance.IsOverOrEnding && !Owner.IsDead; i++)
        {
            // 每次从完整池重新抽取，允许重复；不满足出牌条件的候选跳过并重新选择。
            var candidates = Candidates().ToList();
            while (candidates.Count > 0)
            {
                int index = Owner.Player!.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count);
                var canonical = candidates[index]; candidates.RemoveAt(index);
                // 先注册到当前战斗，再进入出牌区；只ToMutable会缺失战斗归属，跨职业牌无法入场。
                var card = CombatState.CreateCard(canonical, Owner.Player);
                if (i < UpgradedPlays && card.IsUpgradable) card.UpgradeInternal();
                // CardModel.CombatState由所在牌堆决定，必须先入出牌区再调用原版/其他Mod的条件判断。
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Play, Owner.Player);
                if (!OttoReplay.CanAutoPlay(card)) { await CardPileCmd.RemoveFromCombat(card); continue; }
                var target = OttoReplay.Target(card, null);
                if (card.TargetType is TargetType.AnyEnemy or TargetType.AnyAlly && target is null)
                { await CardPileCmd.RemoveFromCombat(card); continue; }
                await CardCmd.AutoPlay(ctx, card, target);
                break;
            }
        }
    }
}
