using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 집터 하나(결정 2026-10-28: 12곳). 주민이 살면 그레이박스 집(벽 · 지붕을 주민 색으로, 프로퍼티 블록)을, 비어 있으면 표지판을 보여 줌.
    /// 주민은 현관(door)에서 나오고 들어감.
    /// </summary>
    public class HousePlot : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public Transform door;
        public GameObject house;
        public GameObject emptySign;
        public Renderer[] wallRenderers = { };
        public Renderer[] roofRenderers = { };

        public VillagerDefinition Resident { get; private set; }

        MaterialPropertyBlock block;

        public Vector3 DoorPosition => door ? door.position : transform.position;

        public void Show(VillagerDefinition resident)
        {
            Resident = resident;
            if (house) house.SetActive(resident);
            if (emptySign) emptySign.SetActive(!resident);
            if (!resident)
                return;
            block ??= new MaterialPropertyBlock();
            Tint(wallRenderers, resident.bodyColor);
            Tint(roofRenderers, resident.accentColor);
        }

        void Tint(Renderer[] renderers, Color color)
        {
            block.SetColor(BaseColorId, color);
            foreach (var r in renderers)
                if (r) r.SetPropertyBlock(block);
        }
    }
}
