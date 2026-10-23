using UnityEngine;
using UnityEngine.Rendering;

namespace PogeunVillage
{
    /// <summary>
    /// 월드를 작은 행성처럼 둥글게 보이게 하는 전역 설정.
    /// 실제 위치/물리는 평평한 그대로이고, CurvedToon 셰이더가 화면에 그릴 때만 휘어 보이게 합니다.
    /// </summary>
    [ExecuteAlways]
    public class CurvedWorld : MonoBehaviour
    {
        static readonly int OriginId = Shader.PropertyToID("_PV_CurveOrigin");
        static readonly int ParamsId = Shader.PropertyToID("_PV_CurveParams");

        [Tooltip("행성 반지름(m). 작을수록 더 둥글게 보입니다.")]
        [Range(20f, 400f)] public float radius = 70f;

        public bool curveEnabled = true;

        [Tooltip("행성의 꼭대기가 될 대상(보통 플레이어). 비어 있으면 카메라 위치를 사용합니다.")]
        public Transform focus;

        [Tooltip("씬 뷰에서도 휘어진 모습으로 볼지 여부. 끄면 씬 뷰는 평평하게 보여 배치 작업이 편합니다.")]
        public bool curveInSceneView = true;

        [Tooltip("휘어짐 때문에 오브젝트가 잘려 사라지지 않도록 컬링 시야각을 넓히는 배율")]
        [Range(1f, 3f)] public float cullingFovScale = 1.8f;

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Shader.SetGlobalVector(ParamsId, Vector4.zero);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            bool isSceneView = cam.cameraType == CameraType.SceneView;
            bool on = curveEnabled
                      && cam.cameraType != CameraType.Preview
                      && (!isSceneView || curveInSceneView);

            Vector3 origin = focus ? focus.position : cam.transform.position;
            Shader.SetGlobalVector(OriginId, new Vector4(origin.x, 0f, origin.z, 0f));
            Shader.SetGlobalVector(ParamsId, new Vector4(radius, on ? 1f : 0f, 0f, 0f));

            // 컬링은 휘기 전(평평한) 위치로 계산되므로, 화면 위쪽 밖에 있다가 휘어져
            // 화면 안으로 내려오는 오브젝트(지붕, 나무 등)가 사라지지 않게 컬링 범위를 넓힘
            if (on && !cam.orthographic)
            {
                float fov = Mathf.Min(cam.fieldOfView * cullingFovScale, 170f);
                var projection = Matrix4x4.Perspective(fov, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
                cam.cullingMatrix = projection * cam.worldToCameraMatrix;
            }
            else
            {
                cam.ResetCullingMatrix();
            }
        }
    }
}
