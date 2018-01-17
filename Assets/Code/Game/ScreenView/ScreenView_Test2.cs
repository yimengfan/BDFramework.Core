using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ScreenView;

[ScreenView("test2")]
public class ScreenView_Test2 : IScreenView
 {

	 public string Name { get; private set; }
	 public bool IsLoad { get; private set; }
	 public bool IsBusy { get; private set; }
	 public bool IsTransparent { get; private set; }
	 
	 public void BeginInit(Action<Exception> onInit, ScreenViewLayer layer)
	 {
		 Debug.Log("进入Test Screen 2");
	 }

	 public void BeginExit(Action<Exception> onExit)
	 {
		
	 }

	 public void Destory()
	 {
		 
	 }

	 public void Update(float delta)
	 {
		 Debug.Log("sv2 update");
	 }

	 public void UpdateTask(float delta)
	 {
		 
	 }

	 public void FixedUpdate(float delta)
	 {
		
	 }
 }
