using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 6-1c 확인(BALANCE 19-2): 2일차 아침에 저장 → (가) 그대로 하루 더 = 3일차 아침 / (나) 끄고 다시 켜서 불러와 하루 더 = 3일차 아침.
    /// 두 3일차 아침이 같음(모아 · 인벤토리 · 주민 · 캠핑 · 부탁 · 꽃 · 묘목 · 오늘의 테마 · 물고기 그림자 · 곤충 · 난수 상태).
    /// 다시 켠 2일차 아침도 저장한 그날과 같음(그림자 · 곤충 · 오늘의 테마 포함).
    /// </summary>
    public class SaveRoundTripPlayTests : ToolSceneTest
    {
        protected override bool WithVillagers => true;
        protected override bool WithInsects => true;

        SaveManager save;

        [UnitySetUp]
        public IEnumerator FindSave()
        {
            save = Object.FindAnyObjectByType<SaveManager>();
            yield break;
        }

        IEnumerator Sleep()
        {
            Object.FindAnyObjectByType<DayCycle>().Sleep();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LoadThenPlay_EqualsPlayWithoutSaving()
        {
            // 하루를 조금 바꿔 둠(꽃에 물 · 묘목) — 다음 아침 난수(번식 · 시듦)가 쓰이게
            var garden = ground.GetComponent<GardenKeeper>();
            foreach (var flower in ground.Objects())
                if (flower.kind == GroundObjectKind.Flower) garden.Garden.Water(flower.Cell);
            Assert.IsNotNull(ground.Placer.Spawn(GroundObjectKind.Sapling, FindOpenSpot()));

            yield return Sleep();                                  // 2일차 아침 저장
            string day2 = SaveCodec.ToJson(save.Capture());
            string copy = Path.Combine(TestSaveFolder, "day2.json");
            File.Copy(save.FilePath, copy, true);

            yield return Sleep();                                  // (가) 그대로 3일차
            string withoutReload = SaveCodec.ToJson(save.Capture());

            File.Copy(copy, save.FilePath, true);                  // (나) 2일차 저장으로 다시 켬
            SceneManager.LoadScene("VillagePrototype");
            yield return null;
            yield return null;
            save = Object.FindAnyObjectByType<SaveManager>();
            yield return null;
            yield return Tap(keyboard.fKey);                       // 시작 화면(6-1d): 이어하기
            Assert.IsTrue(save.Loaded);
            Assert.AreEqual(day2, SaveCodec.ToJson(save.Capture()), "다시 켠 2일차 아침 = 저장한 2일차 아침(그림자 · 곤충 · 오늘의 테마 · 난수까지)");

            yield return Sleep();
            string withReload = SaveCodec.ToJson(save.Capture());
            Assert.AreEqual(withoutReload, withReload, "저장 → 불러와 진행 = 저장 없이 진행");
        }
    }
}
