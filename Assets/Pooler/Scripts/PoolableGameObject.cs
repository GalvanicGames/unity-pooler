using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using UnityPoolerInternal;

namespace UnityPooler
{
	/// <summary>
	/// This class is a required component for any prefab that is to be pooled
	/// </summary>
	public class PoolableGameObject : MonoBehaviour
	{
		#region public
		/// <summary>
		/// Should a creation message be sent. This isn't a cheap operation 
		/// and populating the pool on first frame is desirable.
		/// </summary>
		public bool sendCreationMessage = true;
		
		/// <summary>
		/// Is there a cap for this pool?
		/// </summary>
		public bool useCap;

		/// <summary>
		/// If there is a cap then what is that cap amount?
		/// </summary>
		public int capAmount;

		/// <summary>
		/// If capped and an object is reused then what messaging should be
		/// used.
		/// </summary>
		public ReuseMessageType reuseMessaging = ReuseMessageType.SendMessage;

		/// <summary>
		/// Returns an object from the pool
		/// </summary>
		/// <returns>The object returned.</returns>
		public PoolableGameObject Get()
		{
			if (_mPooledObjs.Count == 0)
			{
				if (useCap && _numOfActiveObjs >= capAmount)
				{
					// We're at cap
					return ReuseObject();
				}

				IncrementPool();
			}

			PoolableGameObject obj = _pooledObjs.Pop();
			obj._isActive = true;
			obj.gameObject.SetActive(true);
			_numOfActiveObjs++;

			if (useCap)
			{
				_liveObjs.Enqueue(obj);
			}

			return obj;
		}

		/// <summary>
		/// Releases an object back to the pool.
		/// </summary>
		public void Release()
		{
			if (_originalObject == null)
			{
				Debug.LogErrorFormat(RELEASING_UNPOOLED_OBJ, gameObject.name);
				return;
			}

			if (!_isActive)
			{
				Debug.LogErrorFormat(RELEASED_INACTIVE_OBJ, gameObject.name);
				return;
			}

			_isActive = false;
			gameObject.SetActive(false);
			_originalObject._pooledObjs.Push(this);
			_numOfActiveObjs--;
		}

		/// <summary>
		/// Adds one object to the pool
		/// </summary>
		public void IncrementPool()
		{
			AddToPool(1);
		}

		/// <summary>
		/// Populates the pool up to 'numberToPopulate' will not add to
		/// the pool past that count.
		/// </summary>
		/// <param name="numberToPopulate">Amount to bring the pool up to.</param>
		public void PopulatePool(int numberToPopulate)
		{
			AddToPool(numberToPopulate - _pooledObjs.Count);
		}

		/// <summary>
		/// Adds 'numberToAdd' to the pool.
		/// </summary>
		/// <param name="numberToAdd">Amount to add to the pool.</param>
		public void AddToPool(int numberToAdd)
		{
			bool activeState = gameObject.activeSelf;
			gameObject.SetActive(false);

			for (int i = 0; i < numberToAdd; i++)
			{
				if (useCap && (_pooledObjs.Count + _numOfActiveObjs >= capAmount))
				{
					// We're done here
					break;
				}

				PoolableGameObject newObj = Instantiate(gameObject).GetComponent<PoolableGameObject>();
				newObj.transform.parent = _container;
				newObj._originalObject = this;

				_pooledObjs.Push(newObj);

				if (sendCreationMessage)
				{
					SendCreationMessage(newObj);
				}
			}

			gameObject.SetActive(activeState);
		}
		#endregion

		#region private
		[System.NonSerialized]
		private PoolableGameObject _originalObject;

		[System.NonSerialized]
		private bool _isActive;

		[System.NonSerialized]
		private bool _isBeingTracked;

		[System.NonSerialized]
		private int _numOfActiveObjs;
		
		private const string CREATION_FUNCTION = "OnPooledObjCreated";
		private const string REUSE_FUNCTION = "OnPooledObjReused";
		private const string CONTAINER_NAME = "[ObjectPool]";
		private const string RELEASING_UNPOOLED_OBJ = "ObjectPool - {0} is being released but isn't tracked!";
		private const string RELEASED_INACTIVE_OBJ = "ObjectPool - Releasing {0} which is already considered released!";

		/// <summary>
		/// The messaging types.
		/// </summary>
		public enum ReuseMessageType
		{
			/// <summary>
			/// No messaging used, just return the resued object.
			/// </summary>
			None,

			/// <summary>
			/// Enable and disable the object to trigger the OnEnable and
			/// OnDisable functions.
			/// </summary>
			EnableDisable,

			/// <summary>
			/// Send message to invokes function OnPooledObjReused on the
			/// object and all active children.
			/// </summary>
			SendMessage
		}

		public Stack<PoolableGameObject> _pooledObjs
		{
			get
			{
				if (_mPooledObjs == null)
				{
					_mPooledObjs = new Stack<PoolableGameObject>();
				}

				return _mPooledObjs;
			}
		}

		[System.NonSerialized]
		private Stack<PoolableGameObject> _mPooledObjs;

		private Queue<PoolableGameObject> _liveObjs
		{
			get
			{
				if (_mLiveObjs == null)
				{
					_mLiveObjs = new Queue<PoolableGameObject>();
				}

				return _mLiveObjs;
			}
		}

		[System.NonSerialized]
		private Queue<PoolableGameObject> _mLiveObjs; 

		private Transform _container
		{
			get
			{
				if (_mContainer == null)
				{
					_mContainer = new GameObject(CONTAINER_NAME).AddComponent<PoolContainer>();
				}

				if (!_isBeingTracked)
				{
					_mContainer.AddPool(this, Clear);
					_isBeingTracked = false;
				}

				return _mContainer.transform;
			}
		}

		private static PoolContainer _mContainer;

		private void SendCreationMessage(PoolableGameObject newObj)
		{
			// The object is inactive so we have to do this manually.
			MonoBehaviour[] behaviours = newObj.GetComponentsInChildren<MonoBehaviour>(true);

			for (int i = 0; i < behaviours.Length; i++)
			{
				MethodInfo method = behaviours[i].GetType().GetMethod(
					CREATION_FUNCTION,
					BindingFlags.NonPublic |
					BindingFlags.Instance |
					BindingFlags.Public,
					null,
					System.Type.EmptyTypes,
					null);

				if (method != null)
				{
					method.Invoke(behaviours[i], null);
				}
			}
		}

		private PoolableGameObject ReuseObject()
		{
			// Find an active object
			PoolableGameObject objToUse;

			do
			{
				if (_liveObjs.Count == 0)
				{
					// We couldn't find an object...this is pretty unexpected (capAmount maybe set to 0)
					return null;
				}

				objToUse = _liveObjs.Dequeue();
			}
			while (!objToUse._isActive);

			if (reuseMessaging == ReuseMessageType.EnableDisable)
			{
				objToUse.gameObject.SetActive(false);
				objToUse.gameObject.SetActive(true);
			}
			else if (reuseMessaging == ReuseMessageType.SendMessage)
			{
				SendReuseMessage(objToUse.gameObject);
			}

			_liveObjs.Enqueue(objToUse);

			return objToUse;
		}

		private void SendReuseMessage(GameObject obj)
		{
			obj.gameObject.SendMessage(REUSE_FUNCTION);

			for (int i = 0; i < obj.transform.childCount; i++)
			{
				GameObject child = obj.transform.GetChild(i).gameObject;

				if (child.activeSelf)
				{
					SendReuseMessage(child);
				}
			}
		}

		private void Clear()
		{
			_mPooledObjs = null;
			_mLiveObjs = null;
			_isBeingTracked = false;
			_numOfActiveObjs = 0;
		}

		#endregion
	}
}