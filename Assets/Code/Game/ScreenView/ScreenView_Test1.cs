using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ScreenView;

[ScreenView("test", true)]
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
		 
		 Debug.Log("进入Test Screen 1");
		 //
		 ScreenViewMgr.I.BeginNav("test2");
	 }

	 public void BeginExit(Action<Exception> onExit)
	 {
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
