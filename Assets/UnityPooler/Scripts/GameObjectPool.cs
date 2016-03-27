using UnityEditor;
using UnityEngine;

namespace UnityPooler
{
	/// <summary>
	/// This class acts as an layer between the user and the PoolableGameObject
	/// class.
	/// </summary>
	public static class GameObjectPool
	{
		public static bool verboseLogging;

		private const string REQUIRES_COMP = "ObjectPool - {0} requires a PoolableGameObject component for {1}()";

		/// <summary>
		/// Receives a GameObject from objToCreateFrom's object
		/// pool.
		/// </summary>
		/// <param name="objToCreateFrom">The prefab or GameObject that we
		/// want a duplicated object of.</param>
		/// <returns>A GameObject from the object pool.</returns>
		public static GameObject GetObj(GameObject objToCreateFrom)
		{
			PoolableGameObject poolable = objToCreateFrom.GetComponent<PoolableGameObject>();

			if (poolable == null)
			{
				Debug.LogErrorFormat(REQUIRES_COMP, objToCreateFrom.name, "Get");
				return null;
			}

			return poolable.Get().gameObject;
		}

		/// <summary>
		/// Releases an object back to its object pool.
		/// </summary>
		/// <param name="objToRelease">Object to release</param>
		public static void ReleaseObj(GameObject objToRelease)
		{
			PoolableGameObject poolable = objToRelease.GetComponent<PoolableGameObject>();

			if (poolable == null)
			{
				Debug.LogErrorFormat(REQUIRES_COMP, objToRelease.name, "Release");
				return;
			}

			poolable.Release();
		}

		/// <summary>
		/// Populates the prefab's or GameObject's object pool up to the specified count.
		/// </summary>
		/// <param name="objToPopulate">The prefab or GameObject to populate.</param>
		/// <param name="amount">Number to populate with.</param>
		public static void PopulatePoolWithObj(GameObject objToPopulate, int amount)
		{
			PoolableGameObject poolable = objToPopulate.GetComponent<PoolableGameObject>();

			if (poolable == null)
			{
				Debug.LogErrorFormat(REQUIRES_COMP, objToPopulate.name, "PopulatePool");
				return;
			}

			poolable.PopulatePool(amount);
		}

		/// <summary>
		/// Adds to the prefab's or GameObject's object pool.
		/// </summary>
		/// <param name="objToAddTo">The prefab or GameObject to add to.</param>
		/// <param name="amount">Amount to add.</param>
		public static void AddToPoolWithObj(GameObject objToAddTo, int amount)
		{
			PoolableGameObject poolable = objToAddTo.GetComponent<PoolableGameObject>();

			if (poolable == null)
			{
				Debug.LogErrorFormat(REQUIRES_COMP, objToAddTo.name, "AddToPool");
				return;
			}

			poolable.AddToPool(amount);
		}

		/// <summary>
		/// Increments the prefab's or GameObject's object pool.
		/// </summary>
		/// <param name="objToInc">The prefab or GameObject to increment.</param>
		public static void IncrementPoolWithObj(GameObject objToInc)
		{
			PoolableGameObject poolable = objToInc.GetComponent<PoolableGameObject>();

			if (poolable == null)
			{
				Debug.LogErrorFormat(REQUIRES_COMP, objToInc.name, "IncrementPool");
				return;
			}

			poolable.IncrementPool();
		}

		/// <summary>
		/// Populates the prefab's pool to the desired amount specified in the inspector.
		/// </summary>
		/// <param name="objToPopulate">The prefab to populate.</param>
		public static void PopulateToDesiredWithObj(GameObject objToPopulate)
		{
			PoolableGameObject poolable = objToPopulate.GetComponent<PoolableGameObject>();

			if (poolable == null)
			{
				Debug.LogErrorFormat(REQUIRES_COMP, objToPopulate.name, "PopulateToDesired");
				return;
			}

			poolable.PopulateToDesired();
		}

		/// <summary>
		/// Releases all live objects and clears the pool.
		/// </summary>
		/// <param name="objToClear"></param>
		public static void ReleaseAndClearPoolWithObj(GameObject objToClear)
		{
			PoolableGameObject poolable = objToClear.GetComponent<PoolableGameObject>();

			if (poolable == null)
			{
				Debug.LogErrorFormat(REQUIRES_COMP, objToClear.name, "ReleaseAndClearPool");
				return;
			}

			poolable.ReleaseObjectsAndClearPool();
		}

		//
		// Extensions
		//

		/// <summary>
		/// Gets an object from this GameObject's object pool.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static GameObject Get(this GameObject obj)
		{
			return GetObj(obj);
		}

		/// <summary>
		/// Releases the GameObject back to its object pool.
		/// </summary>
		/// <param name="obj"></param>
		public static void Release(this GameObject obj)
		{
			ReleaseObj(obj);
		}

		/// <summary>
		/// Populates the GameObject's object pool up to the amount specified.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="amount">Number to populate with.</param>
		public static void PopulatePool(this GameObject obj, int amount)
		{
			PopulatePoolWithObj(obj, amount);
		}

		/// <summary>
		/// Adds to the GameObject's object pool count.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="amount">Amount to add to the pool.</param>
		public static void AddToPool(this GameObject obj, int amount)
		{
			AddToPoolWithObj(obj, amount);
		}

		/// <summary>
		/// Increments the GameObject's object pool count.
		/// </summary>
		/// <param name="obj"></param>
		public static void IncrementPool(this GameObject obj)
		{
			IncrementPoolWithObj(obj);
		}

		/// <summary>
		/// Populates the prefab to the desired amount specified in the inspector (on the PoolableGameObject component).
		/// </summary>
		/// <param name="obj">The prefab, or GameObject, to populate.</param>
		public static void PopulateToDesired(this GameObject obj)
		{
			PopulateToDesiredWithObj(obj);
		}

		/// <summary>
		/// Releases all live objects tied to the supplied obj and clears the pool (destroys the objects).
		/// This will generate a lot of garbage.
		/// </summary>
		/// <param name="obj">Prefab, or GameObject, to clear.</param>
		public static void ReleaseAndClearPool(this GameObject obj)
		{
			ReleaseAndClearPoolWithObj(obj);
		}
	}
}
