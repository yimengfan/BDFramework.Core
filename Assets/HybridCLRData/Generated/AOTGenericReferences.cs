using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"BDFramework.Core.dll",
		"Google.Protobuf.dll",
		"System.Buffers.dll",
		"System.Core.dll",
		"System.Memory.dll",
		"System.Runtime.CompilerServices.Unsafe.dll",
		"System.dll",
		"UnityEngine.CoreModule.dll",
		"ZString.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// BDFramework.Adaptor.ActionAdaptor<object>
	// BDFramework.DataListener.ADataListenerT<object>
	// BDFramework.DataListener.AStatusListener.<>c__DisplayClass12_0<object>
	// BDFramework.Mgr.ManagerBase<object,object>
	// Cysharp.Text.Utf16ValueStringBuilder.TryFormat<object>
	// Google.Protobuf.Collections.RepeatedField.<GetEnumerator>d__20<object>
	// Google.Protobuf.Collections.RepeatedField<object>
	// Google.Protobuf.FieldCodec.<>c__16<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass12_0<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass16_0<object>
	// Google.Protobuf.FieldCodec<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass1_0<object>
	// Google.Protobuf.MessageParser<object>
	// System.Action<byte>
	// System.Action<double>
	// System.Action<float>
	// System.Action<int,int>
	// System.Action<int,object>
	// System.Action<int>
	// System.Action<object,object>
	// System.Action<object>
	// System.ArraySegment.ArraySegmentEnumerator<ushort>
	// System.ArraySegment<ushort>
	// System.Buffers.ArrayPool<ushort>
	// System.Buffers.DefaultArrayPool.Bucket<ushort>
	// System.Buffers.DefaultArrayPool<ushort>
	// System.Collections.Generic.ArraySortHelper<byte>
	// System.Collections.Generic.ArraySortHelper<double>
	// System.Collections.Generic.ArraySortHelper<float>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<double>
	// System.Collections.Generic.Comparer<float>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<double>
	// System.Collections.Generic.EqualityComparer<float>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<byte>
	// System.Collections.Generic.ICollection<double>
	// System.Collections.Generic.ICollection<float>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<byte>
	// System.Collections.Generic.IComparer<double>
	// System.Collections.Generic.IComparer<float>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<object,LitJson.ArrayMetadata>
	// System.Collections.Generic.IDictionary<object,LitJson.ObjectMetadata>
	// System.Collections.Generic.IDictionary<object,LitJson.PropertyMetadata>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<byte>
	// System.Collections.Generic.IEnumerable<double>
	// System.Collections.Generic.IEnumerable<float>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<byte>
	// System.Collections.Generic.IEnumerator<double>
	// System.Collections.Generic.IEnumerator<float>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<byte>
	// System.Collections.Generic.IList<double>
	// System.Collections.Generic.IList<float>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<byte>
	// System.Collections.Generic.List.Enumerator<double>
	// System.Collections.Generic.List.Enumerator<float>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List.SynchronizedList<byte>
	// System.Collections.Generic.List.SynchronizedList<double>
	// System.Collections.Generic.List.SynchronizedList<float>
	// System.Collections.Generic.List.SynchronizedList<int>
	// System.Collections.Generic.List.SynchronizedList<object>
	// System.Collections.Generic.List<byte>
	// System.Collections.Generic.List<double>
	// System.Collections.Generic.List<float>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<double>
	// System.Collections.Generic.ObjectComparer<float>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<double>
	// System.Collections.Generic.ObjectEqualityComparer<float>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<byte>
	// System.Collections.ObjectModel.ReadOnlyCollection<double>
	// System.Collections.ObjectModel.ReadOnlyCollection<float>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<byte>
	// System.Comparison<double>
	// System.Comparison<float>
	// System.Comparison<int>
	// System.Comparison<object>
	// System.Func<object,byte>
	// System.Func<object,int>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.Linq.Buffer<object>
	// System.Linq.Enumerable.Iterator<object>
	// System.Linq.Enumerable.WhereArrayIterator<object>
	// System.Linq.Enumerable.WhereEnumerableIterator<object>
	// System.Linq.Enumerable.WhereListIterator<object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<object,object>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<object,object>
	// System.Linq.Enumerable.WhereSelectListIterator<object,object>
	// System.Predicate<byte>
	// System.Predicate<double>
	// System.Predicate<float>
	// System.Predicate<int>
	// System.Predicate<object>
	// System.ReadOnlySpan<ushort>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Span<ushort>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<object>
	// System.Threading.Tasks.Task.<>c<object>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_1<object>
	// System.Threading.Tasks.TaskFactory<object>
	// UnityEngine.Events.InvokableCall<byte>
	// UnityEngine.Events.UnityAction<byte>
	// UnityEngine.Events.UnityEvent<byte>
	// }}

	public void RefMethods()
	{
		// int BDFramework.DataListener.AStatusListener.GetData<int>(string)
		// System.Void BDFramework.DataListener.AStatusListener.RemoveListener<object>(string,System.Action<object>)
		// BDFramework.DataListener.ADataListenerT<object> BDFramework.DataListener.StatusListenerServer.Create<object>(string)
		// BDFramework.DataListener.ADataListenerT<object> BDFramework.DataListener.StatusListenerServer.GetService<object>(string)
		// int BDFramework.DataListener.ValueListenerEx.GetData<int>(BDFramework.DataListener.AStatusListener,System.Enum)
		// object BDFramework.Hotfix.Reflection.ReflectionExtension.GetAttributeInILRuntime<object>(System.Reflection.MemberInfo)
		// object[] BDFramework.Hotfix.Reflection.ReflectionExtension.GetAttributeInILRuntimes<object>(System.Reflection.MemberInfo)
		// object BDFramework.Mgr.ManagerBase<object,object>.CreateInstance<object>(BDFramework.Mgr.ClassData,object[])
		// object BDFramework.Mgr.ManagerBase<object,object>.CreateInstance<object>(object,object[])
		// int BDFramework.ResourceMgr.BResources.AsyncLoad<object>(string,System.Action<object>,BDFramework.ResourceMgr.LoadPathType,string)
		// object BDFramework.ResourceMgr.BResources.Load<object>(string,BDFramework.ResourceMgr.LoadPathType,string)
		// int BDFramework.ResourceMgr.IResMgr.AsyncLoad<object>(string,System.Action<object>,BDFramework.ResourceMgr.LoadPathType)
		// object BDFramework.ResourceMgr.IResMgr.Load<object>(string,BDFramework.ResourceMgr.LoadPathType)
		// System.Void BDFramework.Sql.SqliteHelper.SQLiteService.CreateTable<object>()
		// System.Void Cysharp.Text.Utf16ValueStringBuilder.Append<object>(object)
		// string Cysharp.Text.ZString.Concat<object,object,object>(object,object,object)
		// Google.Protobuf.FieldCodec<object> Google.Protobuf.FieldCodec.ForMessage<object>(uint,Google.Protobuf.MessageParser<object>)
		// object LitJson.JsonMapper.ToObject<object>(string,System.Type)
		// object SQLite4Unity3d.TableQueryForILRuntime.From<object>(string)
		// System.Collections.Generic.List<object> SQLite4Unity3d.TableQueryForILRuntime.FromAll<object>(string)
		// object System.Activator.CreateInstance<object>()
		// object[] System.Array.Empty<object>()
		// bool System.Linq.Enumerable.Contains<object>(System.Collections.Generic.IEnumerable<object>,object)
		// bool System.Linq.Enumerable.Contains<object>(System.Collections.Generic.IEnumerable<object>,object,System.Collections.Generic.IEqualityComparer<object>)
		// int System.Linq.Enumerable.Count<object>(System.Collections.Generic.IEnumerable<object>)
		// object System.Linq.Enumerable.FirstOrDefault<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Select<object,object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,object>)
		// object[] System.Linq.Enumerable.ToArray<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.List<object> System.Linq.Enumerable.ToList<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Iterator<object>.Select<object>(System.Func<object,object>)
		// System.Span<ushort> System.MemoryExtensions.AsSpan<ushort>(ushort[],int)
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// System.Collections.Generic.IEnumerable<object> System.Reflection.CustomAttributeExtensions.GetCustomAttributes<object>(System.Reflection.MemberInfo)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__28<object>>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__28<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__5>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Test.Reducer_Demo06Copy.<RequestServerByAsync>d__3>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Test.Reducer_Demo06Copy.<RequestServerByAsync>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__28<object>>(BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__28<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__5>(BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Test.Reducer_Demo06Copy.<RequestServerByAsync>d__3>(BDFramework.UFlux.Test.Reducer_Demo06Copy.<RequestServerByAsync>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Contains.Store.<Dispatch>d__24<object>>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Contains.Store.<Dispatch>d__24<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<BDFramework.UFlux.Contains.Store.<Dispatch>d__24<object>>(BDFramework.UFlux.Contains.Store.<Dispatch>d__24<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8>(BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<HotfixCheck.<TestAction>d__1>(HotfixCheck.<TestAction>d__1&)
		// int& System.Runtime.CompilerServices.Unsafe.As<object,int>(object&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.Object.Instantiate<object>(object)
	}
}