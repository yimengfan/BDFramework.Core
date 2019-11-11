using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ScreenView;
using BDFramework.UI;
using Code.Game;

[ScreenView((int)ScreenViewEnum.Demo3)]
public class ScreenView_Demo3_WindowsMvc : IScreenView
 {

	 public int Name { get; private set; }
	 public bool IsLoad { get; private set; }
	 
	 public void BeginInit()
	 {
		 //一定要设置为true，否则当前是未加载状态
		 this.IsLoad = true;
		 //打开 window 3
		 UIManager.Inst.LoadWindows((int)WinEnum.Win_Demo3);
		 UIManager.Inst.ShowWindow((int)WinEnum.Win_Demo3);
		 //
		 Debug.Log("进入demo3");
	 }

	 public void BeginExit()
	 {
	     //退出设置为false，否则下次进入不会调用begininit
		 this.IsLoad = false;
		 Destory();

	 }

	 public void Destory()
	 {
		 
	 }

	 public void Update(float delta)
	 {
	 }

	 public void FixedUpdate(float delta)
	 {
		
	 }
 }
