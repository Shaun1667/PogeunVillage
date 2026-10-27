using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PogeunVillage.EditorTools
{
    /// <summary>
    /// EditMode 테스트 전체 실행. 메뉴: Pogeun Village > Run EditMode Tests
    /// 결과 요약은 콘솔에, 테스트별 결과는 Temp/PogeunVillageTestResults.txt에 남깁니다.
    /// </summary>
    public static class EditModeTestRunner
    {
        const string ResultPath = "Temp/PogeunVillageTestResults.txt";

        [MenuItem("Pogeun Village/Run EditMode Tests")]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callbacks = new ResultCallbacks(api);
            api.RegisterCallbacks(callbacks);
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        class ResultCallbacks : ICallbacks
        {
            readonly TestRunnerApi api;
            readonly List<string> lines = new();

            public ResultCallbacks(TestRunnerApi api) => this.api = api;

            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren)
                    lines.Add($"{result.TestStatus}\t{result.FullName}\t{result.Message}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                api.UnregisterCallbacks(this);
                string summary = $"[PogeunVillage] EditMode 테스트: 통과 {result.PassCount}, 실패 {result.FailCount}, 건너뜀 {result.SkipCount}";

                var text = new StringBuilder(summary).AppendLine();
                foreach (string line in lines) text.AppendLine(line);
                File.WriteAllText(ResultPath, text.ToString());

                if (result.FailCount > 0) Debug.LogError(summary);
                else Debug.Log(summary);
            }
        }
    }
}
