using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityPooler
{
	/// <summary>
	/// This class is a required component for any prefab that is to be pooled
	/// </summary>
	public class PoolableGameObject : MonoBehaviour
	{
		#region public
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
		/// Attach to be invoked when a new GameObject is created.
		/// </summary>
		public static System.Action<GameObject> onObjectCreation;

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
				gameObject.SetActive(false);
				return;
			}

			if (!_isActive)
			{
				return;
			}

			if (_liveNode != null)
			{
				_originalObject._liveObjs.Remove(_liveNode);
				_originalObject._pooledNodes.Push(_liveNode);
				_liveNode = null;
			}

			_isActive = false;

			if (!persistAcrossScenes && _tempContainer == null ||
				persistAcrossScenes && _persistedContainer == null)
			{
				// Nothing to release to, might be quitting
				return;
			}

			transform.SetParent(container, false);
			gameObject.SetActive(false);
			_originalObject._pooledObjs.Push(this);
			_originalObject._numOfActiveObjs--;
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
		/// Count of GameObjects currently in the pool.
		/// </summary>
		/// <returns>The number of GameObjects in the pool.</returns>
		public int AmountInPool()
		{
			return _pooledObjs.Count;
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

				if (persistAcrossScenes)
				{
					DontDestroyOnLoad(newObj.gameObject);
				}

				newObj.transform.SetParent(container);
				newObj._originalObject = this;

				_pooledObjs.Push(newObj);

				if (!newObj._createMsgSent)
				{
					SendCreationMessage(newObj);
				}

				if (useCap || persistAcrossScenes)
				{
					_pooledNodes.Push(new LinkedListNode<PoolableGameObject>(null));
				}
			}

			gameObject.SetActive(activeState);
		}

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

		//[System.NonSerialized]
		//private IGameObjectPoolable[] _poolables;

		/// <summary>
		/// The function that will be called for reuse on MonoBehaviours. Change this if it conflicts.
		/// </summary>
		private const string TEMP_CONTAINER_NAME = "[ObjectPool]";
		private const string PERSISTED_CONTAINER_NAME = "[Persisted ObjectPool]";

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

		private static Transform _tempContainer;
		private static Transform _persistedContainer;

		private void Awake()
		{
			if (!_createMsgSent)
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
				_tempContainer = new GameObject(TEMP_CONTAINER_NAME).transform;
			}

			if (_persistedContainer == null)
			{
				_persistedContainer = new GameObject(PERSISTED_CONTAINER_NAME).transform;
				DontDestroyOnLoad(_persistedContainer.gameObject);
			}

			SceneManager.activeSceneChanged += SceneTransitioning;
		}

		private void SceneTransitioning(Scene from, Scene to)
		{
			if (persistAcrossScenes)
			{
				if (releaseOnSceneTransition)
				{
					while (_liveObjs.Count > 0)
					{
						_liveObjs.First.Value.Release();
					}
				}
			}
			else
			{
				Clear();
			}
		}

		private void SendCreationMessage(PoolableGameObject newObj)
		{
			newObj._createMsgSent = true;

			if (onObjectCreation != null)
			{
				onObjectCreation(newObj.gameObject);
			}

			var _poolables = newObj.GetComponentsInChildren<IGameObjectPoolable>(true);

			for (int i = 0; i < _poolables.Length; i++)
			{
				_poolables[i].OnObjectCreated();
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

			var _poolables = objToUse.GetComponentsInChildren<IGameObjectPoolable>(true);

			for (int i = 0; i < _poolables.Length; i++)
			{
				_poolables[i].OnObjectReused();
			}

			_liveObjs.AddLast(objNode);

			return objToUse;
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
