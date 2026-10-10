using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace OttoMod;

/// <summary>只负责伙伴的呈现，不参与随机或数值结算；多人玩家各自在自己的视觉节点下持有伙伴。</summary>
public static class ZooVisuals
{
    private const string NodeName = "OttoZooCompanion";
    private static readonly Vector2 Home = new(112, 0);

    public static void Sync(Creature owner)
    {
        var visuals = owner.GetCreatureNode()?.Visuals;
        if (visuals is null || !GodotObject.IsInstanceValid(visuals)) return;
        ClearFrom(visuals);
        if (owner.IsDead) return;
        AttachMany(visuals, Zoo.All(owner).Select(p => p.SpriteKey));
    }

    /// <summary>把素材贴到 OTTO 右侧脚边；开放该方法方便独立预览和真实引擎验收。</summary>
    public static Node2D Attach(Node2D visuals, string spriteKey)
    {
        ClearFrom(visuals);
        return AddBody(visuals, spriteKey, NodeName, Home, 172);
    }

    /// <summary>多伙伴共用一个容器并按固定顺序排列；保留原有单伙伴尺寸和无头顶名字的设置。</summary>
    public static Node2D? AttachMany(Node2D visuals, IEnumerable<string> spriteKeys)
    {
        var order = new[] { "xuan_dog", "shan_ni_ruo", "huo_xing_bao" };
        var keys = order.Where(spriteKeys.Contains).ToArray();
        ClearFrom(visuals);
        if (keys.Length == 0) return null;
        if (keys.Length == 1) return Attach(visuals, keys[0]);
        var group = new Node2D { Name = NodeName };
        visuals.AddChild(group);
        for (int i = 0; i < keys.Length; i++)
            AddBody(group, keys[i], keys[i], new Vector2(94 + i * 88, i == 1 ? 0 : 10), 146);
        return group;
    }

    private static Node2D AddBody(Node2D parent, string spriteKey, string nodeName, Vector2 home, float height)
    {
        // 美术导入脚本写入 AtlasTexture，把透明留白裁切掉，统一以脚底为锚点。
        var texture = ResourceLoader.Load<Texture2D>($"res://OTTO/images/companions/{spriteKey}_body.tres");
        var root = new Node2D { Name = nodeName, Position = home, ZIndex = 1 };
        root.SetMeta("home", home);
        parent.AddChild(root);
        var body = new Node2D { Name = "Body" };
        root.AddChild(body);
        float scale = height / texture.GetHeight();
        var sprite = new Sprite2D
        {
            Name = "Sprite", Texture = texture, Scale = Vector2.One * scale,
            Position = new Vector2(0, -height / 2), TextureFilter = CanvasItem.TextureFilterEnum.Linear
        };
        body.AddChild(sprite);
        // 伙伴头顶不显示名字；召唤时淡入并轻轻弹起，空闲呼吸只作用于 Body。
        root.Modulate = new Color(1, 1, 1, 0);
        root.Position = home + new Vector2(0, 18);
        var enter = root.CreateTween().SetParallel();
        enter.TweenProperty(root, "modulate:a", 1f, .25);
        enter.TweenProperty(root, "position", home, .3).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        var idle = body.CreateTween().SetLoops();
        idle.TweenProperty(body, "scale:y", 1.025f, 1.1).SetTrans(Tween.TransitionType.Sine);
        idle.TweenProperty(body, "scale:y", 1f, 1.1).SetTrans(Tween.TransitionType.Sine);
        return root;
    }

    public static void Activate(Creature owner, string spriteKey)
    {
        var root = owner.GetCreatureNode()?.Visuals.GetNodeOrNull<Node2D>(NodeName);
        if (root is null) return;
        root = root.GetNodeOrNull<Node2D>(spriteKey) ?? root;
        if (!root.HasMeta("home")) return;
        var home = root.GetMeta("home").AsVector2();
        // 回合效果触发时的小跳，直接反馈是哪位伙伴在行动。
        var tween = root.CreateTween();
        tween.TweenProperty(root, "position", home + new Vector2(8, -12), .12);
        tween.TweenProperty(root, "position", home, .18);
    }

    public static void Clear(Creature owner)
    {
        var visuals = owner.GetCreatureNode()?.Visuals;
        if (visuals is not null) ClearFrom(visuals);
    }

    private static void ClearFrom(Node visuals)
    {
        var old = visuals.GetNodeOrNull<Node2D>(NodeName);
        if (old is null) return;
        // 立即脱离树再延迟释放，保证同一帧多次召唤也只有一个可见伙伴。
        visuals.RemoveChild(old);
        old.QueueFree();
    }
}

/// <summary>视觉树重建时从真实状态恢复伙伴，包括战斗重载或远端玩家视觉晚于状态创建的情况。</summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class ZooVisualReadyPatch
{
    [HarmonyPostfix]
    private static void AfterReady(NCreature __instance)
    {
        ZooVisuals.Sync(__instance.Entity);
    }
}
