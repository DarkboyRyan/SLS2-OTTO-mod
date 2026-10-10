using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Saves;

namespace OttoMod;

/// <summary>死亡音效不受战斗结束停播规则影响，倒地后保留最终姿态。</summary>
public partial class OttoDeathAudio : AudioStreamPlayer
{
    public bool DeathStarted { get; set; }

    private void UpdateVolume()
    {
        var settings = SaveManager.Instance.SettingsSave;
        VolumeLinear = OttoVoiceBus.PlaybackVolume(settings.VolumeMaster, settings.VolumeSfx);
    }

    public void PlayDeath()
    {
        if (DeathStarted) return;
        DeathStarted = true;
        Stream = ResourceLoader.Load<AudioStreamOggVorbis>("res://OTTO/audio/death.ogg");
        if (Stream is AudioStreamOggVorbis stream) stream.Loop = false;
        Bus = OttoVoiceBus.Ensure();
        UpdateVolume();
        Play();
    }

    public override void _Process(double delta) => UpdateVolume();
    public override void _ExitTree() => Stop();
}

[HarmonyPatch]
internal static class OttoDeathPresentation
{
    private const string AudioNode = "OttoDeathAudio";
    private static bool IsOtto(NCreature creature) =>
        !NonInteractiveMode.IsActive && creature.Entity.Player?.Character is OttoCharacter;

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
    [HarmonyPostfix]
    private static void Death(NCreature __instance, ref float __result)
    {
        if (!IsOtto(__instance)) return;
        var animation = __instance.Visuals.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animation == null || !animation.HasAnimation("die")) return;
        var audio = __instance.GetNodeOrNull<OttoDeathAudio>(AudioNode);
        if (audio == null)
        {
            audio = new OttoDeathAudio { Name = AudioNode };
            __instance.AddChild(audio);
        }
        if (audio.DeathStarted) return;
        AncientSpeech.Clear(__instance.Entity);
        AncientFormVisuals.Clear(__instance.Entity);
        animation.Play("die");
        audio.PlayDeath();
        __result = (float)animation.GetAnimation("die").Length;
    }

    // 原版无 Spine 的临时复活动画会调用 SpineAnimation；使用自己的起身动画。
    [HarmonyPatch(typeof(NCreature), "AnimTempRevive")]
    [HarmonyPrefix]
    private static bool Revive(NCreature __instance)
    {
        if (!IsOtto(__instance)) return true;
        if (__instance.GetNodeOrNull<OttoDeathAudio>(AudioNode) is { } audio)
        {
            audio.Stop();
            audio.DeathStarted = false;
        }
        __instance.Visuals.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("revive");
        return false;
    }
}
