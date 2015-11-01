using UnityEditor;
using UnityEngine;
using UnityPooler;

namespace UnityPoolerEditor
{
	[CustomEditor(typeof(PoolableGameObject))]
	[CanEditMultipleObjects]
	public class PlatformerMotor2DEditor : Editor
	{
		private SerializedProperty _sendCreationMessageProp;
		private SerializedProperty _useCapProp;
		private SerializedProperty _capAmountProp;
		private SerializedProperty _reuseMessagingProp;

		private void OnEnable()
		{
			_sendCreationMessageProp = serializedObject.FindProperty("sendCreationMessage");
			_useCapProp = serializedObject.FindProperty("useCap");
			_capAmountProp = serializedObject.FindProperty("capAmount");
			_reuseMessagingProp = serializedObject.FindProperty("reuseMessaging");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(_sendCreationMessageProp, new GUIContent("Send Creation Message"));
			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(_useCapProp, new GUIContent("Use Cap?"));

			if (_useCapProp.hasMultipleDifferentValues || _useCapProp.boolValue)
			{
				EditorGUILayout.PropertyField(_capAmountProp, new GUIContent("Cap Amount"));
				EditorGUILayout.PropertyField(_reuseMessagingProp, new GUIContent("Reuse Message Type"));
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
