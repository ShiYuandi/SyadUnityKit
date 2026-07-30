using System.IO;
using System.Text;
using Syad.UnityKit.RuntimeSettings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Syad.UnityKit.Editor
{
    /// <summary>为新项目一键创建运行设置入口和默认 JSON。</summary>
    public static class RuntimeSettingsSetupMenu
    {
        private const string MenuPath =
            "Tools/SYAD Unity Kit/Runtime Settings/一键创建运行设置";
        private const string ConfigAssetPath =
            "Assets/StreamingAssets/Configs/runtime-settings.json";

        [MenuItem(MenuPath, false, 100)]
        private static void CreateRuntimeSettingsSetup()
        {
            EnsureDefaultConfigExists();
            RuntimeSettingsBootstrap bootstrap = EnsureBootstrapExists();

            Selection.activeGameObject = bootstrap.gameObject;
            EditorGUIUtility.PingObject(bootstrap.gameObject);
            Debug.Log(
                "SYAD RuntimeSettings：启动对象和默认配置已经准备完成。"
                + "构建 Windows Player 后即可验证窗口设置。",
                bootstrap);
        }

        private static void EnsureDefaultConfigExists()
        {
            if (File.Exists(ConfigAssetPath))
            {
                Debug.Log(
                    "SYAD RuntimeSettings：已保留现有配置，不会覆盖："
                    + ConfigAssetPath);
                return;
            }

            string directory = Path.GetDirectoryName(ConfigAssetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ApplicationRuntimeSettings defaultSettings =
                new ApplicationRuntimeSettings();
            string json = JsonUtility.ToJson(defaultSettings, true);
            File.WriteAllText(
                ConfigAssetPath,
                json,
                new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }

        private static RuntimeSettingsBootstrap EnsureBootstrapExists()
        {
            RuntimeSettingsBootstrap[] existingComponents =
                Resources.FindObjectsOfTypeAll<RuntimeSettingsBootstrap>();
            for (int i = 0; i < existingComponents.Length; i++)
            {
                RuntimeSettingsBootstrap existing = existingComponents[i];
                if (existing.gameObject.scene.IsValid()
                    && !EditorUtility.IsPersistent(existing))
                {
                    return existing;
                }
            }

            GameObject gameObject = new GameObject("Runtime Settings Bootstrap");
            Undo.RegisterCreatedObjectUndo(gameObject, "创建运行设置启动对象");
            RuntimeSettingsBootstrap bootstrap =
                Undo.AddComponent<RuntimeSettingsBootstrap>(gameObject);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            return bootstrap;
        }
    }
}
