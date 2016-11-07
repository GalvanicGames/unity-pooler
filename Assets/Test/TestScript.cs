using UnityEngine;
using UnityPooler;

namespace UnityPoolerTest
{
	public class TestScript : MonoBehaviour, IGameObjectPoolable
	{
		private void OnEnable()
		{
			Debug.Log("obj get!");
		}

		private void OnDisable()
		{
			Debug.Log("obj released!");
		}

		public virtual void OnObjecteCreated()
		{
			Debug.Log("obj parent created!");
		}

		public void OnObjectReused()
		{
			Debug.Log("obj parent reused!");
		}
	}
}