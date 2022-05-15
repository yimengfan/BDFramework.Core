using UnityEngine;

namespace BDFramework.ResourceMgr
{
	public class Singleton<T> : MonoBehaviour where T : Singleton<T>
	{
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					T[] managers = Object.FindObjectsOfType(typeof(T)) as T[];
					if (managers.Length != 0)
					{
						if (managers.Length == 1)
						{
							instance = managers[0];
							instance.gameObject.name = typeof(T).Name;
							return instance;
						}
						else
						{
							Debug.LogError("Class " + typeof(T).Name + " exists multiple times in violation of singleton pattern. Destroying all copies");
							foreach (T manager in managers)
							{
								Destroy(manager.gameObject);
							}
						}
					}

					var go = new GameObject(typeof(T).Name, typeof(T));
					instance = go.GetComponent<T>();
					DontDestroyOnLoad(go);
				}

				return instance;
			}
			set { instance = value as T; }
		}

		private static T instance;
	}
}