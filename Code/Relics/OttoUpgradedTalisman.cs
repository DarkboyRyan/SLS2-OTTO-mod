using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace OttoMod;

/// <summary>老棍和棍亩：通过原版事件升级棍亩获得，每场战斗开始时给予两层无实体。</summary>
[Pool(typeof(OttoRelicPool))]
[CustomID("OTTO_UPGRADED_TALISMAN")]
public sealed class OttoUpgradedTalisman : CustomRelicModel
{
    public const string ImagePath = "res://OTTO/images/relics/otto_upgraded_talisman_square.tres";
    // 原版黑暗之血等升级初始遗物也使用 Starter，正常商店和随机遗物奖励不会抽到它。
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool IsAllowedInShops => false;

    // 原图加透明留白形成方形纹理，保持双人图片长宽比。
    public override string PackedIconPath => ImagePath;
    protected override string BigIconPath => ImagePath;
    protected override string PackedIconOutlinePath => ImagePath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IntangiblePower>(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];

    // 已经是最终升级版；再次遇到升级入口时保持本遗物，不落入原版未知遗物的头环兜底。
    public override RelicModel GetUpgradeReplacement() => ModelDb.Relic<OttoUpgradedTalisman>();

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        // 只在进入战斗房间时触发。事件替换已移除旧遗物，因此这里单独给 2 层，不会叠成 3 层。
        if (room is not CombatRoom) return;
        Flash();
        // 使用原版无实体，敌方回合末每次减少一层，持续规则与其他来源的无实体一致。
        await PowerCmd.Apply<IntangiblePower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, null);
    }
}
