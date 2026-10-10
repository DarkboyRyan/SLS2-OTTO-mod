using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.Attributes;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;

namespace OttoMod;

/// <summary>只注册 OTTO 专属遗物；普通遗物奖励仍沿用铁甲战士的解锁规则。</summary>
[CustomID("OTTO_RELIC_POOL")]
public sealed class OttoRelicPool : CustomRelicPoolModel
{
    // 遗物说明若包含能量标记，也与 OTTO 卡牌使用同一个人物图标。
    public override string BigEnergyIconPath => OttoCardPool.OttoEnergyIconPath;
    public override string TextEnergyIconPath => OttoCardPool.OttoTextEnergyIconPath;
    public override Color LabOutlineColor => ModelDb.RelicPool<IroncladRelicPool>().LabOutlineColor;

    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState) =>
        // 只在生成奖励时借用原版普通遗物，不把它们重新注册，避免改变原版遗物归属。
        base.GetUnlockedRelics(unlockState)
            .Concat(ModelDb.RelicPool<IroncladRelicPool>().GetUnlockedRelics(unlockState)
                .Where(relic => relic.Rarity != RelicRarity.Starter))
            .DistinctBy(relic => relic.Id);
}

/// <summary>棍亩：OTTO 初始遗物，每场战斗开始时获得一层原版无实体，替换燃烧之血。</summary>
[Pool(typeof(OttoRelicPool))]
[CustomID("OTTO_TALISMAN")]
public sealed class OttoTalisman : CustomRelicModel
{
    public const string ImagePath = "res://OTTO/images/relics/otto_talisman.png";
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 原样使用用户提供的图片。界面负责缩放，小图、大图和悬停轮廓共用其透明边界。
    public override string PackedIconPath => ImagePath;
    protected override string BigIconPath => ImagePath;
    protected override string PackedIconOutlinePath => ImagePath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IntangiblePower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];

    // BaseLib 的原版初始遗物升级事件会调用这里，再由游戏移除棍亩并装上升级版。
    // 返回规范模型即可，游戏会创建属于当前玩家的可变实例，避免两个玩家共享状态。
    public override RelicModel GetUpgradeReplacement() => ModelDb.Relic<OttoUpgradedTalisman>();

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        // 与原版金刚杵使用相同的战斗开始时机。非战斗房间不会触发，也不会每回合重新给予。
        if (room is not CombatRoom) return;
        Flash();
        // 直接使用原版无实体，减伤、叠层与敌方回合末递减都由游戏统一处理。
        await PowerCmd.Apply<IntangiblePower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, null);
    }
}
