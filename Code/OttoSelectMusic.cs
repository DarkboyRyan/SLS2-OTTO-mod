using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;

namespace OttoMod;

/// <summary>选角界面的单次选角音乐；节点随界面释放，音量读取游戏设置。</summary>
public partial class OttoSelectMusic : AudioStreamPlayer
{
    public const string AudioPath = "res://OTTO/audio/otto_select.ogg";
    internal static OttoSelectMusic? Active { get; private set; }

    internal static void Select(NCharacterSelectScreen screen, bool selected)
    {
        StopSelected();
        if (!selected) return;
        var stream = ResourceLoader.Load<AudioStreamOggVorbis>(AudioPath);
        if (stream == null)
        {
            MainFile.Logger.Warn("OTTO select music could not be loaded.");
            return;
        }
        stream.Loop = false;
        var player = new OttoSelectMusic { Name = "OttoSelectMusic", Stream = stream };
        player.Finished += () =>
        {
            if (Active == player) StopSelected();
        };
        screen.AddChild(player);
        Active = player;
        player.UpdateVolume();
        // 暂时压低 FMOD 菜单音乐，不修改用户的音量设置或停止其播放进度。
        NAudioManager.Instance?.SetBgmVol(0);
        player.Play();
    }

    internal static void StopSelected()
    {
        var player = Active;
        Active = null;
        if (GodotObject.IsInstanceValid(player))
        {
            player!.Stop();
            player.QueueFree();
        }
        if (player != null)
            NAudioManager.Instance?.SetBgmVol(SaveManager.Instance.SettingsSave.VolumeBgm);
    }

    private void UpdateVolume()
    {
        var settings = SaveManager.Instance.SettingsSave;
        VolumeLinear = settings.VolumeMaster <= 0 ? 0 : Mathf.Pow(settings.VolumeBgm, 2);
    }

    public override void _Process(double delta)
    {
        if (GetParent() is not Control screen || !screen.IsVisibleInTree())
        {
            StopSelected();
            return;
        }
        UpdateVolume();
    }

    public override void _ExitTree()
    {
        if (Active == this) StopSelected();
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class OttoSelectMusicSelectionPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectScreen __instance,
        NCharacterSelectButton charSelectButton, CharacterModel characterModel) =>
        OttoSelectMusic.Select(__instance, characterModel is OttoCharacter &&
            !charSelectButton.IsLocked && !charSelectButton.IsRandom);
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuClosed))]
internal static class OttoSelectMusicClosePatch
{
    [HarmonyPrefix]
    private static void Prefix() => OttoSelectMusic.StopSelected();
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.BeginRun))]
internal static class OttoSelectMusicEmbarkPatch
{
    [HarmonyPrefix]
    private static void Prefix() => OttoSelectMusic.StopSelected();
}

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetBgmVol))]
internal static class OttoSelectMusicVolumePatch
{
    // 在选角时调整音乐音量，也保持原菜单音乐静音；自定义播放器读取实际设置。
    [HarmonyPrefix]
    private static void Prefix(ref float volume)
    {
        if (OttoSelectMusic.Active != null) volume = 0;
    }
}
