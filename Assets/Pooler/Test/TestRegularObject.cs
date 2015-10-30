using UnityEngine;
using System.Collections.Generic;
using UnityPooler;

namespace UnityPoolerTest
{
	public class TestRegularObject : MonoBehaviour
	{
		public int numToPopulate;
		public int numToGet;
		public int numToRelease;

		public bool useCap;
		public int capAmount;
		public bool useCache;

		public class TestClass1 : IPoolable
		{
			public static int numInWild = 0;

			public void OnPoolCreate()
			{

			}

			public void OnPoolGet()
			{
				numInWild++;
			}

			public void OnPoolRelease()
			{
				numInWild--;
			}

			public void OnPoolReuse()
			{
				//Debug.Log("Reused");
			}
		}

		public class TestClass2
		{
			public TestClass2(int a, string b)
			{
				Debug.Log(a + " " + b);
			}
		}

		private List<TestClass1> _objs;

		// Use this for initialization
		void Start()
		{
			_objs = new List<TestClass1>(numToPopulate);
			ObjectPool<TestClass1>.useCap = useCap;
			ObjectPool<TestClass1>.capAmount = capAmount;
			ObjectPool<TestClass1>.useDictionaryCache = useCache;
			ObjectPool<TestClass1>.PopulatePool(numToPopulate);
		}

		// Update is called once per frame
		void Update()
		{
			if (Input.GetKeyDown(KeyCode.G))
			{
				GetObjs();
				Debug.Log("Num in Wild: " + TestClass1.numInWild);
			}

			if (Input.GetKeyDown(KeyCode.R))
			{
				ReleaseObjs();
				Debug.Log("Num in Wild: " + TestClass1.numInWild);
			}

		}

		private void GetObjs()
		{
			for (int i = 0; i < numToGet; i++)
			{
				_objs.Add(ObjectPool<TestClass1>.Get());
			}
		}

		private void ReleaseObjs()
		{
			for (int i = 0; i < numToRelease; i++)
			{
				if (_objs.Count == 0)
				{
					return;
				}

				TestClass1 tc = _objs[_objs.Count - 1];
				_objs.RemoveAt(_objs.Count - 1);

				ObjectPool<TestClass1>.Release(tc);
			}
		}
	}
}
