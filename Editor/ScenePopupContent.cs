namespace RapidoPlay
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    public class ScenePopupContent : PopupWindowContent
    {
        private Vector2 _scrollPosition;
        private Texture2D _openIcon;

        private GUIStyle _normalButtonStyle;
        private GUIStyle _selectedButtonStyle;
        private GUIStyle _labelStyle;

        private string _currentScenePath;

        private struct CachedScene
        {
            public int index;
            public string Path;
            public string Name;
        }

        private readonly List<CachedScene> _cachedScenes = new ();

        public ScenePopupContent() : base()
        {
            _openIcon = EditorGUIUtility.IconContent("d_SceneAsset Icon").image as Texture2D;

            var normalTexture = CreateTexture(new Color(0f, 0f, 0f, 0f));
            var selectedTexture = CreateTexture(new Color(0f, 0f, 0f, 0.2f));
            var onHoverState = new GUIStyleState()
            {
                background = selectedTexture,
                textColor = GUI.skin.button.onHover.textColor,
            };

            _normalButtonStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                onHover = onHoverState,
                hover = onHoverState,
                normal =
                {
                    background = normalTexture
                }
            };
            _selectedButtonStyle = new GUIStyle(_normalButtonStyle)
            {
                normal =
                {
                    background = selectedTexture
                }
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    background = normalTexture
                }
            };
        }

        public override void OnOpen()
        {
            _cachedScenes.Clear();
            for (var i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                var scene = EditorBuildSettings.scenes[i];
                if (scene.enabled)
                {
                    _cachedScenes.Add(new CachedScene
                    {
                        index = i,
                        Path = scene.path,
                        Name = Path.GetFileNameWithoutExtension(scene.path)
                    });
                }
            }

            _currentScenePath = SceneManager.GetActiveScene().path;
        }

        public override Vector2 GetWindowSize() => new (300, 300);

        public override void OnGUI(Rect rect)
        {
            if (_cachedScenes.Count == 0)
            {
                GUILayout.Label("No scenes in Build Settings!", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            var currentSelected = RapidoPlayEditor.SelectedScenePath;
            foreach (var scene in _cachedScenes)
            {
                GUILayout.BeginHorizontal();

                var sceneBtnStyle = _currentScenePath.Equals(scene.Path) ? _selectedButtonStyle : _normalButtonStyle;
                if (GUILayout.Button(new GUIContent(_openIcon, "Open Scene"), sceneBtnStyle, GUILayout.Width(24), GUILayout.Height(24)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene.Path);
                        editorWindow.Close();
                    }
                }

                GUILayout.Label(scene.index.ToString(), _labelStyle, GUILayout.Width(24), GUILayout.Height(24));

                var isSelected = currentSelected.Equals(scene.Path);
                if (GUILayout.Button(scene.Name, isSelected ? _selectedButtonStyle : _normalButtonStyle, GUILayout.Height(24)))
                {
                    RapidoPlayEditor.SelectScene(scene.Path);
                }

                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            DrawHorizontalLine();

            if (GUILayout.Button("⚙ Open Build Profiles...", GUILayout.Height(30)))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
                editorWindow.Close();
            }

            if (Event.current.type == EventType.MouseMove && EditorWindow.mouseOverWindow == editorWindow)
                editorWindow?.Repaint();
        }

        private void DrawHorizontalLine()
        {
            var lineRect = EditorGUILayout.GetControlRect(false, 1);
            lineRect.height = 1;
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        public static Texture2D CreateTexture(Color col)
        {
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixel(0, 0, col);
            texture.Apply();
            return texture;
        }
    }
}
