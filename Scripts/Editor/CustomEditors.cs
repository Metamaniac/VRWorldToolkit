﻿#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit
{
    /// <summary>
    /// Custom editors to improve functionality on the built in VRChat components
    /// </summary>
    public class CustomEditors : MonoBehaviour
    {
        /// <summary>
        /// Custom editor for VRC_MirrorReflection with added quick actions
        /// </summary>
        [CustomEditor(typeof(VRC_MirrorReflection), true, isFallback = false)]
        [CanEditMultipleObjects]
        public class CustomMirrorInspector : Editor
        {
            bool showExplanations;

            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("VRWorld Toolkit Additions", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Quick set Reflect Layers:");

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Show only players")) MirrorLayerChange(262656);

                if (GUILayout.Button("Show players/world")) MirrorLayerChange(262657);

                EditorGUILayout.EndHorizontal();

                if (Selection.gameObjects.Length == 1)
                {
                    var currentMirror = (VRC_MirrorReflection)target;

                    if ((LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.positions.Length == 0 && currentMirror.m_DisablePixelLights) || (LightmapSettings.lightProbes is null && currentMirror.m_DisablePixelLights))
                        EditorGUILayout.HelpBox("No baked light probes found in lighting data. Dynamic objects objects such as players and pickups won't appear lit in mirrors without having baked light probes.", MessageType.Warning);

                    if (currentMirror.m_ReflectLayers == -1025)
                        EditorGUILayout.HelpBox("Avoid using default layers on mirrors to save on frames, you should disable all layers that aren't needed in this mirror.", MessageType.Info);

                    if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("UiMenu"), currentMirror.m_ReflectLayers))
                        EditorGUILayout.HelpBox("Having UiMenu enabled on mirrors causes VRChat UI elements to render twice which can cause noticeable performance drop in populated instances", MessageType.Warning);
                }

                showExplanations = EditorGUILayout.Foldout(showExplanations, "VRChat specific layer explanations");

                if (showExplanations)
                {
                    var style = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true
                    };

                    GUILayout.Label("<b>Player:</b>\nThis layer is used for other players than yourself", style);
                    GUILayout.Label("<b>PlayerLocal:</b>\nThis layer is only used for first-person view and should not be enabled in mirrors", style);
                    GUILayout.Label("<b>MirrorReflection:</b>\nThis layer is used for your own mirror version", style);
                }
            }

            /// <summary>
            /// Change selected Reflect Layers on selected VRC_MirrorReflections to the supplied LayerMask value
            /// </summary>
            /// <param name="layerMask">New LayerMask value to set for Reflect Layers</param>
            private static void MirrorLayerChange(int layerMask)
            {
                for (var index = 0; index < Selection.objects.Length; index++)
                {
                    var gameObject = Selection.objects[index] as GameObject;

                    if (gameObject == null) continue;

                    var mirror = gameObject.GetComponent<VRC_MirrorReflection>();

                    mirror.m_ReflectLayers.value = layerMask;

                    PrefabUtility.RecordPrefabInstancePropertyModifications(mirror);
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        /// <summary>
        /// Custom editor addition for drawing avatar pedestal bounds
        /// </summary>
        [CustomEditor(typeof(VRC_AvatarPedestal), true, isFallback = false)]
        [CanEditMultipleObjects]
        public class CustomAvatarInspector : Editor
        {
            const float innerBound = 1.5f;
            const float outerBound = 2f;

            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("VRWorld Toolkit Additions", EditorStyles.boldLabel);

                GUILayout.Label("Selected IDs (Ordered by hierarchy):");

                var pedestals = serializedObject.targetObjects.Select(x => x as VRC_AvatarPedestal).OrderBy(x => x.transform.GetSiblingIndex());

                foreach (VRC_AvatarPedestal pedestal in pedestals)
                {
                    EditorGUI.BeginChangeCheck();

                    pedestal.blueprintId = EditorGUILayout.DelayedTextField(pedestal.name + " ID: ", pedestal.blueprintId);

                    if (EditorGUI.EndChangeCheck())
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(pedestal);
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    }
                }

                if (GUILayout.Button("Copy selected IDs"))
                {
                    EditorGUIUtility.systemCopyBuffer = String.Join("\n", pedestals.Select(x => x.blueprintId));
                }
            }


            /// <summary>
            /// Draw bounds for selected avatar pedestals
            /// </summary>
            [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
            static void DrawAvatarPedestalGizmos(VRC_AvatarPedestal pedestal, GizmoType gizmoType)
            {
                Transform pedestalTransform;

                //Get transform from the pedestal placement value otherwise get transform of the pedestal itself
                if (pedestal.Placement != null)
                {
                    pedestalTransform = pedestal.Placement;
                }
                else
                {
                    pedestalTransform = pedestal.transform;
                }

                //Set gizmo matrix to match the pedestal for proper placement and rotation
                Gizmos.matrix = pedestalTransform.localToWorldMatrix;

                //Draw the inner and outer bounds
                DrawBound(pedestalTransform, innerBound, Color.green, true);
                DrawBound(pedestalTransform, outerBound, Color.red, false);
            }

            /// <summary>
            /// Helper function for drawing gizmo bounds
            /// </summary>
            /// <param name="placement">Center of the bounds</param>
            /// <param name="size">Size of the bounds</param>
            /// <param name="color">Color of the bounds</param>
            /// <param name="showFront">Whether to change the color depending on which side is being looked at</param>
            private static void DrawBound(Transform placement, float size, Color color, bool showFront)
            {
                if (Vector3.Distance(placement.position, Camera.current.transform.position) < 25f)
                {
                    //Set gizmo color to the passed variable
                    Gizmos.color = color;

                    //Change color to red if showing the front is active and active camera is behind the pedestal
                    if (showFront)
                    {
                        Vector3 cameraDirection = placement.position - Camera.current.transform.position;

                        float angle = Vector3.Angle(placement.forward, cameraDirection);

                        if (Mathf.Abs(angle) < 90)
                        {
                            Gizmos.color = Color.red;
                        }
                    }

                    //Draw the bounds
                    Gizmos.DrawWireCube(Vector3.up * 1.2f, new Vector3(1f * size, 1f * size));
                }
            }
        }
    }
}
#endif