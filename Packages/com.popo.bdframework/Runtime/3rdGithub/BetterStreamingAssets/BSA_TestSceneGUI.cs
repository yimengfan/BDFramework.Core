// Better Streaming Assets, Piotr Gwiazdowski <gwiazdorrr+github at gmail.com>, 2017

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using UnityEngine.Profiling;
using System.Collections;
using Stopwatch = System.Diagnostics.Stopwatch;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Better.StreamingAssets
{
    public class BSA_TestSceneGUI : MonoBehaviour
    {
        public UnityEngine.UI.Text InProgressText;
        public string EditorApkPath = "BetterStreamingAssetsTest.apk";
        public int RepetitionCount = 10;
        public bool LogToFile = false;

        private class CoroutineHost : MonoBehaviour { }

        private class TestInfo
        {
            public ReadMode readMode;
            public TestType testType;
            public string path;
            public int attempts;
            public Exception error;
            public TimeSpan duration;
            public long bytesRead;
            public long memoryPeak;
            public long maxMemoryPeak;
        }


        private delegate void TestResultDelegate(TimeSpan avgDuration, long avgBytesRead, long avgMemoryPeak, long maxMemoryPeak, string[] assetNames);

        [Flags]
        private enum ReadMode
        {
            BSA = 1 << 0,
            WWW = 1 << 1,
            Direct = 1 << 5,
            UnityWebRequest = 1 << 6
        }

        [Flags]
        private enum TestType
        {
            CheckIfExists = 1 << 0,
            LoadBytes = 1 << 1,
        }

        private string m_status = string.Empty;
        private TestType m_testModes = TestType.CheckIfExists;
        private ReadMode m_readModes = ReadMode.WWW;
        private CoroutineHost coroutineHost;

        private Vector2 m_assetsScroll;
        private Vector2 m_resultsScroll;

        private string[] m_allStreamingAssets = new string[0];
        private List<TestInfo> m_results = new List<TestInfo>();
        private HashSet<string> m_selectedPaths = new HashSet<string>();


        void OnEnable()
        {
            InProgressText.enabled = false;
        }

        void OnDisable()
        {
            InProgressText.enabled = true;
        }

        void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(0, 0, Screen.width, Screen.height)))
            {
                if (string.IsNullOrEmpty(BetterStreamingAssets.Root))
                {
#if UNITY_EDITOR
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("APK path");
                        EditorApkPath = GUILayout.TextField(EditorApkPath);
                    }

                    if (GUILayout.Button("Use APK (Android like)"))
                    {
                        BetterStreamingAssets.InitializeWithExternalApk(EditorApkPath);
                        Initialize();
                    }

                    if (GUILayout.Button("Use Assets/StreamingAssets directory (iOS/Standalone like)"))
                    {
                        BetterStreamingAssets.Initialize();
                        Initialize();
                    }
                    return;
#else
                    BetterStreamingAssets.Initialize();
                    Initialize();
#endif
                }

                if (m_allStreamingAssets.Length == 0)
                {
                    GUILayout.Label("No streaming assets found in " + BetterStreamingAssets.Root);
                    return;
                }

                GUILayout.Label("Using " + BetterStreamingAssets.Root);

                GUILayout.Label("Discovered streaming assets:");
                using (var scope = new GUILayout.ScrollViewScope(m_assetsScroll, GUILayout.MaxHeight(300)))
                {
                    m_assetsScroll = scope.scrollPosition;
                    foreach (var path in m_allStreamingAssets)
                    {
                        var wasSelected = m_selectedPaths.Contains(path);
                        if (GUILayout.Toggle(wasSelected, path))
                        {
                            if (!wasSelected)
                                m_selectedPaths.Add(path);
                        }
                        else
                        {
                            if (wasSelected)
                                m_selectedPaths.Remove(path);
                        }
                    }
                }

                const float VerticalSpace = 10.0f;
                GUILayout.Space(VerticalSpace);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Repetition count: " + RepetitionCount, GUILayout.Width(150.0f));
                    RepetitionCount = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)RepetitionCount, 1.0f, 20.0f));
                }

                LogToFile = GUILayout.Toggle(LogToFile, "Log results to file");

                GUILayout.Space(VerticalSpace);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Test modes: ", GUILayout.Width(150.0f));
                    DoTestTypeToggle(TestType.CheckIfExists);
                    DoTestTypeToggle(TestType.LoadBytes);
                }

                GUILayout.Space(VerticalSpace);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Read modes: ", GUILayout.Width(150.0f));
                    DoReadModeToggle(ReadMode.BSA);
                    DoReadModeToggle(ReadMode.WWW);
                    DoReadModeToggle(ReadMode.UnityWebRequest);
#if !UNITY_ANDROID || UNITY_EDITOR
                    DoReadModeToggle(ReadMode.Direct);
#endif
                }

                GUI.enabled = m_selectedPaths.Count > 0;
                if (GUILayout.Button("Test Selected Paths (" + m_selectedPaths.Count + ")"))
                {
                    coroutineHost.StartCoroutine(TestAllCoroutine(m_selectedPaths.ToArray(), RepetitionCount, m_readModes, m_testModes, m_results));
                }
                GUI.enabled = true;

                GUILayout.Box(m_status);

                using (var scroll = new GUILayout.ScrollViewScope(m_resultsScroll))
                {
                    m_resultsScroll = scroll.scrollPosition;

                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUI.skin.label.clipping = TextClipping.Clip;

                    foreach (var result in m_results)
                    {
                        using (var layout = new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(result.path, GUILayout.Width(200));
                            GUILayout.Label(result.readMode.ToString(), GUILayout.Width(160));
                            GUILayout.Label(result.testType.ToString(), GUILayout.Width(160));

                            if (result.error != null)
                            {
                                GUILayout.Label(result.error.GetType().ToString());
                            }
                            else
                            {
                                GUILayout.Label(result.duration.ToString());
                                GUILayout.Label((result.memoryPeak / 1024.0 / 1024.0).ToString("F2") + " MB");
                            }
                        }
                    }

                    GUI.skin.label.clipping = TextClipping.Overflow;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        private string StreamingAssetsPath
        {
            get
            {
#if UNITY_EDITOR
                if (BetterStreamingAssets.Root == EditorApkPath)
                {
                    return "jar:" + new Uri(EditorApkPath).AbsoluteUri + "!/assets";
                }
#endif
                return Application.streamingAssetsPath;
            }
        }

        private void Initialize()
        {
            m_allStreamingAssets = BetterStreamingAssets.GetFiles("/", "*", SearchOption.AllDirectories);

            coroutineHost = gameObject.AddComponent<CoroutineHost>();

            // allocate something for mono heap to grow
            var bytes = new byte[200 * 1024 * 1024];
            Debug.LogFormat("Allocated {0}, mono heap size: {1}", bytes.Length, Profiler.GetMonoHeapSizeLong());
        }

        private void DoTestTypeToggle(TestType testMode)
        {
            bool wasSet = (m_testModes & testMode) == testMode;
            if (GUILayout.Toggle(wasSet, testMode.ToString()))
            {
                m_testModes |= testMode;
            }
            else if (wasSet)
            {
                if (m_testModes != testMode)
                    m_testModes &= ~testMode;
            }
        }

        private void DoReadModeToggle(ReadMode readMode)
        {
            bool wasSet = (m_readModes & readMode) == readMode;
            if (GUILayout.Toggle(wasSet, readMode.ToString()))
            {
                m_readModes |= readMode;
            }
            else if (wasSet)
            {
                if (m_readModes != readMode)
                    m_readModes &= ~readMode;
            }
        }

        private IEnumerator TestAllCoroutine(IEnumerable<string> paths, int attempts, ReadMode readModes, TestType testTypes, List<TestInfo> results)
        {
            LogWorkProgress("starting...");

            string logPath = Path.Combine(Application.persistentDataPath, "BSA_test_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_.csv");

            enabled = false;
            results.Clear();

            try
            {
                foreach (var path in paths)
                {
                    foreach (ReadMode readMode in Enum.GetValues(typeof(ReadMode)))
                    {
                        if ((readMode & readModes) != readMode)
                            continue;

                        foreach (TestType testType in Enum.GetValues(typeof(TestType)))
                        {
                            if ((testType & testTypes) != testType)
                                continue;

                            var testInfo = new TestInfo()
                            {
                                readMode = readMode,
                                testType = testType,
                                path = path,
                                attempts = attempts,
                            };

                            yield return coroutineHost.StartCoroutine(ErrorCatchingCoroutine(TestHarness(readMode, path, testType, attempts,
                                (duration, bytes, memory, maxMemory, names) =>
                                {
                                    testInfo.duration = duration;
                                    testInfo.bytesRead = bytes;
                                    testInfo.memoryPeak = memory;
                                    testInfo.maxMemoryPeak = maxMemory;
                                }),
                                ex =>
                                {
                                    testInfo.error = ex;
                                }
                            ));

                            results.Add(testInfo);
                        }
                    }
                }
            }
            finally
            {
                enabled = true;

                if (LogToFile)
                {
                    using (var writer = File.CreateText(logPath))
                    {
                        foreach (var result in results)
                        {
                            string errorMessage = string.Empty;
                            if (result.error != null)
                                errorMessage = result.error.ToString().Replace(Environment.NewLine, ";");

                            writer.WriteLine("\"{0}\"\t{1}\t{2}\t{3}\t{4}\t\"{5}\"", result.path, result.readMode, result.testType, result.duration, result.memoryPeak, errorMessage);
                        }
                    }

                    LogWorkProgress("Logged at: " + logPath);
                }
            }

        }

        private void LogWorkProgress(string status)
        {
            Debug.Log("WORK PROGRESS: " + status);
            if (string.IsNullOrEmpty(m_status))
                m_status = status;
            else
                m_status += "\n" + status;
        }

        private IEnumerator ErrorCatchingCoroutine(IEnumerator inner, Action<System.Exception> onError)
        {
            m_status = string.Empty;

            for (; ; )
            {
                bool next = false;
                try
                {
                    next = inner.MoveNext();
                }
                catch (System.Exception ex)
                {
                    onError(ex);
                    break;
                }

                if (!next)
                    break;

                yield return inner.Current;
            }
        }

        private IEnumerator TestHarness(ReadMode readMode, string path, TestType testType, int attempts, TestResultDelegate callback)
        {
            var stopwatch = new Stopwatch();

            string[] assetNames = null;

            var streamingAssetsUrl = Path.Combine(StreamingAssetsPath, path.TrimStart('/')).Replace('\\', '/');

            long bytesRead = 0;
            long maxMemoryPeak = 0;
            long totalMemoryPeaks = 0;

            for (int i = 0; i < attempts; ++i)
            {
                IDisposable toDispose = null;

                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                yield return null;

                var memoryUnityBefore = Profiler.GetTotalAllocatedMemoryLong();
                //var memoryMonoBefore = Profiler.GetMonoUsedSizeLong();
                stopwatch.Start();

                if (readMode == ReadMode.WWW)
                {
#pragma warning disable 0618 // Type or member is obsolete
                    var www = new WWW(streamingAssetsUrl);
#pragma warning restore 0618 // Type or member is obsolete
                    toDispose = www;
                    {
                        yield return www;

                        Profiler.BeginSample(testType.ToString());

                        switch (testType)
                        {
                            case TestType.CheckIfExists:
                                if (!string.IsNullOrEmpty(www.error))
                                    throw new System.Exception(www.error);
                                break;
                            case TestType.LoadBytes:
                                bytesRead += www.bytes.Length;
                                break;

                            default:
                                throw new NotSupportedException();
                        }
                        Profiler.EndSample();
                    }
                }
                else if (readMode == ReadMode.BSA)
                {
                    Profiler.BeginSample(testType.ToString());

                    switch (testType)
                    {
                        case TestType.CheckIfExists:
                            if (!BetterStreamingAssets.FileExists(path))
                                throw new System.InvalidOperationException();
                            break;
                        case TestType.LoadBytes:
                            bytesRead += BetterStreamingAssets.ReadAllBytes(path).Length;
                            break;
                    }

                    Profiler.EndSample();
                }
                else if (readMode == ReadMode.Direct)
                {
                    var p = streamingAssetsUrl;
                    Profiler.BeginSample(testType.ToString());

                    switch (testType)
                    {
                        case TestType.CheckIfExists:
                            if (!File.Exists(p))
                                throw new System.InvalidOperationException();
                            break;
                        case TestType.LoadBytes:
                            bytesRead += File.ReadAllBytes(p).Length;
                            break;
                    }

                    Profiler.EndSample();
                }
                else if (readMode == ReadMode.UnityWebRequest)
                {
                    var www = UnityEngine.Networking.UnityWebRequest.Get(streamingAssetsUrl);
                    toDispose = www;
#pragma warning disable 0618 // Type or member is obsolete
                    yield return www.Send();
#pragma warning restore 0618 // Type or member is obsolete

                    Profiler.BeginSample(testType.ToString());

                    switch (testType)
                    {
                        case TestType.CheckIfExists:
                            if (!string.IsNullOrEmpty(www.error))
                                throw new System.Exception(www.error);
                            break;
                        case TestType.LoadBytes:
                            bytesRead += (int)www.downloadedBytes;
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                    Profiler.EndSample();
                }

                stopwatch.Stop();

                var memoryPeak = Math.Max(0, Profiler.GetTotalAllocatedMemoryLong() - memoryUnityBefore);
                // + Math.Max(0, Profiler.GetMonoUsedSizeLong() - memoryMonoBefore);

                maxMemoryPeak = System.Math.Max(memoryPeak, maxMemoryPeak);
                totalMemoryPeaks += memoryPeak;

                yield return null;

                if (toDispose != null)
                    toDispose.Dispose();

                yield return null;
            }

            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            yield return null;

            callback(new TimeSpan(stopwatch.ElapsedTicks / attempts), bytesRead / attempts, totalMemoryPeaks / attempts, maxMemoryPeak, assetNames);
        }
    }
}