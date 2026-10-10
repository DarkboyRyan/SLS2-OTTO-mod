using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace OttoMod;

/// <summary>形态只替换主体精灵。伙伴、碰撞范围、特效点和原有受击/死亡动画继续使用原视觉树。</summary>
public static class AncientFormVisuals
{
    public const string FormNode = "OttoAncientForm";
    public static void Sync(Creature owner)
    {
        var visuals = owner.GetCreatureNode()?.Visuals;
        if (visuals is null || !GodotObject.IsInstanceValid(visuals)) return;
        SetActive(visuals, !owner.IsDead && owner.HasPower<AncientFormPower>());
    }
    public static void Clear(Creature owner)
    {
        if (owner.GetCreatureNode()?.Visuals is { } visuals) SetActive(visuals, false);
    }
    public static Node2D? SetActive(Node2D visuals, bool active)
    {
        var body = visuals.GetNodeOrNull<Node2D>("Visuals/Body");
        var original = body?.GetNodeOrNull<Sprite2D>("Sprite");
        if (body is null || original is null) return null;
        var old = body.GetNodeOrNull<Node2D>(FormNode);
        original.Visible = !active;
        if (!active)
        {
            if (old is not null) { body.RemoveChild(old); old.QueueFree(); }
            return null;
        }
        if (old is not null) return old; // 重复进入只刷新玩法计时，不创建第二个摆动动画。
        var root = new Node2D { Name = FormNode };
        body.AddChild(root);
        var texture = ResourceLoader.Load<Texture2D>("res://OTTO/images/character/otto_ancient_body.tres");
        const float height = 304;
        root.AddChild(new Sprite2D
        {
            Name = "Sprite", Texture = texture, Position = new Vector2(0, -height / 2),
            Scale = Vector2.One * (height / texture.GetHeight()), TextureFilter = CanvasItem.TextureFilterEnum.Linear
        });
        // 以脚底为轴持续左右摇摆；不改图片像素，也不与原动画的贴图轨道争夺同一个 Sprite。
        root.Position = new Vector2(-12, 0);
        root.Rotation = -.055f;
        var sway = root.CreateTween().SetLoops().SetParallel();
        sway.TweenProperty(root, "position:x", 12f, .65).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        sway.TweenProperty(root, "rotation", .055f, .65).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        sway.Chain().TweenProperty(root, "position:x", -12f, .65).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        sway.Parallel().TweenProperty(root, "rotation", -.055f, .65).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        return root;
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class AncientFormReadyPatch
{
    [HarmonyPostfix]
    private static void Restore(NCreature __instance)
    {
        AncientFormVisuals.Sync(__instance.Entity);
        if (__instance.Entity.HasPower<AncientFormPower>()) AncientSpeech.Sync(__instance.Entity);
    }
}
