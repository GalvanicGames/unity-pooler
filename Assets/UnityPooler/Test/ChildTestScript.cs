using UnityEngine;
using System.Collections;

namespace UnityPoolerTest
{
	public class ChildTestScript : TestScript
	{
		protected override void OnPooledObjCreated()
		{
			//Debug.Log("obj child created!");
		}
	}
}