using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PogeunVillage.EditorTools
{
    /// <summary>
    /// 테스트 전체 실행. 메뉴: Pogeun Village > Run EditMode Tests / Run PlayMode Tests
    /// 결과는 Temp/PogeunVillageTestResults_{EditMode|PlayMode}.txt(테스트별 한 줄 + 마지막 줄 요약)와 콘솔 요약.
    /// PlayMode는 플레이 모드에 들어가며 도메인이 다시 로드되므로, 콜백을 에디터 로드 때마다 등록합니다(InitializeOnLoad).
    /// Test Runner 창에서 돌려도 같은 파일에 남습니다.
    /// </summary>
    [InitializeOnLoad]
    public static class TestRunMenu
    {
        static TestRunMenu()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new ResultCallbacks());
        }

        [MenuItem("Pogeun Village/Run EditMode Tests")]
        public static void RunEditMode() => Run(TestMode.EditMode);

        [MenuItem("Pogeun Village/Run PlayMode Tests")]
        public static void RunPlayMode() => Run(TestMode.PlayMode);

        static void Run(TestMode mode)
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(new Filter { testMode = mode }));
        }

        static string ResultPath(TestMode mode) => $"Temp/PogeunVillageTestResults_{mode}.txt";

        class ResultCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            // 도메인 리로드와 상관없도록, 끝날 때 결과 트리 전체를 한 번에 씀
            public void RunFinished(ITestResultAdaptor result)
            {
                TestMode mode = result.Test.TestMode;
                string summary = $"[PogeunVillage] {mode} 테스트: 통과 {result.PassCount}, 실패 {result.FailCount}, 건너뜀 {result.SkipCount}";

                var text = new StringBuilder();
                AppendLeaves(result, text);
                text.AppendLine(summary);
                File.WriteAllText(ResultPath(mode), text.ToString());

                if (result.FailCount > 0) Debug.LogError(summary);
                else Debug.Log(summary);
            }

            static void AppendLeaves(ITestResultAdaptor result, StringBuilder text)
            {
                if (!result.HasChildren)
                {
                    text.AppendLine($"{result.TestStatus}\t{result.FullName}\t{result.Message?.Replace('\n', ' ')}");
                    return;
                }
                foreach (var child in result.Children)
                    AppendLeaves(child, text);
            }
        }
    }
}
