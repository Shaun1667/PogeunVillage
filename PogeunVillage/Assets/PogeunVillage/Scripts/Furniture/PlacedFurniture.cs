using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 놓인 가구 하나(3-4a). F → FurnitureKeeper가 '들기 / 그대로'를 물음.
    /// 테마 가구(3-4d)는 모양을 4가지가 함께 쓰고, 그 주민 색을 프로퍼티 블록으로 입힘.
    /// </summary>
    [RequireComponent(typeof(GridObject))]
    public class PlacedFurniture : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("테마 가구: 주민 몸 색을 입힐 부분")]
        public Renderer[] bodyRenderers = { };
        [Tooltip("테마 가구: 주민 포인트 색을 입힐 부분")]
        public Renderer[] accentRenderers = { };

        public FurnitureDefinition Definition { get; private set; }
        public int Rotation { get; private set; }

        FurnitureKeeper keeper;
        Interactable interactable;

        public void Setup(FurnitureKeeper owner, FurnitureDefinition definition, int rotation)
        {
            keeper = owner;
            Definition = definition;
            Rotation = rotation;
            Tint(definition);
            interactable = GetComponent<Interactable>();
            if (interactable)
            {
                interactable.displayName = definition.displayName;
                interactable.Interacted += OnInteract;
            }
        }

        /// <summary>테마 가구면 그 주민 색(미리보기도 씀)</summary>
        public void Tint(FurnitureDefinition definition)
        {
            var owner = definition ? definition.themeOwner : null;
            if (!owner)
                return;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, owner.bodyColor);
            foreach (var r in bodyRenderers) if (r) r.SetPropertyBlock(block);
            block.SetColor(BaseColorId, owner.accentColor);
            foreach (var r in accentRenderers) if (r) r.SetPropertyBlock(block);
        }

        void OnDestroy()
        {
            if (interactable) interactable.Interacted -= OnInteract;
        }

        void OnInteract(Interactable _, PlayerInteractor by) => keeper.AskPickUp(this);
    }
}
