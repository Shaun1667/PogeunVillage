using System;
using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// Varco3D 모델 목록(P-3, 결정 2026-10-30). 규격은 MODELING_SPEC.md — 원본(Art/Raw)은 높이 약 1 · 바닥 가운데 피벗이라
    /// 게임 크기는 여기 높이로 맞춤. 재질은 CurvedToon + 모델 색 텍스처(모델마다 하나).
    /// 에셋: Assets/PogeunVillage/Data/ModelLibrary.asset — 메뉴 `Pogeun Village/Import Varco Models`가 채움(있는 값은 건드리지 않음).
    /// </summary>
    [CreateAssetMenu(fileName = "ModelLibrary", menuName = "Pogeun Village/Model Library")]
    public class ModelLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public GameObject model;
            public Material material;
            [Tooltip("게임 안 높이(m)")]
            public float height = 1f;
            [Tooltip("정면을 +Z로 맞추는 Y 회전(도)")]
            public float yaw;
        }

        public List<Entry> entries = new();

        public Entry Find(string id) => entries.Find(e => e.id == id);
    }
}
