#if UNITY_EDITOR
using DONT_TOUCH.Scripts.BlockComponents;
using UnityEditor;
using UnityEngine;

namespace DONT_TOUCH.Scripts.Editors
{
    [CustomEditor(typeof(PrismaticCloudComponent))]
    [CanEditMultipleObjects]
    public sealed class PrismaticCloudComponentEditor : Editor
    {
        private SerializedProperty _hazardDuration;
        private SerializedProperty _effectDuration;
        private SerializedProperty _effectIntensity;

        private void OnEnable()
        {
            _hazardDuration = serializedObject.FindProperty(nameof(PrismaticCloudComponent.HazardDuration));
            _effectDuration = serializedObject.FindProperty(nameof(PrismaticCloudComponent.EffectDuration));
            _effectIntensity = serializedObject.FindProperty(nameof(PrismaticCloudComponent.EffectIntensity));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            if (_hazardDuration != null && _hazardDuration.floatValue < 0.01f)
                _hazardDuration.floatValue = 0.01f;

            if (_effectDuration != null && _effectDuration.floatValue < 0.01f)
                _effectDuration.floatValue = 0.01f;

            if (_effectIntensity != null && _effectIntensity.intValue < 1)
                _effectIntensity.intValue = 1;

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (Tools.current == Tool.Scale)
                Tools.current = Tool.Move;

            if (target is PrismaticCloudComponent cloud)
                PrimitiveDrawer.LockScale(cloud.transform);
        }
    }
}
#endif
