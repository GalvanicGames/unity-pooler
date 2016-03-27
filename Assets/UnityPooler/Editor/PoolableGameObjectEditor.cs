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
		private SerializedProperty _persistAcrossScenesProp;
		private SerializedProperty _releaseOnSceneTransitionProp;
		private SerializedProperty _desiredPopulationAmountProp;

		private void OnEnable()
		{
			_sendCreationMessageProp = serializedObject.FindProperty("sendCreationMessage");
			_useCapProp = serializedObject.FindProperty("useCap");
			_capAmountProp = serializedObject.FindProperty("capAmount");
			_reuseMessagingProp = serializedObject.FindProperty("reuseMessaging");

			_persistAcrossScenesProp = serializedObject.FindProperty("persistAcrossScenes");
			_releaseOnSceneTransitionProp = serializedObject.FindProperty("releaseOnSceneTransition");
			_desiredPopulationAmountProp = serializedObject.FindProperty("desiredPopulationAmount");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(_persistAcrossScenesProp, new GUIContent("Persist Across Scenes"));

			if (_persistAcrossScenesProp.hasMultipleDifferentValues || _persistAcrossScenesProp.boolValue)
			{
				EditorGUILayout.PropertyField(_releaseOnSceneTransitionProp, new GUIContent("Release Objects on Scene Transition"));
			}

			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(_sendCreationMessageProp, new GUIContent("Send Creation Message"));
			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(_useCapProp, new GUIContent("Use Cap?"));

			if (_useCapProp.hasMultipleDifferentValues || _useCapProp.boolValue)
			{
				EditorGUILayout.PropertyField(_capAmountProp, new GUIContent("Cap Amount"));
				EditorGUILayout.PropertyField(_reuseMessagingProp, new GUIContent("Reuse Message Type"));
			}

			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(_desiredPopulationAmountProp, new GUIContent("Desired Population (DOES NOT AUTO POPULATE)"));
			serializedObject.ApplyModifiedProperties();
		}
	}
}
