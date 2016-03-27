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
		/// Should the pooled objects persist across scenes? Otherwise the pool
		/// is torn down when the next scene is loaded.
		/// </summary>
		public bool persistAcrossScenes = false;

		/// <summary>
		/// If we persist across scenes then should all live objects be released
		/// back into the pool?
		/// </summary>
		public bool releaseOnSceneTransition = true;

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
		/// This is purely informational and would be leveraged by an outside script. Keeps 
		/// the desired pool tied to the object. Call PopulateToDesired() to populate the
		/// pool to the desired number.
		/// </summary>
		public int desiredPopulationAmount;

		/// <summary>
		/// The messaging types.
		/// </summary>
		public enum ReuseMessageType
		{
			/// <summary>
			/// No messaging used, just return the reused object.
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

		/// <summary>
		/// Returns an object from the pool
		/// </summary>
		/// <returns>The object returned.</returns>
		public PoolableGameObject Get()
		{
			Initialize();

			if (_pooledObjs.Count == 0)
			{
				if (useCap && _numOfActiveObjs >= capAmount)
				{
					// We're at cap
					return ReuseObject();
				}

				IncrementPool();
			}

			PoolableGameObject obj;

			do
			{
				obj = _pooledObjs.Pop();
			}
			while (obj == null || obj.gameObject == null);

			obj._isActive = true;
			obj.gameObject.SetActive(true);
			_numOfActiveObjs++;

			if (useCap || persistAcrossScenes)
			{
				// Should always have a node available
				LinkedListNode<PoolableGameObject> liveNode = _pooledNodes.Pop();
				liveNode.Value = obj;
				obj._liveNode = liveNode;

				_liveObjs.AddLast(liveNode);
			}

			return obj;
		}

		/// <summary>
		/// Releases an object back to the pool.
		/// </summary>
		public void Release()
		{
			if (gameObject == null)
			{
				return;
			}

			if (_originalObject == null)
			{
				if (GameObjectPool.verboseLogging)
				{
					Debug.LogWarningFormat(RELEASING_UNPOOLED_OBJ, gameObject.name);
				}

				gameObject.SetActive(false);
				return;
			}

			if (!_isActive)
			{
				if (GameObjectPool.verboseLogging)
				{
					Debug.LogWarningFormat(RELEASED_INACTIVE_OBJ, gameObject.name);
				}
				
				return;
			}

			if (_liveNode != null)
			{
				_originalObject._liveObjs.Remove(_liveNode);
				_originalObject._pooledNodes.Push(_liveNode);
				_liveNode = null;
			}

			_isActive = false;

			if (_tempContainer == null ||
				persistAcrossScenes && _persistedContainer == null)
			{
				// Nothing to release to, might be quitting
				return;
			}

			transform.parent = container;
			gameObject.SetActive(false);
			_originalObject._pooledObjs.Push(this);
			_numOfActiveObjs--;
		}

		public void ReleaseOnNextTick()
		{
			_tempContainer.ReleaseOnNextTick(this);
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
		/// Populates the pool to the desired number set in the inspector.
		/// </summary>
		public void PopulateToDesired()
		{
			PopulatePool(desiredPopulationAmount);
		}

		/// <summary>
		/// Adds 'numberToAdd' to the pool.
		/// </summary>
		/// <param name="numberToAdd">Amount to add to the pool.</param>
		public void AddToPool(int numberToAdd)
		{
			if (numberToAdd <= 0)
			{
				return;
			}

			if (GameObjectPool.verboseLogging)
			{
				Debug.LogFormat("Adding {0} {1} to  pool.", numberToAdd, gameObject.name);
			}

			Initialize();

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
				newObj.transform.SetParent(container);
				newObj._originalObject = this;

				_pooledObjs.Push(newObj);

				if (sendCreationMessage && !newObj._createMsgSent)
				{
					SendCreationMessage(newObj);
				}

				if (useCap || persistAcrossScenes)
				{
					_pooledNodes.Push(new LinkedListNode<PoolableGameObject>(null));
				}

				if (persistAcrossScenes)
				{
					DontDestroyOnLoad(newObj);
				}
			}

			gameObject.SetActive(activeState);
		}

		/// <summary>
		/// Releases all live objects and clears (destroys) the pool.
		/// </summary>
		public void ReleaseObjectsAndClearPool()
		{
			Initialize();
			ReleaseObjects();
			Clear();
		}

		#endregion

		#region private
		[System.NonSerialized]
		private PoolableGameObject _originalObject;

		[System.NonSerialized]
		private bool _isActive;

		[System.NonSerialized]
		private int _numOfActiveObjs;

		[System.NonSerialized]
		private bool _createMsgSent;

		[System.NonSerialized]
		private LinkedListNode<PoolableGameObject> _liveNode;

		[System.NonSerialized]
		private bool _initialized;

		/// <summary>
		/// The function that will be called for creation on MonoBehaviours. Change this if it conflicts.
		/// </summary>
		private const string CREATION_FUNCTION = "OnCreate";

		/// <summary>
		/// The function that will be called for reuse on MonoBehaviours. Change this if it conflicts.
		/// </summary>
		private const string REUSE_FUNCTION = "OnReuse";
		private const string TEMP_CONTAINER_NAME = "[ObjectPool]";
		private const string PERSISTED_CONTAINER_NAME = "[Persisted ObjectPool]";
		private const string RELEASING_UNPOOLED_OBJ = "ObjectPool - {0} is being released but isn't tracked!";
		private const string RELEASED_INACTIVE_OBJ = "ObjectPool - Releasing {0} which is already considered released!";

		private Stack<PoolableGameObject> _pooledObjs
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

		private LinkedList<PoolableGameObject> _liveObjs
		{
			get
			{
				if (_mLiveObjs == null)
				{
					_mLiveObjs = new LinkedList<PoolableGameObject>();
				}

				return _mLiveObjs;
			}
		}

		/// <summary>
		/// Pools own nodes instead of ObjectPooler so the right number can be cleared.
		/// </summary>
		private Stack<LinkedListNode<PoolableGameObject>> _pooledNodes
		{
			get
			{
				if (_mPooledNodes == null)
				{
					_mPooledNodes = new Stack<LinkedListNode<PoolableGameObject>>();
				}

				return _mPooledNodes;
				;
			}
		}

		private Stack<LinkedListNode<PoolableGameObject>> _mPooledNodes;

		[System.NonSerialized]
		private LinkedList<PoolableGameObject> _mLiveObjs;

		public Transform container
		{
			get
			{
				if (persistAcrossScenes)
				{
					return _persistedContainer.transform;
				}

				return _tempContainer.transform;
			}
		}

		private static PoolContainer _tempContainer;
		private static PoolContainer _persistedContainer;

		private void Awake()
		{
			if (!_createMsgSent && sendCreationMessage)
			{
				//Not from object pool...send creation event
				SendCreationMessage(this);
			}
		}

		private void Initialize()
		{
			if (_initialized)
			{
				return;
			}

			_initialized = true;

			if (_tempContainer == null)
			{
				_tempContainer = new GameObject(TEMP_CONTAINER_NAME).AddComponent<PoolContainer>();
			}

			_tempContainer.RegisterOnDestroy(SceneTransitioning);

			if (_persistedContainer == null)
			{
				_persistedContainer = new GameObject(PERSISTED_CONTAINER_NAME).AddComponent<PoolContainer>();
				_persistedContainer.SetAsPersisted();
			}

			if (persistAcrossScenes)
			{
				_persistedContainer.RegisterOnLevelWasLoaded(Reregister);
			}
		}

		private void Reregister()
		{
			if (_tempContainer == null)
			{
				_tempContainer = new GameObject(TEMP_CONTAINER_NAME).AddComponent<PoolContainer>();
			}

			_tempContainer.RegisterOnDestroy(SceneTransitioning);
		}

		private void SendCreationMessage(PoolableGameObject newObj)
		{
			newObj._createMsgSent = true;

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
			LinkedListNode<PoolableGameObject> objNode;

			do
			{
				if (_liveObjs.Count == 0)
				{
					// We couldn't find an object...this is pretty unexpected (capAmount maybe set to 0)
					return null;
				}

				objNode = _liveObjs.First;
				_liveObjs.RemoveFirst();
				objToUse = objNode.Value;
			}
			while (objToUse == null || objToUse.gameObject == null || !objToUse._isActive);

			if (reuseMessaging == ReuseMessageType.EnableDisable)
			{
				objToUse.gameObject.SetActive(false);
				objToUse.gameObject.SetActive(true);
			}
			else if (reuseMessaging == ReuseMessageType.SendMessage)
			{
				SendReuseMessage(objToUse.gameObject);
			}

			_liveObjs.AddLast(objNode);

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

		private void ReleaseObjects()
		{
			// Go through the live objects and release them all
			LinkedListNode<PoolableGameObject> node = _liveObjs.First;

			while (node != null)
			{
				if (node.Value != null)
				{
					node.Value.Release();
				}
				else
				{
					_liveObjs.RemoveFirst();
					_pooledNodes.Push(node);
				}

				// Release removes us from the list
				node = _liveObjs.First;
			}
		}

		private void SceneTransitioning()
		{
			if (persistAcrossScenes)
			{
				if (releaseOnSceneTransition)
				{
					ReleaseObjects();
				}
			}
			else
			{
				Clear();
			}
		}

		/// <summary>
		/// Completely resets the pool. Live objects are on their own but pooled objects
		/// will be destroyed.
		/// </summary>
		private void Clear()
		{
			if (_mPooledObjs != null)
			{
				while (_pooledObjs.Count > 0)
				{
					GameObject obj = _pooledObjs.Pop().gameObject;

					if (obj != null)
					{
						Destroy(obj);
					}
				}
			}

			_mPooledNodes = null;
			_liveNode = null;
			_mPooledObjs = null;
			_mLiveObjs = null;
			_mLiveObjs = null;
			_numOfActiveObjs = 0;
			_initialized = false;
		}

		private void OnDestroy()
		{
			if (_originalObject != null && _isActive)
			{
				_originalObject._numOfActiveObjs--;

				if (_originalObject._numOfActiveObjs < 0)
				{
					_originalObject._numOfActiveObjs = 0;
				}
			}
		}

		#endregion
	}
}