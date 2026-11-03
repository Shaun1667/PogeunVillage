using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 물건 모델 보이기(P-4i): 물건에 모델(ItemDefinition.pickupModel)이 있으면 그 자리의 그레이박스 렌더러를 숨기고 모델을 자식 "Model"로 붙임.
    /// 떨어진 물건(Pickup) · 마을 곤충(InsectBehaviour)이 함께 씀.
    /// </summary>
    public static class ItemLook
    {
        public static bool Show(ItemDefinition item, Transform parent)
        {
            if (!item || !item.pickupModel || parent.Find("Model"))
                return false;
            foreach (var greybox in parent.GetComponentsInChildren<Renderer>())
                greybox.enabled = false;
            var model = Object.Instantiate(item.pickupModel, parent);
            model.name = "Model";
            model.transform.localScale = Vector3.one * item.pickupScale;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = item.pickupMaterial;
            return true;
        }
    }
}
