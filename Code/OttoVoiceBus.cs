using Godot;

namespace OttoMod;

/// <summary>使用现有 Master 混音通路，不在 FMOD 音频线程运行期间改动总线拓扑。</summary>
internal static class OttoVoiceBus
{
    public static string Ensure() => "Master";

    // 游戏已把主音量平方应用到 Godot Master 总线；播放器不能再乘一次。
    // 保留显式主音量静音保护，其余主音量变化由现有总线控制。
    // 语音相对音效提高 6 dB；录音峰值已离线限制，不添加运行时总线效果。
    public static float PlaybackVolume(float masterVolume, float sfxVolume) =>
        masterVolume <= 0 ? 0 : Mathf.Min(1f, Mathf.Pow(sfxVolume, 2) * 2f);
}
