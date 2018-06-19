using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ScreenView;
using BDFramework.UI;

[ScreenView("demo2")]
public class ScreenView_Demo3_WindowsMvc : IScreenView
 {

	 public string Name { get; private set; }
	 public bool IsLoad { get; private set; }
	 public bool IsBusy { get; private set; }
	 public bool IsTransparent { get; private set; }
	 
	 public void BeginInit(Action<Exception> onInit, ScreenViewLayer layer)
	 {
		 //一定要设置为true，否则当前是未加载状态
		 this.IsLoad = true;
		 //打开 window 3
		 UIMgr.Inst.LoadWindows(WinEnum.Win_XVC);
		 UIMgr.Inst.ShowWindow(WinEnum.Win_XVC);
		 //
		 Debug.Log("进入demo3");
	 }

	 public void BeginExit(Action<Exception> onExit)
	 {
	     //退出设置为false，否则下次进入不会调用begininit
		 this.IsLoad = false;
		 Destory();
		 onExit(null);
		 
		 Destory();
		// Debug.Log("退出 Test Screen 2");
	 }

	 public void Destory()
	 {
		 
	 }

	 public void Update(float delta)
	 {
		// Debug.Log("sv2 update");
	 }

	 public void UpdateTask(float delta)
	 {
		 
	 }

	 public void FixedUpdate(float delta)
	 {
		
	 }
 }
