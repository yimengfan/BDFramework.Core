using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"BDFramework.AOT.dll",
		"DOTween.dll",
		"Google.Protobuf.dll",
		"ServiceStack.Text.dll",
		"System.Core.dll",
		"System.Runtime.CompilerServices.Unsafe.dll",
		"UniTask.dll",
		"UnityEngine.AssetBundleModule.dll",
		"UnityEngine.CoreModule.dll",
		"ZString.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Text.Utf16ValueStringBuilder.TryFormat<int>
	// Cysharp.Text.Utf16ValueStringBuilder.TryFormat<object>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<object>
	// Google.Protobuf.Collections.RepeatedField.<GetEnumerator>d__20<object>
	// Google.Protobuf.Collections.RepeatedField<object>
	// Google.Protobuf.FieldCodec.<>c__16<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass12_0<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass16_0<object>
	// Google.Protobuf.FieldCodec<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass1_0<object>
	// Google.Protobuf.MessageParser<object>
	// ServiceStack.GetMemberDelegate<object>
	// ServiceStack.SetMemberDelegate<object>
	// ServiceStack.Text.Common.DeserializeDictionary.<>c__DisplayClass17_0<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeDictionary.<>c__DisplayClass3_0<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeDictionary.<>c__DisplayClass4_0<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeDictionary.ParseDictionaryDelegate<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeDictionary<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeList.<>c<object,ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeList.<>c__DisplayClass7_0<object,ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeList<object,ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeListWithElements.<>c__DisplayClass3_0<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeListWithElements.<>c__DisplayClass3_1<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeListWithElements.ParseListDelegate<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeListWithElements<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeType.<>c<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeType.<>c__DisplayClass1_0<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeType.<>c__DisplayClass2_0<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeType.StringToTypeContext<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.DeserializeType<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.JsReader<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.JsReader<ServiceStack.Text.Jsv.JsvTypeSerializer>
	// ServiceStack.Text.Common.JsWriter<ServiceStack.Text.Json.JsonTypeSerializer>
	// ServiceStack.Text.Common.JsWriter<ServiceStack.Text.Jsv.JsvTypeSerializer>
	// ServiceStack.Text.CsvConfig<object>
	// ServiceStack.Text.CsvReader.<>c<object>
	// ServiceStack.Text.CsvReader<object>
	// ServiceStack.Text.CsvSerializer.<>c<object>
	// ServiceStack.Text.CsvSerializer<object>
	// ServiceStack.Text.CsvWriter.<>c<object>
	// ServiceStack.Text.CsvWriter<object>
	// ServiceStack.Text.JsConfig<object>
	// ServiceStack.Text.Json.JsonReader.<>c<object>
	// ServiceStack.Text.Json.JsonReader<object>
	// ServiceStack.Text.Json.JsonWriter<object>
	// ServiceStack.Text.Jsv.JsvReader.<>c<object>
	// ServiceStack.Text.Jsv.JsvReader<object>
	// ServiceStack.Text.Jsv.JsvWriter<object>
	// ServiceStack.Text.TypeConfig.<>c<object>
	// ServiceStack.Text.TypeConfig.<>c__DisplayClass20_0<object>
	// ServiceStack.Text.TypeConfig<object>
	// System.Action<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Action<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Action<byte>
	// System.Action<double>
	// System.Action<float>
	// System.Action<int,int>
	// System.Action<int,object>
	// System.Action<int>
	// System.Action<object,System.DateTime>
	// System.Action<object,System.DateTimeOffset>
	// System.Action<object,System.Decimal>
	// System.Action<object,System.Guid>
	// System.Action<object,System.IntPtr,int>
	// System.Action<object,System.Nullable<System.DateTime>>
	// System.Action<object,System.Nullable<System.DateTimeOffset>>
	// System.Action<object,System.Nullable<System.Decimal>>
	// System.Action<object,System.Nullable<System.Guid>>
	// System.Action<object,System.Nullable<System.TimeSpan>>
	// System.Action<object,System.Nullable<byte>>
	// System.Action<object,System.Nullable<double>>
	// System.Action<object,System.Nullable<float>>
	// System.Action<object,System.Nullable<int>>
	// System.Action<object,System.Nullable<long>>
	// System.Action<object,System.Nullable<object>>
	// System.Action<object,System.Nullable<sbyte>>
	// System.Action<object,System.Nullable<short>>
	// System.Action<object,System.Nullable<uint>>
	// System.Action<object,System.Nullable<ushort>>
	// System.Action<object,System.TimeSpan>
	// System.Action<object,byte>
	// System.Action<object,double>
	// System.Action<object,float>
	// System.Action<object,int>
	// System.Action<object,long>
	// System.Action<object,object>
	// System.Action<object,sbyte>
	// System.Action<object,short>
	// System.Action<object,uint>
	// System.Action<object,ushort>
	// System.Action<object>
	// System.ArraySegment.Enumerator<byte>
	// System.ArraySegment.Enumerator<ushort>
	// System.ArraySegment<byte>
	// System.ArraySegment<ushort>
	// System.Buffers.ArrayPool<ushort>
	// System.Buffers.MemoryManager<ushort>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.LockedStack<ushort>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.PerCoreLockedStacks<ushort>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool<ushort>
	// System.ByReference<byte>
	// System.ByReference<ushort>
	// System.Collections.Generic.ArraySortHelper<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.ArraySortHelper<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ArraySortHelper<byte>
	// System.Collections.Generic.ArraySortHelper<double>
	// System.Collections.Generic.ArraySortHelper<float>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<double>
	// System.Collections.Generic.Comparer<float>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.EqualityComparer<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<byte>
	// System.Collections.Generic.ICollection<double>
	// System.Collections.Generic.ICollection<float>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.IComparer<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IComparer<byte>
	// System.Collections.Generic.IComparer<double>
	// System.Collections.Generic.IComparer<float>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<object,LitJson.ArrayMetadata>
	// System.Collections.Generic.IDictionary<object,LitJson.ObjectMetadata>
	// System.Collections.Generic.IDictionary<object,LitJson.PropertyMetadata>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<byte>
	// System.Collections.Generic.IEnumerable<double>
	// System.Collections.Generic.IEnumerable<float>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<byte>
	// System.Collections.Generic.IEnumerator<double>
	// System.Collections.Generic.IEnumerator<float>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IList<byte>
	// System.Collections.Generic.IList<double>
	// System.Collections.Generic.IList<float>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.IReadOnlyCollection<object>
	// System.Collections.Generic.KeyValuePair<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>,object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.List.Enumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.List.Enumerator<byte>
	// System.Collections.Generic.List.Enumerator<double>
	// System.Collections.Generic.List.Enumerator<float>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.List<byte>
	// System.Collections.Generic.List<double>
	// System.Collections.Generic.List<float>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.Generic.ObjectComparer<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<double>
	// System.Collections.Generic.ObjectComparer<float>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<SQLite4Unity3d.SQLiteConnection.IndexInfo>
	// System.Collections.Generic.ObjectEqualityComparer<ServiceStack.Text.Common.DeserializeDictionary.TypesKey<ServiceStack.Text.Json.JsonTypeSerializer>>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Queue.Enumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.Queue.Enumerator<int>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.Queue<int>
	// System.Collections.Generic.Queue<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<byte>
	// System.Collections.ObjectModel.ReadOnlyCollection<double>
	// System.Collections.ObjectModel.ReadOnlyCollection<float>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Comparison<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Comparison<byte>
	// System.Comparison<double>
	// System.Comparison<float>
	// System.Comparison<int>
	// System.Comparison<object>
	// System.Converter<object,object>
	// System.EventHandler<object>
	// System.Func<SQLite4Unity3d.SQLiteConnection.IndexedColumn,byte>
	// System.Func<SQLite4Unity3d.SQLiteConnection.IndexedColumn,int>
	// System.Func<SQLite4Unity3d.SQLiteConnection.IndexedColumn,object>
	// System.Func<System.IntPtr,int,System.DateTime>
	// System.Func<System.IntPtr,int,System.DateTimeOffset>
	// System.Func<System.IntPtr,int,System.Decimal>
	// System.Func<System.IntPtr,int,System.Guid>
	// System.Func<System.IntPtr,int,System.TimeSpan>
	// System.Func<System.IntPtr,int,byte>
	// System.Func<System.IntPtr,int,double>
	// System.Func<System.IntPtr,int,float>
	// System.Func<System.IntPtr,int,int>
	// System.Func<System.IntPtr,int,long>
	// System.Func<System.IntPtr,int,object>
	// System.Func<System.IntPtr,int,sbyte>
	// System.Func<System.IntPtr,int,short>
	// System.Func<System.IntPtr,int,uint>
	// System.Func<System.IntPtr,int,ushort>
	// System.Func<System.Threading.Tasks.VoidTaskResult>
	// System.Func<System.ValueTuple<object,byte>>
	// System.Func<byte,byte>
	// System.Func<byte,object>
	// System.Func<int>
	// System.Func<object,System.Threading.Tasks.VoidTaskResult>
	// System.Func<object,System.ValueTuple<object,byte>>
	// System.Func<object,byte>
	// System.Func<object,int>
	// System.Func<object,object,System.Threading.Tasks.VoidTaskResult>
	// System.Func<object,object,System.ValueTuple<object,byte>>
	// System.Func<object,object,int>
	// System.Func<object,object,object,object>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.Linq.Buffer<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Linq.Buffer<int>
	// System.Linq.Buffer<object>
	// System.Linq.Enumerable.<DistinctIterator>d__68<object>
	// System.Linq.Enumerable.Iterator<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Linq.Enumerable.Iterator<byte>
	// System.Linq.Enumerable.Iterator<object>
	// System.Linq.Enumerable.WhereArrayIterator<object>
	// System.Linq.Enumerable.WhereEnumerableIterator<object>
	// System.Linq.Enumerable.WhereListIterator<object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<SQLite4Unity3d.SQLiteConnection.IndexedColumn,object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<byte,object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<object,object>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<SQLite4Unity3d.SQLiteConnection.IndexedColumn,object>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<byte,object>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<object,object>
	// System.Linq.Enumerable.WhereSelectListIterator<SQLite4Unity3d.SQLiteConnection.IndexedColumn,object>
	// System.Linq.Enumerable.WhereSelectListIterator<byte,object>
	// System.Linq.Enumerable.WhereSelectListIterator<object,object>
	// System.Linq.EnumerableSorter<SQLite4Unity3d.SQLiteConnection.IndexedColumn,int>
	// System.Linq.EnumerableSorter<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Linq.OrderedEnumerable.<GetEnumerator>d__1<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Linq.OrderedEnumerable<SQLite4Unity3d.SQLiteConnection.IndexedColumn,int>
	// System.Linq.OrderedEnumerable<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Linq.Set<object>
	// System.Memory<ushort>
	// System.Nullable<System.DateTime>
	// System.Nullable<System.DateTimeOffset>
	// System.Nullable<System.Decimal>
	// System.Nullable<System.Guid>
	// System.Nullable<System.TimeSpan>
	// System.Nullable<byte>
	// System.Nullable<double>
	// System.Nullable<float>
	// System.Nullable<int>
	// System.Nullable<long>
	// System.Nullable<object>
	// System.Nullable<sbyte>
	// System.Nullable<short>
	// System.Nullable<uint>
	// System.Nullable<ushort>
	// System.Predicate<SQLite4Unity3d.SQLiteConnection.IndexedColumn>
	// System.Predicate<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Predicate<byte>
	// System.Predicate<double>
	// System.Predicate<float>
	// System.Predicate<int>
	// System.Predicate<object>
	// System.ReadOnlyMemory<ushort>
	// System.ReadOnlySpan.Enumerator<byte>
	// System.ReadOnlySpan.Enumerator<ushort>
	// System.ReadOnlySpan<byte>
	// System.ReadOnlySpan<ushort>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<object,byte>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.ValueTuple<object,byte>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<int>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.ValueTuple<object,byte>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<int>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<object,byte>>
	// System.Runtime.CompilerServices.TaskAwaiter<int>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Span<byte>
	// System.Span<ushort>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.ValueTuple<object,byte>>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<int>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<object>
	// System.Threading.Tasks.Task<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.Task<System.ValueTuple<object,byte>>
	// System.Threading.Tasks.Task<int>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskFactory.<>c<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory.<>c<System.ValueTuple<object,byte>>
	// System.Threading.Tasks.TaskFactory.<>c<int>
	// System.Threading.Tasks.TaskFactory.<>c<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass32_0<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass32_0<System.ValueTuple<object,byte>>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass32_0<int>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass32_0<object>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<System.ValueTuple<object,byte>>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<int>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<object>
	// System.Threading.Tasks.TaskFactory<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory<System.ValueTuple<object,byte>>
	// System.Threading.Tasks.TaskFactory<int>
	// System.Threading.Tasks.TaskFactory<object>
	// System.Tuple<object,object,object>
	// System.Tuple<object,object>
	// System.ValueTuple<object,byte>
	// System.ValueTuple<object,object,object,object,object>
	// System.ValueTuple<object,object,object>
	// System.ValueTuple<object,object>
	// UnityEngine.Events.InvokableCall<byte>
	// UnityEngine.Events.UnityAction<byte>
	// UnityEngine.Events.UnityEvent<byte>
	// }}

	public void RefMethods()
	{
		// System.Void Cysharp.Text.Utf16ValueStringBuilder.Append<int>(int)
		// System.Void Cysharp.Text.Utf16ValueStringBuilder.Append<object>(object)
		// System.Void Cysharp.Text.Utf16ValueStringBuilder.AppendFormat<object,object,object>(string,object,object,object)
		// System.Void Cysharp.Text.Utf16ValueStringBuilder.AppendFormat<object,object>(string,object,object)
		// System.Void Cysharp.Text.Utf16ValueStringBuilder.AppendFormat<object>(string,object)
		// System.Void Cysharp.Text.Utf16ValueStringBuilder.AppendFormatInternal<object>(object,int,System.ReadOnlySpan<System.Char>,string)
		// string Cysharp.Text.ZString.Concat<object,object,int>(object,object,int)
		// string Cysharp.Text.ZString.Concat<object,object,object,object,object,object,object>(object,object,object,object,object,object,object)
		// string Cysharp.Text.ZString.Concat<object,object,object,object,object>(object,object,object,object,object)
		// string Cysharp.Text.ZString.Concat<object,object,object>(object,object,object)
		// string Cysharp.Text.ZString.Concat<object,object>(object,object)
		// string Cysharp.Text.ZString.Format<object,object,object>(string,object,object,object)
		// string Cysharp.Text.ZString.Format<object,object>(string,object,object)
		// string Cysharp.Text.ZString.Format<object>(string,object)
		// Cysharp.Threading.Tasks.UniTask.Awaiter Cysharp.Threading.Tasks.EnumeratorAsyncExtensions.GetAwaiter<object>(object)
		// object DG.Tweening.TweenSettingsExtensions.SetDelay<object>(object,float)
		// object DG.Tweening.TweenSettingsExtensions.SetEase<object>(object,DG.Tweening.Ease)
		// Google.Protobuf.FieldCodec<object> Google.Protobuf.FieldCodec.ForMessage<object>(uint,Google.Protobuf.MessageParser<object>)
		// object LitJson.JsonMapper.ToObject<object>(string,System.Type)
		// object ServiceStack.AutoMappingUtils.ConvertTo<object>(object)
		// object ServiceStack.AutoMappingUtils.ConvertTo<object>(object,bool)
		// object ServiceStack.Text.CsvSerializer.ConvertFrom<object>(object)
		// object ServiceStack.Text.CsvSerializer.DeserializeFromString<object>(string)
		// string ServiceStack.Text.CsvSerializer.SerializeToCsv<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Void ServiceStack.Text.CsvStreamExtensions.WriteCsv<object>(System.IO.TextWriter,System.Collections.Generic.IEnumerable<object>)
		// object System.Activator.CreateInstance<object>()
		// byte[] System.Array.Empty<byte>()
		// double[] System.Array.Empty<double>()
		// float[] System.Array.Empty<float>()
		// int[] System.Array.Empty<int>()
		// long[] System.Array.Empty<long>()
		// object[] System.Array.Empty<object>()
		// bool System.Linq.Enumerable.Any<object>(System.Collections.Generic.IEnumerable<object>)
		// bool System.Linq.Enumerable.Any<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// bool System.Linq.Enumerable.Contains<object>(System.Collections.Generic.IEnumerable<object>,object)
		// bool System.Linq.Enumerable.Contains<object>(System.Collections.Generic.IEnumerable<object>,object,System.Collections.Generic.IEqualityComparer<object>)
		// int System.Linq.Enumerable.Count<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Distinct<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.DistinctIterator<object>(System.Collections.Generic.IEnumerable<object>,System.Collections.Generic.IEqualityComparer<object>)
		// object System.Linq.Enumerable.First<object>(System.Collections.Generic.IEnumerable<object>)
		// object System.Linq.Enumerable.FirstOrDefault<object>(System.Collections.Generic.IEnumerable<object>)
		// object System.Linq.Enumerable.FirstOrDefault<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// object System.Linq.Enumerable.Last<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Linq.IOrderedEnumerable<SQLite4Unity3d.SQLiteConnection.IndexedColumn> System.Linq.Enumerable.OrderBy<SQLite4Unity3d.SQLiteConnection.IndexedColumn,int>(System.Collections.Generic.IEnumerable<SQLite4Unity3d.SQLiteConnection.IndexedColumn>,System.Func<SQLite4Unity3d.SQLiteConnection.IndexedColumn,int>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Select<SQLite4Unity3d.SQLiteConnection.IndexedColumn,object>(System.Collections.Generic.IEnumerable<SQLite4Unity3d.SQLiteConnection.IndexedColumn>,System.Func<SQLite4Unity3d.SQLiteConnection.IndexedColumn,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Select<byte,object>(System.Collections.Generic.IEnumerable<byte>,System.Func<byte,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Select<object,object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,object>)
		// int[] System.Linq.Enumerable.ToArray<int>(System.Collections.Generic.IEnumerable<int>)
		// object[] System.Linq.Enumerable.ToArray<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.List<int> System.Linq.Enumerable.ToList<int>(System.Collections.Generic.IEnumerable<int>)
		// System.Collections.Generic.List<object> System.Linq.Enumerable.ToList<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Iterator<SQLite4Unity3d.SQLiteConnection.IndexedColumn>.Select<object>(System.Func<SQLite4Unity3d.SQLiteConnection.IndexedColumn,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Iterator<byte>.Select<object>(System.Func<byte,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Iterator<object>.Select<object>(System.Func<object,object>)
		// System.Span<ushort> System.MemoryExtensions.AsSpan<ushort>(ushort[],int)
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo,bool)
		// System.Collections.Generic.IEnumerable<object> System.Reflection.CustomAttributeExtensions.GetCustomAttributes<object>(System.Reflection.MemberInfo)
		// System.Collections.Generic.IEnumerable<object> System.Reflection.CustomAttributeExtensions.GetCustomAttributes<object>(System.Reflection.MemberInfo,bool)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter,BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7>(Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter&,BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8>(Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter&,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<object,byte>>,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8>(System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<object,byte>>&,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter,BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7>(Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter&,BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8>(Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter&,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<object,byte>>,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8>(System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<object,byte>>&,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<object,byte>>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssets>d__18>(Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter&,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssets>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<object,byte>>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToThreadPoolAwaitable.Awaiter,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssets>d__18>(Cysharp.Threading.Tasks.SwitchToThreadPoolAwaitable.Awaiter&,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssets>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<object,byte>>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssets>d__18>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssets>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetVersionInfo>d__12>(Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter&,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetVersionInfo>d__12&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.Core.Tools.Http.BWebClient.<DownloadStringTaskAsync>d__0>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.Core.Tools.Http.BWebClient.<DownloadStringTaskAsync>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetVersionInfo>d__12>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetVersionInfo>d__12&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetsInfo>d__13>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetsInfo>d__13&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__28<object>>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__28<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__5>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Test.Reducer_Demo06Copy.<RequestServerByAsync>d__3>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Test.Reducer_Demo06Copy.<RequestServerByAsync>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7>(BDFramework.ResourceMgr.AssetsVersionController.<GetServerVersionInfo>d__7&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8>(BDFramework.ResourceMgr.AssetsVersionController.<StartVersionControl>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<object,byte>>.Start<BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssets>d__18>(BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssets>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.Core.Tools.Http.BWebClient.<DownloadStringTaskAsync>d__0>(BDFramework.Core.Tools.Http.BWebClient.<DownloadStringTaskAsync>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetVersionInfo>d__12>(BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetVersionInfo>d__12&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetsInfo>d__13>(BDFramework.ResourceMgr.AssetsVersionController.<DownloadAssetsInfo>d__13&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__28<object>>(BDFramework.UFlux.Reducer.AReducers.<ExcuteAsync>d__28<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__5>(BDFramework.UFlux.Test.Reducer_Demo06.<RequestServerByAsync>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>.Start<BDFramework.UFlux.Test.Reducer_Demo06Copy.<RequestServerByAsync>d__3>(BDFramework.UFlux.Test.Reducer_Demo06Copy.<RequestServerByAsync>d__3&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,AssetBundleBenchmark01.<IE_02_LoadAll>d__18>(Cysharp.Threading.Tasks.UniTask.Awaiter&,AssetBundleBenchmark01.<IE_02_LoadAll>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,AssetBundleBenchmark01.<IE_02_LoadAll>d__18>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,AssetBundleBenchmark01.<IE_02_LoadAll>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UFlux.Contains.Store.<Dispatch>d__24<object>>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UFlux.Contains.Store.<Dispatch>d__24<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8>(System.Runtime.CompilerServices.TaskAwaiter<object>&,BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<AssetBundleBenchmark01.<IE_02_LoadAll>d__18>(AssetBundleBenchmark01.<IE_02_LoadAll>d__18&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<BDFramework.UFlux.Contains.Store.<Dispatch>d__24<object>>(BDFramework.UFlux.Contains.Store.<Dispatch>d__24<object>&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8>(BDFramework.UnitTest.APITest_LitJson.<AwaitAsyncTest>d__8&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<HotfixCheck.<TestAction>d__1>(HotfixCheck.<TestAction>d__1&)
		// bool System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<uint>()
		// ushort& System.Runtime.CompilerServices.Unsafe.Add<ushort>(ushort&,int)
		// int& System.Runtime.CompilerServices.Unsafe.As<int,int>(int&)
		// int& System.Runtime.CompilerServices.Unsafe.As<object,int>(object&)
		// object& System.Runtime.CompilerServices.Unsafe.As<int,object>(int&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// ushort& System.Runtime.CompilerServices.Unsafe.As<byte,ushort>(byte&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// uint System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(byte&)
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<object>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<uint>()
		// byte& System.Runtime.InteropServices.MemoryMarshal.GetReference<byte>(System.ReadOnlySpan<byte>)
		// uint System.Runtime.InteropServices.MemoryMarshal.Read<uint>(System.ReadOnlySpan<byte>)
		// System.Threading.Tasks.Task<int> System.Threading.Tasks.TaskFactory.StartNew<int>(System.Func<int>,System.Threading.CancellationToken,System.Threading.Tasks.TaskCreationOptions,System.Threading.Tasks.TaskScheduler)
		// System.Threading.Tasks.Task<object> System.Threading.Tasks.TaskFactory.StartNew<object>(System.Func<object>,System.Threading.CancellationToken,System.Threading.Tasks.TaskCreationOptions,System.Threading.Tasks.TaskScheduler)
		// System.Tuple<object,object> System.Tuple.Create<object,object>(object,object)
		// object[] UnityEngine.AssetBundle.ConvertObjects<object>(UnityEngine.Object[])
		// object[] UnityEngine.AssetBundle.LoadAllAssets<object>()
		// object UnityEngine.AssetBundle.LoadAsset<object>(string)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.Component.GetComponentInChildren<object>()
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>(bool)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// object UnityEngine.Object.Instantiate<object>(object)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform,bool)
		// object UnityEngine.Resources.Load<object>(string)
		// string string.Join<object>(string,System.Collections.Generic.IEnumerable<object>)
		// string string.JoinCore<object>(System.Char*,int,System.Collections.Generic.IEnumerable<object>)
	}
}