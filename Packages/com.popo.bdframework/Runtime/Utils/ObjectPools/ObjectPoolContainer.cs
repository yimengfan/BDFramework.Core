namespace BDFramework.ResourceMgr
{
	/// <summary>
	/// Pool容器
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
