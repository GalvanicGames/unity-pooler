using UnityEngine;
using System.Collections.Generic;
using UnityPooler;

namespace UnityPoolerTest
{
	public class ExampleTest : MonoBehaviour
	{
		public int numberToPopulate;
		public int numberToCreateOnPress;
		public int numberToReleaseOnPress;

		public GameObject poolablePrefab;

		private List<GameObject> _createdObjs = new List<GameObject>();

		// Use this for initialization
		void Start()
		{
			poolablePrefab.PopulatePool(numberToPopulate);
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.G))
			{
				GetObjs();
			}

			if (Input.GetKeyDown(KeyCode.R))
			{
				ReleaseObjs();
			}
		}

		public void GetObjs()
		{
			for (int i = 0; i < numberToCreateOnPress; i++)
			{
				_createdObjs.Add(poolablePrefab.Get());
			}
		}

		public void ReleaseObjs()
		{
			for (int i = 0; i < numberToReleaseOnPress; i++)
			{
				if (_createdObjs.Count == 0)
				{
					break;
				}

				_createdObjs[_createdObjs.Count - 1].Release();
				_createdObjs.RemoveAt(_createdObjs.Count - 1);
			}
		}
	}
}