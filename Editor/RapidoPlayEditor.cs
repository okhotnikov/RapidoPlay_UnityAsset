namespace RapidoPlay
{
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;
    using System.IO;
    using UnityEditor.SceneManagement;

    public static class RapidoPlayEditor
    {
        private const string PREF_KEY = "MyCustomSelectedScene";
        private const string TOOLBAR_ICONS_PATH = "Packages/com.eokhotnikov.rapidoplay/Editor/Icons/";
        private const string ON_PLAY_POSTFIX = "_onPlay";
        private const int MAX_CHARS = 12;
        private static MainToolbarDropdown _toolbarButton;
        private static MainToolbarContent _toolbarButtonContent;
        private static SceneAsset _lastOpenedScene = null;

        public static string SelectedScenePath
        {
            get => EditorPrefs.GetString(PREF_KEY, "");
            set => EditorPrefs.SetString(PREF_KEY, value);
        }

        static RapidoPlayEditor()
        {
            EditorApplication.playModeStateChanged += (state) =>
            {
                MainToolbar.Refresh("RapidoPlay/PlayFirstSceneButton");
                MainToolbar.Refresh("RapidoPlay/PlaySelectedSceneButton");
                HandleOnPlayModeChanged(state);
            };
        }

        [MainToolbarElement("RapidoPlay/SceneSwitcher", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 0)]
        public static MainToolbarElement CreateSceneSwitcher()
        {
            _toolbarButtonContent = new MainToolbarContent("Scenes", "Open and select scenes");
            var path = SelectedScenePath;
            var name = string.IsNullOrEmpty(path) ? "Select Scene" : Path.GetFileNameWithoutExtension(path);
            if (name.Length > MAX_CHARS)
                name = name[..MAX_CHARS] + "...";

            _toolbarButtonContent.text = $"{name}";

            _toolbarButton = new MainToolbarDropdown(_toolbarButtonContent, (Rect rect) =>
            {
                PopupWindow.Show(rect, new ScenePopupContent());
            });

            return _toolbarButton;
        }

        [MainToolbarElement("RapidoPlay/PlaySelectedSceneButton", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 1)]
        public static MainToolbarElement CreatePlaySelectedSceneButton()
        {
            var btnName = "PlaySelectedSceneButton";
            var fullName = EditorApplication.isPlaying ? btnName + ON_PLAY_POSTFIX : btnName;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TOOLBAR_ICONS_PATH}{fullName}.png");
            var content = new MainToolbarContent(icon,"Play Selected Scene");

            return new MainToolbarButton(content, () =>
            {
                if (EditorApplication.isPlaying)
                    return;

                if (string.IsNullOrEmpty(SelectedScenePath))
                {
                    EditorUtility.DisplayDialog(
                        "No Scene Selected",
                        "Please select a scene using the dropdown before trying to play.",
                        "OK");
                    return;
                }

                foreach (var scene in EditorBuildSettings.scenes)
                {
                    if (!scene.path.Equals(SelectedScenePath))
                        continue;

                    StartFromScene(scene.path);
                    return;
                }

                EditorUtility.DisplayDialog(
                    "Scene Not in Build Settings",
                    "The selected scene is not included in the Build Settings. Please add it to the Build Settings to use this feature.",
                    "OK");
            });
        }

        [MainToolbarElement("RapidoPlay/PlayFirstSceneButton", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 2)]
        public static MainToolbarElement CreatePlayFirstSceneButton()
        {
            var btnName = "PlayFirstSceneButton";
            var fullName = EditorApplication.isPlaying ? btnName + ON_PLAY_POSTFIX : btnName;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TOOLBAR_ICONS_PATH}{fullName}.png");
            var content = new MainToolbarContent(icon,"Play First Scene (as device)");

            return new MainToolbarButton(content, () =>
            {
                if (EditorApplication.isPlaying)
                    return;

                if (EditorBuildSettings.scenes.Length > 0)
                {
                    var scenePath = EditorBuildSettings.scenes[0].path;
                    StartFromScene(scenePath);
                }
                else
                {
                    if (EditorUtility.DisplayDialog(
                            "Cannot Start",
                            "No scenes in Build Settings. Please add at least one scene to the Build Settings to use this feature.",
                            "Open Build Settings",
                            "Cancel"))
                    {
                        EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
                    }
                }
            });
        }

        private static void StartFromScene(string scenePath)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            if (EditorApplication.isPlaying)
            {
                _lastOpenedScene = sceneAsset;
                EditorApplication.isPlaying = false;
                return;
            }

            SwitchScene(sceneAsset);
        }

        private static void SwitchScene(SceneAsset scene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.playModeStartScene = scene;
            EditorApplication.isPlaying = true;
        }

        private static void HandleOnPlayModeChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.ExitingPlayMode)
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }

        public static void SelectScene(string path)
        {
            SelectedScenePath = path;
            MainToolbar.Refresh("RapidoPlay/SceneSwitcher");
        }
    }
}
