using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 꽃 모습(P-3): 꽃의 "Petals" 아래 렌더러 — Varco3D 꽃 모델은 종류마다 메시 · 재질 · 크기가 달라, 번식 · 불러오기 때 셋을 함께 옮김.
    /// 모습 이름은 재질 이름(저장 `CellData.look`).
    /// </summary>
    public readonly struct FlowerLook
    {
        public readonly Material material;
        public readonly Mesh mesh;
        public readonly Vector3 scale;

        FlowerLook(Material material, Mesh mesh, Vector3 scale)
        {
            this.material = material;
            this.mesh = mesh;
            this.scale = scale;
        }

        public string Name => material ? material.name : "";
        public bool IsValid => material;

        public static Renderer Petals(GridObject flower)
        {
            var t = flower ? flower.transform.Find("Petals") : null;
            return t ? t.GetComponentInChildren<Renderer>() : null;
        }

        public static FlowerLook Of(GridObject flower)
        {
            var petals = Petals(flower);
            if (!petals)
                return default;
            var filter = petals.GetComponent<MeshFilter>();
            return new FlowerLook(petals.sharedMaterial, filter ? filter.sharedMesh : null, flower.transform.Find("Petals").localScale);
        }

        public void ApplyTo(GridObject flower)
        {
            var petals = Petals(flower);
            if (!petals || !IsValid)
                return;
            petals.sharedMaterial = material;
            var filter = petals.GetComponent<MeshFilter>();
            if (filter && mesh)
                filter.sharedMesh = mesh;
            flower.transform.Find("Petals").localScale = scale;
        }
    }
}
