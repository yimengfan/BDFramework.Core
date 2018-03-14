using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ScreenView;
using BDFramework.UI;

[ScreenView("sv_test1", true)]
public class ScreenView_Test : IScreenView
 {

	 public string Name { get; private set; }
	 public bool IsLoad { get; private set; }
	 public bool IsBusy { get; private set; }
	 public bool IsTransparent { get; private set; }
	 
	 public void BeginInit(Action<Exception> onInit, ScreenViewLayer layer)
	 {
		 //一定要设置为true，否则当前是未加载状态
		 this.IsLoad = true;
		 
		 //加载窗口, 0是窗口id,建议自行换成枚举
		 UIMgr.I.LoadWindows(WinEnum.Win_Test1, WinEnum.Win_Test2 , WinEnum.Win_XVC );
		 UIMgr.I.ShowWindow(WinEnum.Win_Test1);
		 
		 Debug.Log("进入Test Screen 1");
	 }

	 public void BeginExit(Action<Exception> onExit)
	 {
		 //退出设置为false，否则下次进入不会调用begininit
		 this.IsLoad = false;
		 Destory();
		 onExit(null);
		 
		 
		 //1..退出时候 向win test2 发消息
		 var d = WinData.Create();
		 d.SetData("rotation", UnityEngine.Random.Range(-359, 359));
		 UIMgr.I.PushData(WinEnum.Win_Test2, d);
		 
		 //
		 Debug.Log("退出Test Screen 1");
	 }

	 public void Destory()
	 {
		 
	 }

	 public void Update(float delta)
	 {
		Debug.Log("sv1 update");
	 }

	 public void UpdateTask(float delta)
	 {
		 
	 }

	 public void FixedUpdate(float delta)
	 {
		
	 }
 }
