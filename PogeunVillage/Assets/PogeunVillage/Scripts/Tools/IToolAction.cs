namespace PogeunVillage
{
    /// <summary>
    /// 땅 칸을 다루는 도구(삽 · 물뿌리개)의 동작. 플레이어에 붙여 두면 PlayerTools가 휘두를 때 차례로 묻습니다.
    /// 무언가를 했으면 true(= 맞음, 내구도 -1), 할 일이 없으면 false(허공).
    /// </summary>
    public interface IToolAction
    {
        bool TryUse(PlayerTools by, ToolDefinition tool);
    }
}
