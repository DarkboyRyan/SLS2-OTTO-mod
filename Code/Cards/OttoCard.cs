using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.Attributes;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace OttoMod;

/// <summary>OTTO 独立卡池；只注册到 OTTO，不把新卡混入原版角色奖励。</summary>
[CustomID("OTTO_CARD_POOL")]
public sealed class OttoCardPool : CustomCardPoolModel
{
    public const string OttoEnergyIconPath = "res://OTTO/images/ui/otto_energy.png";

    public const string OttoTextEnergyIconPath = "res://OTTO/images/ui/otto_energy_text.png";

    public override string Title => "OTTO";
    // 文字图片按原尺寸插入富文本，必须使用独立的24×24纹理。
    // 不再覆写 EnergyColorName；BaseLib 会用卡池 ID 定位下面两份自定义资源。
    public override string BigEnergyIconPath => OttoEnergyIconPath;
    public override string TextEnergyIconPath => OttoTextEnergyIconPath;
    public override Color DeckEntryCardColor => new("CBAB63");
    public override bool IsColorless => false;
}

/// <summary>共用基类：通过 Pool 自动注册；优先使用指定卡面，其余借用相近的原版插画。</summary>
[Pool(typeof(OttoCardPool))]
public abstract class OttoCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    // 正式标记格挡效果，供卡面预览与v5跨职业随机池共同使用。
    public override bool GainsBlock => CanonicalVars.Any(v => v is BlockVar or CalculatedBlockVar);
    protected virtual CardModel PortraitSource => ModelDb.Card<StrikeIronclad>();
    // 子类只需指定 CustomPortraitPath，普通卡面、替代插画和放大预览就会使用同一张图片。
    public override string PortraitPath => CustomPortraitPath ?? PortraitSource.PortraitPath;
    public override string BetaPortraitPath => CustomPortraitPath ?? PortraitSource.BetaPortraitPath;
    public override Texture2D CustomPortrait => CustomPortraitPath is { } path
        ? ResourceLoader.Load<Texture2D>(path)
        : PortraitSource.Portrait;
}
