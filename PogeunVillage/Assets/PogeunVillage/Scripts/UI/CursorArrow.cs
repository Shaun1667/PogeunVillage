using UnityEngine;
using UnityEngine.UI;

namespace PogeunVillage
{
    /// <summary>
    /// 화면 속 마우스 화살표(인트로 P-5): 텍스처 없이 그리는 UI 도형. 화살 끝이 사각형 왼쪽 위 — 피벗을 (0, 1)로 두면 위치가 곧 끝점.
    /// </summary>
    public class CursorArrow : MaskableGraphic
    {
        // 사각형 안 비율(왼쪽 아래 0, 오른쪽 위 1). 머리 2조각 + 꼬리 2조각
        static readonly Vector2[] Points =
        {
            new(0f, 1f), new(0f, 0.2f), new(0.22f, 0.38f),
            new(0f, 1f), new(0.22f, 0.38f), new(0.72f, 0.44f),
            new(0.2f, 0.42f), new(0.42f, 0.44f), new(0.62f, 0.06f),
            new(0.2f, 0.42f), new(0.62f, 0.06f), new(0.42f, 0f),
        };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            for (int i = 0; i < Points.Length; i++)
                vh.AddVert(new Vector3(r.xMin + r.width * Points[i].x, r.yMin + r.height * Points[i].y), color, Vector4.zero);
            for (int i = 0; i < Points.Length; i += 3)
                vh.AddTriangle(i, i + 1, i + 2);
        }
    }
}
