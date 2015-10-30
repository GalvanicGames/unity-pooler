using System.Collections.Generic;

namespace UnityPooler
{
	/// <summary>
	/// This class manages regular C# object pooling. NOT FOR GAMEOBJECTS!
	/// </summary>
	/// <typeparam name="T">The object that is pooled. Must be a class.</typeparam>
	public static class ObjectPool<T> where T : class
	{
		#region public
		/// <summary>
		/// Should the object's pool be capped? If capped then objects will
		/// be reused if the cap is met.
		/// </summary>
		public static bool useCap;

		/// <summary>
		/// If capped, the amount that pool is capped to.
		/// </summary>
		public static int capAmount;

		/// <summary>
		/// Should a dictionary cache be used instead of searching through
		/// live objects? Could be faster if there are a lot of objects.
		/// </summary>
		public static bool useDictionaryCache;

		/// <summary>
		/// The constructor arguments that should be used. If null then
		/// default constructor is used.
		/// </summary>
		public static object[] constructorArgs;

		/// <summary>
		/// Returns an object from the object pool.
		/// </summary>
		/// <returns>Object from the object pool.</returns>
		public static T Get()
		{
			if (_pooledObjs.Count == 0)
			{
				if (useCap && _numOfActiveObjs >= capAmount)
				{
					return ReuseObject();
				}

				IncrementPool();
			}

			T obj = _pooledObjs.Pop();

			if (useCap)
			{
				LinkedListNode<T> node;

				if (_nodePool.Count == 0)
				{
					node = new LinkedListNode<T>(null);
				}
				else
				{
					node = _nodePool.Pop();
				}
				
				node.Value = obj;
				_liveObjs.AddLast(node);

				if (useDictionaryCache)
				{
					_liveCache[obj] = node;
				}
			}

			_numOfActiveObjs++;

			IPoolable poolable = obj as IPoolable;

			if (poolable != null)
			{
				poolable.OnPoolGet();
			}

			return obj;
		}

		/// <summary>
		/// Releases an object back to the object pool.
		/// </summary>
		/// <param name="objToRelease">Object to release</param>
		public static void Release(T objToRelease)
		{
			if (useCap)
			{
				// If we're capping normal objs then removing is a little bit more 
				// expensive. Either searching or dictionary look up.
				LinkedListNode<T> node;

				if (useDictionaryCache)
				{
					if (_liveCache.TryGetValue(objToRelease, out node))
					{
						_liveCache.Remove(objToRelease);
					}
				}
				else
				{
					node = _liveObjs.Find(objToRelease);
				}

				if (node != null)
				{
					_liveObjs.Remove(node);
					_nodePool.Push(node);
				}
			}

			_pooledObjs.Push(objToRelease);
			_numOfActiveObjs--;

			IPoolable poolable = objToRelease as IPoolable;

			if (poolable != null)
			{
				poolable.OnPoolRelease();
			}
		}

		/// <summary>
		/// Populates the object pool up to the number specified.
		/// </summary>
		/// <param name="numberToPopulate">The amount to populate to.</param>
		public static void PopulatePool(int numberToPopulate)
		{
			CreateObjects(numberToPopulate - _pooledObjs.Count);
		}

		/// <summary>
		/// Adds to the object pool count.
		/// </summary>
		/// <param name="numberToAdd">Amount to add.</param>
		public static void AddToPool(int numberToAdd)
		{
			CreateObjects(numberToAdd);
		}

		/// <summary>
		/// Adds one to the object pool count.
		/// </summary>
		public static void IncrementPool()
		{
			CreateObjects(1);
		}

		/// <summary>
		/// Clears the object pool. Calling between scenes can prevent 
		/// memory leaks.
		/// </summary>
		public static void Clear()
		{
			_mLiveCache = null;
			_mLiveObjs = null;
			_mNodePool = null;
			_mPooledObjs = null;
		}
	
		#endregion

		#region private

		private static int _numOfActiveObjs;

		private static Stack<T> _pooledObjs
		{
			get
			{
				if (_mPooledObjs == null)
				{
					_mPooledObjs = new Stack<T>();
				}

				return _mPooledObjs;
			}
		}

		private static Stack<T> _mPooledObjs;

		private static LinkedList<T> _liveObjs
		{
			get
			{
				if (_mLiveObjs == null)
				{
					_mLiveObjs = new LinkedList<T>();
				}

				return _mLiveObjs;
			}
		}

		private static LinkedList<T> _mLiveObjs;

		private static Stack<LinkedListNode<T>> _nodePool
		{
			get
			{
				if (_mNodePool == null)
				{
					_mNodePool = new Stack<LinkedListNode<T>>();
				}

				return _mNodePool;
			}
		}

		private static Stack<LinkedListNode<T>> _mNodePool;

		private static Dictionary<T, LinkedListNode<T>> _liveCache
		{
			get
			{
				if (_mLiveCache == null)
				{
					_mLiveCache = new Dictionary<T, LinkedListNode<T>>();
				}

				return _mLiveCache;
			}
		}

		private static Dictionary<T, LinkedListNode<T>> _mLiveCache;

		private static void CreateObjects(int numberToCreate)
		{
			for (int i = 0; i < numberToCreate; i++)
			{
				if (useCap && (_pooledObjs.Count + _numOfActiveObjs >= capAmount))
				{
					// At cap
					break;
				}

				T newObj;

				if (constructorArgs == null)
				{
					newObj = System.Activator.CreateInstance<T>();
				}
				else
				{
					newObj = (T)System.Activator.CreateInstance(typeof(T), constructorArgs);
				}

				_pooledObjs.Push(newObj);

				IPoolable poolable = newObj as IPoolable;

				if (poolable != null)
				{
					poolable.OnPoolCreate();
				}

				if (useCap)
				{
					// Add to linkedlist pool to avoid garbage later
					_nodePool.Push(new LinkedListNode<T>(null));
				}
			}
		}

		private static T ReuseObject()
		{
			if (_liveObjs.Count == 0)
			{
				// Unexpected, capAmount maybe equal to 0.
				return null;
			}

			LinkedListNode<T> node = _liveObjs.First;
			_liveObjs.RemoveFirst();
			T obj = node.Value;

			IPoolable poolable = obj as IPoolable;

			if (poolable != null)
			{
				poolable.OnPoolReuse();
			}

			_liveObjs.AddLast(node);

			return obj;
		}

		#endregion
	}
}