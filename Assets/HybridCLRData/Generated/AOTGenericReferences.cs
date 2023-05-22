public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	// BDFramework.Core.dll
	// Google.Protobuf.dll
	// System.Core.dll
	// System.dll
	// UnityEngine.CoreModule.dll
	// ZString.dll
	// mscorlib.dll
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// BDFramework.Mgr.ManagerBase<object,object>
	// Google.Protobuf.Collections.RepeatedField<object>
	// Google.Protobuf.MessageParser<object>
	// System.Action<byte>
	// System.Action<object>
	// System.Action<int,object>
	// System.Action<int,int>
	// System.Action<object,object>
	// System.Action<BDFramework.VersionController.AssetsVersionController.RetStatus,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List<double>
	// System.Collections.Generic.List<byte>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.List<float>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Comparison<object>
	// System.Func<object,byte>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Threading.Tasks.Task<object>
	// UnityEngine.Events.UnityAction<byte>
	// UnityEngine.Events.UnityEvent<byte>
	// }}

	public void RefMethods()
	{
		// System.Void BDFramework.DataListener.AStatusListener.AddListener<object>(string,System.Action<object>,int,int,bool)
		// System.Void BDFramework.DataListener.EventListenerEx.AddListener<object>(BDFramework.DataListener.AStatusListener,System.Action<object>,int,int,bool)
		// System.Void BDFramework.DataListener.EventListenerEx.AddListenerOnce<object>(BDFramework.DataListener.AStatusListener,System.Action<object>,int,bool)
		// System.Void BDFramework.DataListener.EventListenerEx.ClearListener<object>(BDFramework.DataListener.AStatusListener)
		// System.Void BDFramework.DataListener.EventListenerEx.RemoveListener<object>(BDFramework.DataListener.AStatusListener,System.Action<object>)
		// System.Void BDFramework.DataListener.EventListenerEx.TriggerEvent<object>(BDFramework.DataListener.AStatusListener,object)
		// BDFramework.DataListener.ADataListenerT<object> BDFramework.DataListener.StatusListenerServer.Create<object>(string)
		// BDFramework.DataListener.ADataListenerT<object> BDFramework.DataListener.StatusListenerServer.GetService<object>(string)
		// System.Void BDFramework.DataListener.ValueListenerEx.AddListener<object>(BDFramework.DataListener.AStatusListener,System.Enum,System.Action<object>,int,int,bool)
		// System.Void BDFramework.DataListener.ValueListenerEx.AddListener<object>(BDFramework.DataListener.AStatusListener,string,System.Action<object>,int,int,bool)
		// int BDFramework.DataListener.ValueListenerEx.GetData<int>(BDFramework.DataListener.AStatusListener,System.Enum)
		// object BDFramework.Hotfix.Reflection.ReflectionExtension.GetAttributeInILRuntime<object>(System.Reflection.MemberInfo)
		// object[] BDFramework.Hotfix.Reflection.ReflectionExtension.GetAttributeInILRuntimes<object>(System.Reflection.MemberInfo)
		// object BDFramework.Mgr.ManagerBase<object,object>.CreateInstance<object>(object,object[])
		// object BDFramework.Mgr.ManagerBase<object,object>.CreateInstance<object>(BDFramework.Mgr.ClassData,object[])
		// int BDFramework.ResourceMgr.BResources.AsyncLoad<object>(string,System.Action<object>,BDFramework.ResourceMgr.LoadPathType,string)
		// object BDFramework.ResourceMgr.BResources.Load<object>(string,BDFramework.ResourceMgr.LoadPathType,string)
		// System.Void BDFramework.Sql.SqliteHelper.SQLiteService.CreateTable<object>()
		// string Cysharp.Text.ZString.Concat<object,object,object>(object,object,object)
		// object LitJson.JsonMapper.ToObject<object>(string,System.Type)
		// object SQLite4Unity3d.TableQueryForILRuntime.From<object>(string)
		// System.Collections.Generic.List<object> SQLite4Unity3d.TableQueryForILRuntime.FromAll<object>(string)
		// object System.Activator.CreateInstance<object>()
		// object[] System.Array.Empty<object>()
		// object System.Linq.Enumerable.FirstOrDefault<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// object[] System.Linq.Enumerable.ToArray<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.List<object> System.Linq.Enumerable.ToList<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Test.Reducer_Demo06Test.<RequestServerByAsync>d__4>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Test.Reducer_Demo06Test.<RequestServerByAsync>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__4>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Test.Reducer_Demo06Test.<RequestServerByAsync>d__4>(BDFramework.UFlux.Test.Reducer_Demo06Test.<RequestServerByAsync>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__4>(BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__4&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__12<object>>(BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__12<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8>(BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<HotfixCheck.<TestAction>d__1>(HotfixCheck.<TestAction>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<BDFramework.UFlux.Contains.Store.<Dispatch>d__17<object>>(BDFramework.UFlux.Contains.Store.<Dispatch>d__17<object>&)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.Object.Instantiate<object>(object)
	}
}