using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 마을의 곤충 하나(3-7a): 꽃 위 · 물가에서는 둥실둥실 맴돌고, 나무 줄기 · 풀밭에서는 가만히 있다 가끔 들썩임.
    /// 잠자리채에 맞으면 Insects가 잡음. F를 누르면 잡는 법을 알려 줌.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class InsectBehaviour : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("곤충 색을 입힐 몸")]
        public Renderer[] bodyRenderers = { };
        [Tooltip("맴도는 반경(m) · 속도(바퀴/초)")]
        public float hoverRadius = 0.25f, hoverSpeed = 0.35f;
        [Tooltip("위아래 들썩임(m)")]
        public float bobHeight = 0.05f;

        public InsectDefinition Definition { get; private set; }
        public Vector3 Home { get; private set; }

        float phase;
        bool flies;

        public void Setup(InsectDefinition definition, Vector3 home, float startPhase)
        {
            Definition = definition;
            Home = home;
            phase = startPhase;
            flies = definition.site == InsectSite.Flower || definition.site == InsectSite.Water;
            transform.position = home;
            name = "Insect_" + definition.id;
            var interactable = GetComponent<Interactable>();
            interactable.displayName = definition.displayName;
            interactable.Interacted += (_, _) => GameNotice.Post($"[곤충] {definition.displayName} — 잠자리채를 들고 살금살금 다가가 Space", this);
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, definition.color);
            foreach (var r in bodyRenderers) if (r) r.SetPropertyBlock(block);
        }

        void Update()
        {
            if (!Definition)
                return;
            phase += Time.deltaTime;
            float bob = Mathf.Sin(phase * 6f) * bobHeight;
            if (!flies)
            {
                transform.position = Home + Vector3.up * Mathf.Max(0f, bob);
                return;
            }
            float a = phase * hoverSpeed * Mathf.PI * 2f;
            Vector3 offset = new(Mathf.Cos(a) * hoverRadius, bob, Mathf.Sin(a) * hoverRadius);
            transform.position = Home + offset;
            transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)));
        }
    }
}
