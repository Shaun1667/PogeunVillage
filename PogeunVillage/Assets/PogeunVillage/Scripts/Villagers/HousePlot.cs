using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 집터 하나(결정 2026-10-28: 12곳). 주민이 살면 그 주민의 집 모델(P-3b, VillagerDefinition.houseModel)을 세우고 그레이박스 집은 충돌만 남김.
    /// 모델이 없으면 그레이박스 집(벽 · 지붕을 주민 색으로, 프로퍼티 블록). 비어 있으면 표지판. 주민은 현관(door)에서 나오고 들어감.
    /// </summary>
    public class HousePlot : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public Transform door;
        public GameObject house;
        public GameObject emptySign;
        public Renderer[] wallRenderers = { };
        public Renderer[] roofRenderers = { };
        [Tooltip("주민 집 모델을 세움 — 캠핑 구역(텐트)은 끔(P-3b)")]
        public bool showsResidentHouse = true;
        [Tooltip("주민 집 모델이 있으면 감추는 그레이박스 렌더러(충돌은 그대로)")]
        public Renderer[] greyboxRenderers = { };

        public VillagerDefinition Resident { get; private set; }

        MaterialPropertyBlock block;
        GameObject model;
        VillagerDefinition modelOf;

        /// <summary>지금 세운 집 모델(없으면 null)</summary>
        public GameObject Model => model;

        public Vector3 DoorPosition => door ? door.position : transform.position;

        public void Show(VillagerDefinition resident)
        {
            Resident = resident;
            if (house) house.SetActive(resident);
            if (emptySign) emptySign.SetActive(!resident);
            if (!resident)
                return;
            ShowModel(showsResidentHouse ? resident.houseModel : null, resident);
            if (model)
                return;
            block ??= new MaterialPropertyBlock();
            Tint(wallRenderers, resident.bodyColor);
            Tint(roofRenderers, resident.accentColor);
        }

        void ShowModel(GameObject prefab, VillagerDefinition resident)
        {
            if (modelOf != resident)
            {
                if (model)
                {
                    model.SetActive(false);  // Destroy는 프레임 끝이라 그동안 보이지 않게
                    Destroy(model);
                }
                model = prefab && house ? Instantiate(prefab, house.transform) : null;
                modelOf = resident;
            }
            foreach (var r in greyboxRenderers)
                if (r) r.enabled = !model;
        }

        void Tint(Renderer[] renderers, Color color)
        {
            block.SetColor(BaseColorId, color);
            foreach (var r in renderers)
                if (r) r.SetPropertyBlock(block);
        }
    }
}
