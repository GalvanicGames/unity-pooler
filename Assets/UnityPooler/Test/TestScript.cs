using UnityEngine;

namespace UnityPoolerTest
{
	public class TestScript : MonoBehaviour
	{
		protected virtual void OnPooledObjCreated()
		{
			Debug.Log("obj parent created!");
		}

		private void OnEnable()
		{
			Debug.Log("obj get!");
		}

		private void OnDisable()
		{
			Debug.Log("obj released!");
		}
	}
}