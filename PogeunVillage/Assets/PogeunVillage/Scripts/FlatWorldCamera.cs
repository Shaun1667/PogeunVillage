using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 이 카메라로 그릴 때는 둥근 행성 휨 · 가림 반투명을 끔(CurvedWorld가 확인). 맵 바깥 장면(인트로 방, P-5)용.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FlatWorldCamera : MonoBehaviour
    {
    }
}
