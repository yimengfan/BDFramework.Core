using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ScreenView;
using BDFramework.UI;

[ScreenView("demo2")]
public class ScreenView_Demo2_Windows : IScreenView
 {

	 public string Name { get; private set; }
	 public bool IsLoad { get; private set; }
	 
	 public void BeginInit()
	 {
		 //一定要设置为true，否则当前是未加载状态
		 this.IsLoad = true;
		 //打开 window 2
		 UIManager.Inst.LoadWindows((int)WinEnum.Win_Demo2);
		 UIManager.Inst.ShowWindow((int)WinEnum.Win_Demo2);
		 //
		 Debug.Log("进入demo2");
	 }

	 public void BeginExit()
	 {
	     //退出设置为false，否则下次进入不会调用begininit
		 this.IsLoad = false;
		 Destory();
		 
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


	 public void FixedUpdate(float delta)
	 {
		
	 }
 }
