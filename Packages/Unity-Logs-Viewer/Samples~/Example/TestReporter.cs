#if UNITY_CHANGE1 || UNITY_CHANGE2 || UNITY_CHANGE3 || UNITY_CHANGE4
#warning UNITY_CHANGE has been set manually
#elif UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
#define UNITY_CHANGE1
#elif UNITY_5_0 || UNITY_5_1 || UNITY_5_2
#define UNITY_CHANGE2
#else
#define UNITY_CHANGE3
#endif
#if UNITY_2018_3
#define UNITY_CHANGE4
#endif
//use UNITY_CHANGE1 for unity older than "unity 5"
//use UNITY_CHANGE2 for unity 5.0 -> 5.3 
//use UNITY_CHANGE3 for unity 5.3 (fix for new SceneManger system)
//use UNITY_CHANGE4 for unity 2018.3 (Networking system)

using UnityEngine;
using System.Collections;
using System.Threading;
#if UNITY_CHANGE3
using UnityEngine.SceneManagement;
#endif
#if UNITY_CHANGE4
using UnityEngine.Networking;
#endif


//this script used for test purpose ,it do by default 100 logs  + 100 warnings + 100 errors
//so you can check the functionality of in game logs
//just drop this scrip to any empty game object on first scene your game start at
public class TestReporter : MonoBehaviour
{
	public int logTestCount = 100;
	public int threadLogTestCount = 100;
	public bool logEverySecond = true;
	int currentLogTestCount;
	Reporter reporter;
	GUIStyle style;
	Rect rect1;
	Rect rect2;
	Rect rect3;
	Rect rect4;
	Rect rect5;
	Rect rect6;

	Thread thread;

	void Start()
	{
		Application.runInBackground = true;

		reporter = FindObjectOfType(typeof(Reporter)) as Reporter;
		Debug.Log("test long text sdf asdfg asdfg sdfgsdfg sdfg sfg" +
				  "sdfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdfg " +
				  "sdfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdfg " +
				  "sdfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdfg " +
				  "sdfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdfg " +
				  "sdfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdfg ssssssssssssssssssssss" +
				  "asdf asdf asdf asdf adsf \n dfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdf" +
				  "asdf asdf asdf asdf adsf \n dfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdf" +
				  "asdf asdf asdf asdf adsf \n dfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdf" +
				  "asdf asdf asdf asdf adsf \n dfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdf" +
				  "asdf asdf asdf asdf adsf \n dfgsdfg sdfg sdf gsdfg sfdg sf gsdfg sdfg asdf");

		style = new GUIStyle();
		style.alignment = TextAnchor.MiddleCenter;
		style.normal.textColor = Color.white;
		style.wordWrap = true;

		for (int i = 0; i < 10; i++) {
			Debug.Log("Test Collapsed log");
			Debug.LogWarning("Test Collapsed Warning");
			Debug.LogError("Test Collapsed Error");
		}

		for (int i = 0; i < 10; i++) {
			Debug.Log("Test Collapsed log");
			Debug.LogWarning("Test Collapsed Warning");
			Debug.LogError("Test Collapsed Error");
		}

		rect1 = new Rect(Screen.width / 2 - 120, Screen.height / 2 - 225, 240, 50);
		rect2 = new Rect(Screen.width / 2 - 120, Screen.height / 2 - 175, 240, 100);
		rect3 = new Rect(Screen.width / 2 - 120, Screen.height / 2 - 50, 240, 50);
		rect4 = new Rect(Screen.width / 2 - 120, Screen.height / 2, 240, 50);
		rect5 = new Rect(Screen.width / 2 - 120, Screen.height / 2 + 50, 240, 50);
		rect6 = new Rect(Screen.width / 2 - 120, Screen.height / 2 + 100, 240, 50);

		thread = new Thread(new ThreadStart(threadLogTest));
		thread.Start();
	}

	void OnDestroy()
	{
		thread.Abort();
	}

	void threadLogTest()
	{
		for (int i = 0; i < threadLogTestCount; i++) {
			Debug.Log("Test Log from Thread");
			Debug.LogWarning("Test Warning from Thread");
			Debug.LogError("Test Error from Thread");
		}
	}

	float elapsed;
	void Update()
	{
		int drawn = 0;
		while (currentLogTestCount < logTestCount && drawn < 10) {
			Debug.Log("Test Log " + currentLogTestCount);
			Debug.LogError("Test LogError " + currentLogTestCount);
			Debug.LogWarning("Test LogWarning " + currentLogTestCount);
			drawn++;
			currentLogTestCount++;
		}

		elapsed += Time.deltaTime;
		if (elapsed >= 1) {
			elapsed = 0;
			Debug.Log("One Second Passed");
		}
	}

	void OnGUI()
	{
		if (reporter && !reporter.show) {
			GUI.Label(rect1, "Draw circle on screen to show logs", style);
			GUI.Label(rect2, "To use Reporter just create reporter from reporter menu at first scene your game start", style);
			if (GUI.Button(rect3, "Load ReporterScene")) {
#if UNITY_CHANGE3
				SceneManager.LoadScene("ReporterScene");
#else
				Application.LoadLevel("ReporterScene");
#endif
			}
			if (GUI.Button(rect4, "Load test1")) {
#if UNITY_CHANGE3
				SceneManager.LoadScene("test1");
#else
				Application.LoadLevel("test1");
#endif
			}
			if (GUI.Button(rect5, "Load test2")) {
#if UNITY_CHANGE3
				SceneManager.LoadScene("test2");
#else
				Application.LoadLevel("test2");
#endif
			}
			GUI.Label(rect6, "fps : " + reporter.fps.ToString("0.0"), style);
		}
	}

}
