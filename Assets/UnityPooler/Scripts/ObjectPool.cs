using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityPooler
{
	/// <summary>
	/// This class has contains global settings for all object poolers.
	/// </summary>
	public static class ObjectPoolGeneral
	{
		/// <summary>
		/// Will track if an object is not released. This is a slow
		/// operation and should only be used in dev builds. Does not
		/// work if useCap is enabled.
		/// </summary>
		public static bool detectLeaks;

		public static Action onCheckForLeaks;

		/// <summary>
		/// Call this to force all the object pools to see if there are any leaks.
		/// </summary>
		public static void CheckForLeaks()
		{
			if (onCheckForLeaks != null)
			{
				onCheckForLeaks();
			}
		}
	}

	/// <summary>
	/// This class manages regular C# object pooling. NOT FOR GAMEOBJECTS!
	/// </summary>
	/// <typeparam name="T">The object that is pooled. Must be a class.</typeparam>
	public static class ObjectPool<T> where T : class
	{
		#region public
		/// <summary>
		/// The constructor arguments that should be used. If null then
		/// default constructor is used.
		/// </summary>
		public static object[] constructorArgs;

		/// <summary>
		/// Should the object's pool be capped? If capped then objects will
		/// be reused if the cap is met.
		/// </summary>
		public static bool useCap;

		/// <summary>
		/// If capped, the healAmount that pool is capped to.
		/// </summary>
		public static int capAmount;

		/// <summary>
		/// Should a dictionary cache be used instead of searching through
		/// live objects? Could be faster if there are a lot of objects.
		/// </summary>
		public static bool useDictionaryCache;

		/// <summary>
		/// Returns an object from the object pool.
		/// </summary>
		/// <returns>Object from the object pool.</returns>
		public static T Get()
		{
			CheckForLeaks();

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
			if (objToRelease == null)
			{
				return;
			}

			CheckForLeaks();

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
		/// Releases all objects in the list, the list should be cleared afterwards.
		/// </summary>
		/// <param name="list"></param>
		public static void ReleaseRange(List<T> list)
		{
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					Release(list[i]);
				}
			}
		}

		/// <summary>
		/// Releases all the objects in the array. The array should be nulled afterwards.
		/// </summary>
		/// <param name="arr"></param>
		public static void ReleaseRange(T[] arr)
		{
			if (arr != null)
			{
				for (int i = 0; i < arr.Length; i++)
				{
					Release(arr[i]);
				}
			}
		}

		/// <summary>
		/// Populates the object pool up to the number specified.
		/// </summary>
		/// <param name="numberToPopulate">The healAmount to populate to.</param>
		public static void PopulatePool(int numberToPopulate)
		{
			CreateObjects(numberToPopulate - _pooledObjs.Count);
		}

		/// <summary>
		/// Populates the object pool to the number specified within an enumerator. Intended to work well with
		/// https://github.com/GalvanicGames/unity-game-loader
		/// </summary>
		/// <param name="numToPopulate">The number to populate to.</param>
		/// <returns></returns>
		public static IEnumerator PopulatePoolCo(int numToPopulate)
		{
			int amountToAdd = numToPopulate - AmountInPool();

			for (int i = 0; i < amountToAdd; i++)
			{
				IncrementPool();
				yield return null;
			}

			yield return null;
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
			_liveCache.Clear();
			_liveCache.Clear();
			_nodePool.Clear();
			_pooledObjs.Clear();
			_refs.Clear();
		}

		/// <summary>
		/// Check to see if any objects were GC'd instead of released.
		/// This is done automatically with every get/release/create
		/// but can be triggered this way if not frequent enough.
		/// </summary>
		public static void CheckForLeaks()
		{
			for (int i = 0; i < _refs.Count; i++)
			{
				if (!_refs[i].IsAlive)
				{
					Debug.LogErrorFormat(
						"Object {0} was GC'd and not released back to ObjectPool!",
						typeof(T));

					_refs.RemoveAt(i);
					i--;
				}
			}
		}

		/// <summary>
		/// Returns the current count in the pool.
		/// </summary>
		/// <returns></returns>
		public static int AmountInPool()
		{
			return _pooledObjs.Count;
		}

		#endregion

		#region private

		private static int _numOfActiveObjs;

		private static Stack<T> _pooledObjs = new Stack<T>();
		private static LinkedList<T> _liveObjs = new LinkedList<T>();
		private static Stack<LinkedListNode<T>> _nodePool = new Stack<LinkedListNode<T>>();
		private static Dictionary<T, LinkedListNode<T>> _liveCache = new Dictionary<T, LinkedListNode<T>>();
		private static List<WeakReference> _refs = new List<WeakReference>();

		static ObjectPool()
		{
			ObjectPoolGeneral.onCheckForLeaks += CheckForLeaks;
		}

		private static void CreateObjects(int numberToCreate)
		{
			CheckForLeaks();

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
					newObj = Activator.CreateInstance<T>();
				}
				else
				{
					newObj = (T)Activator.CreateInstance(typeof(T), constructorArgs);
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

				if (!useCap && ObjectPoolGeneral.detectLeaks)
				{
					_refs.Add(new WeakReference(newObj));
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