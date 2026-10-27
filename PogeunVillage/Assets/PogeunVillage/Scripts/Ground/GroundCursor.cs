using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 앞 칸 표시. 결정 (2026-10-27): 칸을 다루는 도구(ToolDefinition.usesGroundCell: 삽 · 물뿌리개)를 들었을 때만
    /// 발 앞 칸에 얇은 사각 테두리를 띄움. 막힌 칸이면 색이 바뀜(프로퍼티 블록). 칸이 바뀔 때만 위치 · 색을 갱신.
    /// </summary>
    public class GroundCursor : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        const int IgnoreRaycastLayer = 2;

        public WorldSettings settings;
        public VillageGround ground;
        public PlayerTools tools;
        [Tooltip("테두리 모습(켜고 끄는 대상)")]
        public GameObject outline;

        public bool Visible => outline && outline.activeSelf;
        public Vector2Int Cell { get; private set; }

        Renderer[] renderers;
        MaterialPropertyBlock block;
        bool hasCell;

        void Awake()
        {
            if (!settings || !ground || !tools || !outline)
            {
                Debug.LogError($"{name}: 앞 칸 표시 설정(settings · ground · tools · outline)이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            renderers = outline.GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            outline.SetActive(false);
        }

        void LateUpdate()
        {
            bool show = tools.Equipped && tools.Equipped.usesGroundCell && ground.Grid != null;
            if (!show)
            {
                if (outline.activeSelf) outline.SetActive(false);
                hasCell = false;
                return;
            }

            Transform player = tools.transform;
            Vector3 p = player.position, f = player.forward;
            Vector2Int cell = ground.Grid.FrontCell(new Vector2(p.x, p.z), new Vector2(f.x, f.z), settings.groundCursorReach);
            if (!hasCell || cell != Cell)
                MoveTo(cell);
            if (!outline.activeSelf) outline.SetActive(true);
        }

        void MoveTo(Vector2Int cell)
        {
            Cell = cell;
            hasCell = true;
            Vector2 c = ground.Grid.CellCenter(cell);
            Vector3 top = new(c.x, 50f, c.y);
            float y = Physics.Raycast(top, Vector3.down, out RaycastHit hit, 100f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore)
                ? hit.point.y : tools.transform.position.y;
            transform.position = new Vector3(c.x, y, c.y);

            block.SetColor(BaseColorId, ground.Grid.IsBlocked(cell) ? settings.groundCursorBlockedColor : settings.groundCursorColor);
            foreach (var r in renderers)
                r.SetPropertyBlock(block);
        }
    }
}
