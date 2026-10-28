using UnityEngine;

namespace PogeunVillage
{
    /// <summary>놓인 가구 하나(3-4a). F → FurnitureKeeper가 '들기 / 그대로'를 물음.</summary>
    [RequireComponent(typeof(GridObject))]
    public class PlacedFurniture : MonoBehaviour
    {
        public FurnitureDefinition Definition { get; private set; }
        public int Rotation { get; private set; }

        FurnitureKeeper keeper;
        Interactable interactable;

        public void Setup(FurnitureKeeper owner, FurnitureDefinition definition, int rotation)
        {
            keeper = owner;
            Definition = definition;
            Rotation = rotation;
            interactable = GetComponent<Interactable>();
            if (interactable)
            {
                interactable.displayName = definition.displayName;
                interactable.Interacted += OnInteract;
            }
        }

        void OnDestroy()
        {
            if (interactable) interactable.Interacted -= OnInteract;
        }

        void OnInteract(Interactable _, PlayerInteractor by) => keeper.AskPickUp(this);
    }
}
