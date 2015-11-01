
using System.Collections.Generic;
using UnityEngine;
using UnityPooler;

namespace UnityPoolerInternal
{
	public class PoolContainer : MonoBehaviour
	{
		private List<PoolInfo> _gamePools { get; set; }

		private struct PoolInfo
		{
			public PoolableGameObject pool;
			public System.Action destroyCallback;

			public PoolInfo(PoolableGameObject p, System.Action callback)
			{
				pool = p;
				destroyCallback = callback;
			}
		}

		private void OnDestroy()
		{
			for (int i = 0; i < _gamePools.Count; i++)
			{
				if (_gamePools[i].pool != null && _gamePools[i].pool.gameObject != null)
				{
					_gamePools[i].destroyCallback();
				}
			}
		}

		public void AddPool(PoolableGameObject pool, System.Action destroyCallback)
		{
			if (_gamePools == null)
			{
				_gamePools = new List<PoolInfo>();
			}

			_gamePools.Add(new PoolInfo(pool, destroyCallback));
		}
	}
}
