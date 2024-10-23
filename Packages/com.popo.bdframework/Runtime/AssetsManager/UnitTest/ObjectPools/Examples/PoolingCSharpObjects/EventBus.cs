using System;
using System.Collections.Generic;
using BDFramework;
using UnityEngine;
using BDFramework.ResourceMgr;

public class EventBus : MonoBehaviour
{
	public float delay = 1;

	private ObjectPool<DelayedEvent> eventPool;
	private List<DelayedEvent> activeEvents;
	
	void Awake () 
	{
		eventPool = new ObjectPool<DelayedEvent>(()=> new DelayedEvent(), 5);
		activeEvents = new List<DelayedEvent>();
	}

	void Update()
	{
		if(Input.GetMouseButtonDown(0))
		{
			SpawnEvent();
		}

		for (int i = 0; i < activeEvents.Count; i++)
		{
			activeEvents[i].Update(Time.time);
		}
	}

	private void SpawnEvent()
	{
		var evt = eventPool.GetItem();
		evt.Start(Time.time, delay);
		evt.Triggered += OnEvtComplete;
		activeEvents.Add(evt);

	}

	private void OnEvtComplete(DelayedEvent evt)
	{
		evt.Triggered -= OnEvtComplete;
		activeEvents.Remove(evt);
		eventPool.ReleaseItem(evt);
		Debug.Log("Delayed event started at " + evt.startTime + " completed at "+ evt.endTime);
	}

	private void OnGUI()
	{
		GUILayout.BeginArea(new Rect(20,20,400,200));

		GUILayout.Label("Click Mouse to create Delayed Event");
		GUILayout.Label("Total used objects: " + eventPool.CountUsedItems);
		GUILayout.Label("Total objects in pool: " + eventPool.Count);

		GUILayout.EndArea();
	}
}

public class DelayedEvent
{
	public event Action<DelayedEvent> Triggered;

	public float startTime;
	public float endTime;
	private float delay;

	public void Start(float time, float delay)
	{
		this.delay = delay;
		this.startTime = time;
	}

	public void Update(float time)
	{
		if(time - startTime > delay)
		{
			endTime = time;
			Trigger();
		}
	}

	public void Trigger()
	{
		if(Triggered != null)
		{
			Triggered(this);
		}
	}
}
