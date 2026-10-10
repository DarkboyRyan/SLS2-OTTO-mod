using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Saves;

namespace OttoMod;

/// <summary>召唤音按实际成功召唤排队，每位角色只有一个播放器，不阻塞出牌。</summary>
public partial class ZooSummonAudio : AudioStreamPlayer
{
    private const string NodeName = "OttoZooSummonAudio";
    public static readonly string[] Clips =
    [
        "res://OTTO/audio/summon/xuan_dog.ogg",
        "res://OTTO/audio/summon/shan_ni_ruo.ogg",
        "res://OTTO/audio/summon/huo_xing_bao.ogg"
    ];
    private Creature _owner = null!;
    private float _overlapGain = 1f;
    private readonly Queue<string> _pending = new();

    public static void Enqueue(Creature owner, int kind)
        => Enqueue(owner, kind, NodeName);

    public static void PlayTogether(Creature owner, IEnumerable<int> kinds)
    {
        if (NonInteractiveMode.IsActive || owner.IsDead) return;
        var parent = owner.GetCreatureNode();
        if (parent == null || !GodotObject.IsInstanceValid(parent)) return;
        parent.GetNodeOrNull<ZooSummonAudio>(NodeName)?.Clear();
        var simultaneous = kinds.ToArray();
        foreach (int kind in simultaneous)
        {
            string name = NodeName + kind;
            parent.GetNodeOrNull<ZooSummonAudio>(name)?.Clear();
            Enqueue(owner, kind, name);
            if (parent.GetNodeOrNull<ZooSummonAudio>(name) is { } player)
            {
                player._overlapGain = 1f / Math.Max(1, simultaneous.Length);
                player.UpdateVolume();
            }
        }
    }

    private static void Enqueue(Creature owner, int kind, string nodeName)
    {
        if (NonInteractiveMode.IsActive || owner.IsDead) return;
        var parent = owner.GetCreatureNode();
        if (parent == null || !GodotObject.IsInstanceValid(parent)) return;
        var player = parent.GetNodeOrNull<ZooSummonAudio>(nodeName);
        if (player == null)
        {
            player = new ZooSummonAudio { Name = nodeName, _owner = owner, Bus = OttoVoiceBus.Ensure() };
            player.Finished += player.PlayNext;
            parent.AddChild(player);
        }
        player._pending.Enqueue(Clips[kind]);
        if (!player.Playing) player.PlayNext();
    }

    private bool CanPlay => !_owner.IsDead && CombatManager.Instance.IsInProgress &&
        !CombatManager.Instance.IsOverOrEnding;

    private void PlayNext()
    {
        if (!CanPlay)
        {
            Clear();
            return;
        }
        while (_pending.TryDequeue(out var path))
        {
            var stream = ResourceLoader.Load<AudioStreamOggVorbis>(path);
            if (stream == null) continue;
            stream.Loop = false;
            Stream = stream;
            UpdateVolume();
            Play();
            return;
        }
    }

    private void Clear()
    {
        _pending.Clear();
        Stop();
    }

    private void UpdateVolume()
    {
        var settings = SaveManager.Instance.SettingsSave;
        VolumeLinear = OttoVoiceBus.PlaybackVolume(settings.VolumeMaster, settings.VolumeSfx) * _overlapGain;
    }

    public override void _Process(double delta)
    {
        if (!CanPlay) Clear();
        UpdateVolume();
    }

    public override void _ExitTree() => Clear();
}
