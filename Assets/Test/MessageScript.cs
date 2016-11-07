using UnityEngine;
using UnityPooler;

namespace UnityPoolerTest
{
	public class MessageScript : MonoBehaviour, IGameObjectPoolable
	{
		private void OnEnable()
		{
			Debug.Log(gameObject.name + " OnEnable");
		}

		private void OnDisable()
		{
			Debug.Log(gameObject.name + " OnDisable");
		}

		public void OnObjecteCreated()
		{
			Debug.Log(gameObject.name + " OnPooledObjCreated");
		}

		public void OnObjectReused()
		{
			Debug.Log(gameObject.name + " OnPooledObjReused");
		}
	}
}
