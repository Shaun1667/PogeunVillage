using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-4c 확인 (영업 중 상점 주인 F → 가구 목록 창 · 8가지 · ★ 안 열린 줄 · F 사기(창은 열린 채) · 모아 표시 · 안 열린 가구는 못 삼 ·
    /// 닫았으면 알림만 · 판매함으로 절반에 되팔기 · 별이 처음 오르면 새 가구 알림 · 임시 가구 상자 없음). 오늘의 테마 가구 줄은 3-4d.
    /// </summary>
    public class FurnitureShopPlayTests : ToolSceneTest
    {
        Shop shop;
        FurnitureCounter counter;
        ListWindow window;
        DayCycle dayCycle;
        HudView hud;
        Wallet Wallet => player.GetComponent<PlayerWallet>().Wallet;

        [UnitySetUp]
        public IEnumerator FindShop()
        {
            shop = Object.FindAnyObjectByType<Shop>();
            counter = Object.FindAnyObjectByType<FurnitureCounter>();
            window = Object.FindAnyObjectByType<ListWindow>();
            dayCycle = Object.FindAnyObjectByType<DayCycle>();
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(counter);
            yield break;
        }

        void OpenHours() => dayCycle.Clock.Advance((shop.data.openHour - dayCycle.Clock.HourOfDay + 1) * 60);

        IEnumerator FaceFromFront(Component target)
        {
            Vector3 front = shop.transform.forward;
            Teleport(target.transform.position + front * 0.95f, -front);
            yield return Frames(3);
            Assert.AreSame(target.GetComponent<Interactable>(), interactor.Target, $"{target.name}가 대상");
        }

        string Shown => hud.noticeText.text + "|" + string.Join("|", hud.PendingNotices);

        [UnityTest]
        public IEnumerator Keeper_F_OpensList_BuyFence_StaysOpen()
        {
            OpenHours();
            yield return FaceFromFront(shop.keeper);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(window.IsOpen, "가구 목록 창");
            Assert.AreSame(counter, window.Source);
            Assert.AreEqual("가구 사기", window.titleText.text);
            Assert.AreEqual(9, window.listText.text.Split('\n').Length, "오늘의 테마(3-4d) + 기본 8가지");
            StringAssert.StartsWith("<color=#C0607A><b>오늘의 테마 · ", window.listText.text, "맨 위");
            StringAssert.Contains("나무 울타리 · 200 모아", window.listText.text);
            StringAssert.Contains("새집 · ★4에 열려요", window.listText.text);
            StringAssert.Contains("3,000 모아", window.footerText.text);
            Assert.AreEqual(0, Inv.Count("Fence"), "여는 F로 사지 않음");

            yield return Tap(keyboard.downArrowKey);              // 나무 울타리(맨 위는 오늘의 테마)
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(1, Inv.Count("Fence"));
            Assert.AreEqual(2800, Wallet.Moa);
            Assert.IsTrue(window.IsOpen, "사도 창은 열린 채");
            StringAssert.Contains("2,800 모아", window.footerText.text);
            StringAssert.Contains("샀어요", hud.noticeText.text);

            for (int i = 0; i < 3; i++)
                yield return Tap(keyboard.downArrowKey);           // 통나무 의자(★2)
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(0, Inv.Count("LogStool"));
            Assert.AreEqual(2800, Wallet.Moa, "안 열린 가구는 못 삼");
            StringAssert.Contains("★2에 들어와요", hud.noticeText.text);

            yield return Tap(keyboard.tabKey);
            Assert.IsFalse(window.IsOpen);
        }

        [UnityTest]
        public IEnumerator Closed_KeeperOnlyTellsHours()
        {
            Assert.IsFalse(shop.IsOpen, "6시는 문 닫음");
            yield return FaceFromFront(shop.keeper);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(window.IsOpen);
            StringAssert.Contains("문을 닫았어요", hud.noticeText.text);
        }

        [UnityTest]
        public IEnumerator SellBox_ResellsFurnitureAtHalf()
        {
            OpenHours();
            Assert.IsTrue(playerInventory.TryAdd(Item("Bench"), 2));
            yield return FaceFromFront(Object.FindAnyObjectByType<SellBox>());
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(0, Inv.Count("Bench"));
            Assert.AreEqual(3000 + 400 * 2, Wallet.Moa);
        }

        [UnityTest]
        public IEnumerator StarsRise_NewFurnitureNotice_OnlyFirstTime()
        {
            var rater = Object.FindAnyObjectByType<VillageRater>();
            var village = Object.FindAnyObjectByType<Village>(FindObjectsInactive.Include);
            Assert.AreEqual(1, counter.BestStars);
            var ids = village.Residents.Plots.Where(id => id != null).ToArray();
            int baseTotal = rater.Current.total - rater.Current.friendshipPoints;
            int each = (rater.rules.starThresholds[0] - baseTotal) / ids.Length + 1;  // 친밀도로 ★2 기준을 막 넘김
            foreach (string id in ids)
                village.Friendship.Begin(id, each);
            rater.Evaluate();
            yield return null;
            Assert.AreEqual(2, rater.Current.stars);
            StringAssert.Contains("새 가구가 들어왔어요: 통나무 의자 · 나무 벤치 (작업대 제작법도 열렸어요)", Shown);
            Assert.AreEqual(2, counter.BestStars);

            foreach (string id in ids)
                village.Friendship.Begin(id, -20);                 // ★1로 내려갔다가
            rater.Evaluate();
            Assert.AreEqual(1, rater.Current.stars);
            foreach (string id in ids)
                village.Friendship.Begin(id, each);                // 다시 ★2
            yield return WaitSeconds(settings.hudNoticeSeconds * 4);
            rater.Evaluate();
            yield return null;
            Assert.AreEqual(2, rater.Current.stars);
            StringAssert.DoesNotContain("새 가구", Shown, "다시 ★2가 돼도 알림 없음");
        }

        [Test]
        public void TempFurnitureBox_Removed()
        {
            Assert.IsNull(GameObject.Find("TempFurnitureBox"));
            Assert.AreEqual(8, counter.furniture.Length);
        }
    }
}
