using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 세이브 수치. 값의 기준은 BALANCE.md 19번. 에셋: Assets/PogeunVillage/Data/SaveRules.asset
    /// </summary>
    [CreateAssetMenu(fileName = "SaveRules", menuName = "Pogeun Village/Save Rules")]
    public class SaveRules : ScriptableObject
    {
        [Tooltip("저장 파일 이름(persistentDataPath 안)")]
        public string fileName = "save.json";
        [Tooltip("덮어쓰기 전 파일을 남기는 백업 이름")]
        public string backupFileName = "save.bak.json";
    }
}
