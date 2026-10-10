using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace OttoMod;

/// <summary>
/// 独立可选角色 OTTO。自定义名称、头像、选人海报和战斗形象。
/// 初始生命和金币独立配置，卡牌使用独立动物园卡池；PlaceholderCharacterModel 还会提供
/// 营火和商店动画、音效及地图标记等未替换的默认资源。
/// </summary>
[CustomID(CharacterId)]
public sealed class OttoCharacter : PlaceholderCharacterModel
{
    // 稳定的角色内部 ID：会写入存档，并作为本地化文本的键前缀。
    // 显示名称在 OTTO/localization 中修改，发布后不要随意改这个 ID。
    public const string CharacterId = "OTTO_CHARACTER";
    public const string PortraitPath = "res://OTTO/images/charui/otto.png";
    public const string SelectBackgroundPath = "res://OTTO/scenes/otto_select_bg.tscn";
    public const string CombatVisualPath = "res://OTTO/scenes/otto_combat.tscn";
    public const string OttoEnergyCounterScenePath = "res://OTTO/scenes/otto_energy_counter.tscn";

    // 获取游戏原有的铁甲战士模型。这里只读取它的配置，不修改原版角色。
    private static Ironclad Original => ModelDb.Character<Ironclad>();

    // 明确使用铁甲战士的占位资源，并让 OTTO 出现在正常角色选择列表中。
    public override string PlaceholderID => "ironclad";
    public override bool HideFromVanillaCharacterSelect => false;

    // OTTO 新对局以 91 点当前/最大生命和 91 金币开始；不会修改进行中的存档。
    // 性别和每回合能量仍读取铁甲战士配置（本机版本为 3 点能量）。
    public override CharacterGender Gender => Original.Gender;
    public override int StartingHp => 91;
    public override int StartingGold => 91;
    public override int MaxEnergy => Original.MaxEnergy;
    // 按用户表格配置 11 张初始牌：五打击、五防御、一呼朋唤友。
    public override IEnumerable<CardModel> StartingDeck =>
        Enumerable.Repeat<CardModel>(ModelDb.Card<OttoStrike>(), 5)
            .Concat(Enumerable.Repeat<CardModel>(ModelDb.Card<OttoDefend>(), 5))
            .Append(ModelDb.Card<CallFriends>());
    // 新对局只携带 OTTO 的初始遗物，不再同时获得燃烧之血。
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<OttoTalisman>()];
    public override IReadOnlyList<PotionModel> StartingPotions => Original.StartingPotions;

    // 卡牌与初始遗物使用 OTTO 内容；普通遗物奖励及药水继续沿用铁甲战士配置。
    public override CardPoolModel CardPool => ModelDb.CardPool<OttoCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<OttoRelicPool>();
    public override PotionPoolModel PotionPool => Original.PotionPool;

    // 保持原版的角色颜色、对话气泡和多人指示线样式。
    public override Color NameColor => Original.NameColor;
    public override Color EnergyLabelOutlineColor => Original.EnergyLabelOutlineColor;
    public override Color DialogueColor => Original.DialogueColor;
    public override VfxColor SpeechBubbleColor => Original.SpeechBubbleColor;
    public override Color MapDrawingColor => Original.MapDrawingColor;
    public override Color RemoteTargetingLineColor => Original.RemoteTargetingLineColor;
    public override Color RemoteTargetingLineOutline => Original.RemoteTargetingLineOutline;

    // 攻击在 0.15 秒完全伸拳，与原版伤害等待时间同步；施放仍使用原版节奏。
    public override float AttackAnimDelay => Original.AttackAnimDelay;
    public override float CastAnimDelay => Original.CastAnimDelay;

    // 声明资源路径，让游戏能预加载这份战斗场景。
    // 场景使用透明人物 PNG 和 AnimationPlayer，不需要 Spine 骨骼或 MegaSprite。
    // BaseLib 根据 Attack、Cast、Hit、Dead 等触发器播放对应的同名/小写动画。
    public override string CustomVisualPath => CombatVisualPath;

    // 商店使用独立场景路径，避免与战斗场景的不同节点类型转换相互覆盖。
    public override string CustomMerchantAnimPath => "res://OTTO/scenes/otto_merchant.tscn";

    public override string CustomRestSiteAnimPath => "res://OTTO/scenes/otto_rest_site.tscn";

    // 战斗能量计数器使用独立场景：人物图等比居中，数字标签由 BaseLib 补齐。
    public override string CustomEnergyCounterPath => OttoEnergyCounterScenePath;

    // 显式通过 BaseLib 的工厂转换，避免依赖自动转换注册的先后顺序。
    // CreateVisuals 的 BaseLib 补丁会调用此方法；工厂保留动画节点并补齐视觉组件。
    public override NCreatureVisuals CreateCustomVisuals() =>
        NodeFactory<NCreatureVisuals>.CreateFromScene(CombatVisualPath);

    // 与场景中的 die 动画时长一致，让游戏等待倒地结束后再处理后续流程。
    public override float DeathAnimTime => 0.7f;

    // 选人按钮和游戏内小头像都使用用户提供的原图；只由界面缩放，不改原图。
    public override string CustomCharacterSelectIconPath => PortraitPath;
    public override string CustomCharacterSelectLockedIconPath => PortraitPath;
    public override string CustomIconTexturePath => PortraitPath;

    // 选择 OTTO 时加载独立的 Control 场景，替换铁甲战士的选人背景。
    // 场景把完整照片放在右侧，左侧加深色渐变，为角色名称和介绍留出阅读区域。
    // 以后换背景时可以直接编辑这个 .tscn，不需要改战斗模型或原版角色。
    public override string CustomCharacterSelectBg => SelectBackgroundPath;

    // 游戏内左上角等位置需要 Control 节点，BaseLib 可把 PNG 转成头像控件。
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(PortraitPath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
}
