using UnityEngine;
using UnityEngine.Rendering;

namespace PogeunVillage
{
    /// <summary>
    /// 월드를 작은 행성처럼 둥글게 보이게 하는 전역 설정.
    /// 실제 위치/물리는 평평한 그대로이고, CurvedToon 셰이더가 화면에 그릴 때만 휘어 보이게 합니다.
    /// 게임 카메라에서는 카메라와 플레이어(focus) 사이 물체를 반투명(디더링)으로 그리게 합니다.
    /// 수치는 WorldSettings.
    /// </summary>
    [ExecuteAlways]
    public class CurvedWorld : MonoBehaviour
    {
        static readonly int OriginId = Shader.PropertyToID("_PV_CurveOrigin");
        static readonly int ParamsId = Shader.PropertyToID("_PV_CurveParams");
        static readonly int OccluderTargetId = Shader.PropertyToID("_PV_OccluderTarget");
        static readonly int OccluderParamsId = Shader.PropertyToID("_PV_OccluderParams");

        public WorldSettings settings;

        public bool curveEnabled = true;

        [Tooltip("행성의 꼭대기가 될 대상(보통 플레이어). 비어 있으면 카메라 위치를 사용합니다.")]
        public Transform focus;

        [Tooltip("씬 뷰에서도 휘어진 모습으로 볼지 여부. 끄면 씬 뷰는 평평하게 보여 배치 작업이 편합니다.")]
        public bool curveInSceneView = true;

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Shader.SetGlobalVector(ParamsId, Vector4.zero);
            Shader.SetGlobalVector(OccluderTargetId, Vector4.zero);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (!settings)
            {
                Shader.SetGlobalVector(ParamsId, Vector4.zero);
                Shader.SetGlobalVector(OccluderTargetId, Vector4.zero);
                cam.ResetCullingMatrix();
                return;
            }

            bool isSceneView = cam.cameraType == CameraType.SceneView;
            bool on = curveEnabled
                      && cam.cameraType != CameraType.Preview
                      && (!isSceneView || curveInSceneView);

            Vector3 origin = focus ? focus.position : cam.transform.position;
            Shader.SetGlobalVector(OriginId, new Vector4(origin.x, 0f, origin.z, 0f));
            Shader.SetGlobalVector(ParamsId, new Vector4(settings.curveRadius, on ? 1f : 0f, 0f, 0f));

            // 컬링은 휘기 전(평평한) 위치로 계산되므로, 화면 위쪽 밖에 있다가 휘어져
            // 화면 안으로 내려오는 오브젝트(지붕, 나무 등)가 사라지지 않게 컬링 범위를 넓힘
            if (on && !cam.orthographic)
            {
                float fov = Mathf.Min(cam.fieldOfView * settings.cullingFovScale, 170f);
                var projection = Matrix4x4.Perspective(fov, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
                cam.cullingMatrix = projection * cam.worldToCameraMatrix;
            }
            else
            {
                cam.ResetCullingMatrix();
            }

            // 가림 반투명은 게임 카메라에서 플레이어가 있을 때만
            bool occlusion = focus && cam.cameraType == CameraType.Game;
            Vector3 target = focus ? focus.position + Vector3.up * settings.occlusionTargetHeight : Vector3.zero;
            Shader.SetGlobalVector(OccluderTargetId, new Vector4(target.x, target.y, target.z, occlusion ? 1f : 0f));
            Shader.SetGlobalVector(OccluderParamsId, new Vector4(
                settings.occlusionRadius, settings.occlusionMargin, settings.occlusionSoftness, settings.occlusionStrength));
        }
    }
}
