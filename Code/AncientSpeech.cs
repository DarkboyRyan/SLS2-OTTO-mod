using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Helpers;

namespace OttoMod;

/// <summary>每位角色只有一个语音播放器，随机语音不参与玩法随机数或等待出牌结算。</summary>
public partial class AncientSpeech : AudioStreamPlayer
{
    private const string NodeName = "OttoAncientSpeech";
    public static readonly string[] Clips =
    [
        "res://OTTO/audio/ancient/ai_ya_mi_no.ogg",
        "res://OTTO/audio/ancient/ham.ogg",
        "res://OTTO/audio/ancient/one_hand.ogg",
        "res://OTTO/audio/ancient/spoken_reason.ogg",
        "res://OTTO/audio/ancient/wawo.ogg",
        "res://OTTO/audio/ancient/ancient_speech.ogg"
    ];
    private Creature _owner = null!;
    private bool _continuous;
    private int _lastClip = -1;

    private static AncientSpeech? Get(Creature owner, bool create)
    {
        if (NonInteractiveMode.IsActive || owner.IsDead) return null;
        var parent = owner.GetCreatureNode();
        if (parent == null || !GodotObject.IsInstanceValid(parent)) return null;
        var player = parent.GetNodeOrNull<AncientSpeech>(NodeName);
        if (player != null || !create) return player;
        player = new AncientSpeech { Name = NodeName, _owner = owner, Bus = OttoVoiceBus.Ensure() };
        player.Finished += player.OnFinished;
        parent.AddChild(player);
        return player;
    }

    public static void CardPlayed(CardModel card)
    {
        string? clip = card switch
        {
            JiBai => "ji_bai",
            SilverTerminal => "silver_terminal",
            PigGo => "pig_go",
            OttoRush => "rush",
            ChivePocket => "chive_pocket",
            HaliluTornado => "halilu_tornado",
            _ => null
        };
        if (clip == null && card is not AncientCard) return;
        var player = Get(card.Owner.Creature, true);
        if (player == null) return;
        player._continuous = player._owner.HasPower<AncientFormPower>();
        if (clip != null) player.PlayClip("res://OTTO/audio/cards/" + clip + ".ogg");
        else player.PlayRandom();
    }

    public static void Sync(Creature owner)
    {
        bool active = !owner.IsDead && owner.HasPower<AncientFormPower>();
        var player = Get(owner, active);
        if (player == null) return;
        player._continuous = active;
        if (!active) player.Stop();
        else if (!player.Playing) player.PlayRandom();
    }

    public static void Clear(Creature owner)
    {
        // 死亡时也必须能取得播放器；不使用拒绝死亡角色的 Get。
        if (NonInteractiveMode.IsActive) return;
        var parent = owner.GetCreatureNode();
        if (parent == null || !GodotObject.IsInstanceValid(parent)) return;
        if (parent.GetNodeOrNull<AncientSpeech>(NodeName) is not { } player) return;
        player._continuous = false;
        player.Stop();
    }

    private void PlayRandom()
    {
        // 使用表现层 RNG，且下一段不会与上一段相同。
        int next = _lastClip < 0 ? Random.Shared.Next(Clips.Length)
            : (_lastClip + Random.Shared.Next(1, Clips.Length)) % Clips.Length;
        _lastClip = next;
        PlayClip(Clips[next]);
    }

    private void PlayClip(string path)
    {
        var stream = ResourceLoader.Load<AudioStreamOggVorbis>(path);
        if (stream == null) return;
        Stop();
        stream.Loop = false;
        Stream = stream;
        UpdateVolume();
        Play();
    }

    private void OnFinished()
    {
        if (_continuous && !_owner.IsDead && _owner.HasPower<AncientFormPower>() &&
            CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding)
            PlayRandom();
    }

    private void UpdateVolume()
    {
        var settings = SaveManager.Instance.SettingsSave;
        VolumeLinear = OttoVoiceBus.PlaybackVolume(settings.VolumeMaster, settings.VolumeSfx);
    }

    public override void _Process(double delta)
    {
        if (_owner.IsDead || !CombatManager.Instance.IsInProgress ||
            CombatManager.Instance.IsOverOrEnding || (_continuous && !_owner.HasPower<AncientFormPower>()))
        {
            _continuous = false;
            Stop();
        }
        UpdateVolume();
    }

    public override void _ExitTree()
    {
        _continuous = false;
        Stop();
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class AncientSpeechCardPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result, CardPlay cardPlay) =>
        __result = PlayAfterHook(__result, cardPlay);

    private static async Task PlayAfterHook(Task original, CardPlay play)
    {
        await original;
        if (play.IsFirstInSeries) AncientSpeech.CardPlayed(play.Card);
    }
}
