/// <summary>
/// 过场视频协程占用期间为 true，供 <see cref="SfxLoopRandomGap"/> 等逻辑暂停环境音。
/// 由 <see cref="Level1IntroVideo"/> / <see cref="LevelOutroVideo"/> 在协程内 Enter，finally 中 Exit。
/// </summary>
public static class CutscenePlaybackGate
{
    static int _depth;

    public static bool IsCutscenePlaying => _depth > 0;

    public static void Enter()
    {
        _depth++;
    }

    public static void Exit()
    {
        if (_depth > 0)
            _depth--;
    }
}
