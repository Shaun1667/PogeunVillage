namespace PogeunVillage
{
    /// <summary>
    /// 휘두르기 대신 따로 동작하는 도구(낚싯대 3-6a). 플레이어에 붙여 두면 PlayerTools가 Space를 눌렀을 때 먼저 묻습니다.
    /// 그 도구를 맡으면 true(휘두르지 않음), 아니면 false.
    /// </summary>
    public interface IToolPress
    {
        bool TryPress(PlayerTools by, ToolDefinition tool);
    }
}
