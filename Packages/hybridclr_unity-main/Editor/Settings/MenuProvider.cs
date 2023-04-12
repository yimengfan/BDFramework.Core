using UnityEditor;
using UnityEngine;

public static class MenuProvider
{
    [MenuItem("HybridCLR/Settings", priority = 200)]
    public static void OpenSettings() => SettingsService.OpenProjectSettings("Project/HybridCLR Settings");
    [MenuItem("HybridCLR/Documents/", menuItem = "HybridCLR/Documents/Quick Start")]
    public static void OpenQuickStart() => Application.OpenURL("https://focus-creative-games.github.io/hybridclr/start_up/");
    [MenuItem("HybridCLR/Documents/", menuItem = "HybridCLR/Documents/Benchmark")]
    public static void OpenBenchmark() => Application.OpenURL("https://focus-creative-games.github.io/hybridclr/benchmark/");
    [MenuItem("HybridCLR/Documents/", menuItem = "HybridCLR/Documents/FAQ")]
    public static void OpenFAQ() => Application.OpenURL("https://focus-creative-games.github.io/hybridclr/faq/");
    [MenuItem("HybridCLR/Documents/", menuItem = "HybridCLR/Documents/Common Errors")]
    public static void OpenCommonErrors() => Application.OpenURL("https://focus-creative-games.github.io/hybridclr/common_errors/");
    [MenuItem("HybridCLR/Documents/", menuItem = "HybridCLR/Documents/Bug Report")]
    public static void OpenBugReport() => Application.OpenURL("https://focus-creative-games.github.io/hybridclr/bug_reporter/");
    [MenuItem("HybridCLR/Documents/", menuItem = "HybridCLR/Documents/About HybridCLR")]
    public static void OpenAbout() => Application.OpenURL("https://focus-creative-games.github.io/hybridclr/about/");
}

