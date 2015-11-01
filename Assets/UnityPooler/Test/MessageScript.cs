using UnityEngine;

namespace UnityPoolerTest
{
	public class MessageScript : MonoBehaviour
	{
		private void OnEnable()
		{
			Debug.Log(gameObject.name + " OnEnable");
		}

		private void OnDisable()
		{
			Debug.Log(gameObject.name + " OnDisable");
		}

		private void OnPooledObjCreated()
		{
			Debug.Log(gameObject.name + " OnPooledObjCreated");
		}

		private void OnPooledObjReused()
		{
			Debug.Log(gameObject.name + " OnPooledObjReused");
		}
	}
}
