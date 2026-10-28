using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PogeunVillage
{
    /// <summary>
    /// 실행할 때 원본 글꼴(TTF)에서 동적 TMP 글꼴을 새로 만들어 아래 글자 전부에 씌움(2-4).
    /// 저장된 글꼴 에셋에 실행 중 새 글자가 쌓여 파일이 바뀌고 가져오기 경고가 나는 것을 막음 — 저장된 에셋은 편집 화면용.
    /// </summary>
    public class RuntimeFont : MonoBehaviour
    {
        public Font sourceFont;

        public TMP_FontAsset Font { get; private set; }

        void Awake()
        {
            if (!sourceFont)
            {
                Debug.LogError($"{name}: 원본 글꼴이 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            Font = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            Font.name = sourceFont.name + " (실행 중)";
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                text.font = Font;
        }

        void OnDestroy()
        {
            if (Font)
                Destroy(Font);
        }
    }
}
