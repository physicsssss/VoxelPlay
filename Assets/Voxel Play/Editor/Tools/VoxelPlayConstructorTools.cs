using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {

    [Serializable]
    public class VoxelPlayConstructorTools : EditorWindow {

        ModelDefinition model;

        [MenuItem("Assets/Create/Voxel Play/Constructor Tools...", false, 1000)]
        public static void ShowWindow() {
            VoxelPlayConstructorTools window = GetWindow<VoxelPlayConstructorTools>("Constructor", true);
            window.minSize = new Vector2(300, 450);
            window.Show();
        }

        void OnGUI() {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null) {
                EditorGUILayout.HelpBox("Constructor tools require Voxel Play Environment in the scene..", MessageType.Info);
                return;
            }
            VoxelPlayFirstPersonController fps = VoxelPlayFirstPersonController.instance;
            if (fps == null) {
                EditorGUILayout.HelpBox("Constructor tools require Voxel Play First Person Controller in the scene..", MessageType.Info);
                return;
            }
            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Constructor tools are only available during Play Mode.", MessageType.Info);
                return;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Toggle Constructor Mode", GUILayout.Width(250), GUILayout.Height(30))) {
                fps.ToggleConstructor();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (!env.constructorMode) {
                return;
            }

            OpenSection();
            fps.constructorSize = EditorGUILayout.IntField("Default Constructor Size", fps.constructorSize);
            EditorGUI.BeginChangeCheck();
            model = (ModelDefinition)EditorGUILayout.ObjectField("Model", model, typeof(ModelDefinition), false);
            if (EditorGUI.EndChangeCheck()) {
                if (model != null) {
                    fps.LoadModel(model);
                }
            }
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Model")) {
                fps.NewModel();
                model = fps.loadModel;
            }
            GUI.enabled = model != null;
            if (GUILayout.Button("Load")) {
                fps.LoadModel(model);
            }
            if (GUILayout.Button("Save")) {
                fps.SaveModel(false);
                GUIUtility.ExitGUI ();
            }
            GUI.enabled = true;
            if (GUILayout.Button("Save As New...")) {
                if (fps.SaveModel(true)) {
                    model = fps.loadModel;
                }
                GUIUtility.ExitGUI ();
            }
            EditorGUILayout.EndHorizontal();
            CloseSection();

            OpenSection();
            EditorGUILayout.BeginHorizontal();
            DrawHeaderLabel("Displace");
            if (GUILayout.Button("<X")) {
                fps.DisplaceModel(-1, 0, 0);
            }

            if (GUILayout.Button("X>")) {
                fps.DisplaceModel(1, 0, 0);
            }
            if (GUILayout.Button("<Y")) {
                fps.DisplaceModel(0, -1, 0);
            }

            if (GUILayout.Button("Y>")) {
                fps.DisplaceModel(0, 1, 0);
            }

            if (GUILayout.Button("<Z")) {
                fps.DisplaceModel(0, 0, -1);
            }

            if (GUILayout.Button("Z>")) {
                fps.DisplaceModel(0, 0, 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawHeaderLabel("Resize Area");
            if (GUILayout.Button("-X")) {
                fps.ResizeModel(-1, 0, 0);
            }

            if (GUILayout.Button("+X")) {
                fps.ResizeModel(1, 0, 0);
            }
            if (GUILayout.Button("-Y")) {
                fps.ResizeModel(0, -1, 0);
            }

            if (GUILayout.Button("+Y")) {
                fps.ResizeModel(0, 1, 0);
            }

            if (GUILayout.Button("-Z")) {
                fps.ResizeModel(0, 0, -1);
            }

            if (GUILayout.Button("+Z")) {
                fps.ResizeModel(0, 0, 1);
            }
            EditorGUILayout.EndHorizontal();

            CloseSection();
        }

        void OpenSection() {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
        }

        void CloseSection() {
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        void NewModel(VoxelPlayFirstPersonController fps) {
            if (!EditorUtility.DisplayDialog("New Model", "Discard any change and start a new model definition?", "Ok", "Cancel")) {
                return;
            }
            fps.NewModel();
        }


        void DrawHeaderLabel(string s) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(s, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }


    }

}