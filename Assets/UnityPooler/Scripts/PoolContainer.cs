using System.Collections.Generic;
using UnityEngine;
using UnityPooler;

namespace UnityPoolerInternal
{
	public class PoolContainer : MonoBehaviour
	{
		private bool _isApplicationQuitting;
		private System.Action _onDestoryCallback;
		private System.Action _onLevelWasLoadedCallback;
		private List<PoolableGameObject> _objsToRelease = new List<PoolableGameObject>();

		public void RegisterOnDestroy(System.Action callback)
		{
			_onDestoryCallback += callback;
		}

		public void RegisterOnLevelWasLoaded(System.Action callback)
		{
			_onLevelWasLoadedCallback += callback;
		}

		public void ReleaseOnNextTick(PoolableGameObject objToRelease)
		{
			_objsToRelease.Add(objToRelease);
		}

		public void SetAsPersisted()
		{
			DontDestroyOnLoad(gameObject);
		}

		private void OnLevelWasLoaded(int level)
		{
			if (_onLevelWasLoadedCallback != null)
			{
				_onLevelWasLoadedCallback();
			}
		}

		private void Update()
		{
			while (_objsToRelease.Count > 0)
			{
				_objsToRelease[_objsToRelease.Count - 1].Release();
				_objsToRelease.RemoveAt(_objsToRelease.Count - 1);
			}
		}

		private void OnApplicationQuit()
		{
			_isApplicationQuitting = true;
		}

		private void OnDestroy()
		{
			if (_onDestoryCallback != null && !_isApplicationQuitting)
			{
				_onDestoryCallback();
			}

			_onDestoryCallback = null;
		}
	}
}
