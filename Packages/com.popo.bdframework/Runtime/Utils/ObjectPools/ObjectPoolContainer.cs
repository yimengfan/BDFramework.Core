namespace BDFramework.Utils
{
	/// <summary>
	/// 对象池容器，包装单个池化对象的使用状态。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObjectPoolContainer<T>
	{
		private T item;

		public bool Used { get; private set; }

		public void Consume()
		{
			Used = true;
		}

		public T Item
		{
			get
			{
				return item;
			}
			set
			{
				item = value;
			}
		}

		public void Release()
		{
			Used = false;
		}
	}
}
