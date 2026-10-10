using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace OttoMod;

/// <summary>假商人默认直接播放 Spine 动画；OTTO 使用 AnimationPlayer，必须走自己的待机动画。</summary>
[HarmonyPatch(typeof(NFakeMerchant), "StartCharacterAnimation")]
internal static class OttoFakeMerchantPatch
{
    [HarmonyPrefix]
    private static bool PlayOttoIdle(NCreatureVisuals visuals)
    {
        // 按人物贴图识别，只处理 OTTO，不干扰同场其他职业和商人的骨骼动画。
        var sprite = visuals.GetNodeOrNull<Sprite2D>("Visuals/Body/Sprite");
        if (sprite?.Texture?.ResourcePath != "res://OTTO/images/character/otto_combat.png") return true;
        visuals.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("idle");
        return false;
    }
}

/// <summary>商店场景由 BaseLib 包装为 NMerchantCharacter；直接处理 OTTO 动画，避免包装节点被当作 SpineSprite。</summary>
[HarmonyPatch]
internal static class OttoMerchantAnimationPatch
{
    private static AnimationPlayer? Animation(NMerchantCharacter character)
    {
        var root = character.GetNodeOrNull<Node2D>("OttoMerchantVisuals");
        if (root?.GetNodeOrNull<Sprite2D>("Visuals/Body/Sprite")?.Texture?.ResourcePath !=
            "res://OTTO/images/character/otto_combat.png") return null;
        return root.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
    }

    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static bool Ready(NMerchantCharacter __instance)
    {
        if (Animation(__instance) is not { } animation) return true;
        animation.Play("idle");
        return false;
    }

    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static bool Play(NMerchantCharacter __instance, string anim)
    {
        if (Animation(__instance) is not { } animation) return true;
        var name = anim == "die" ? "die" : "idle";
        if (animation.HasAnimation(name)) animation.Play(name);
        return false;
    }
}
