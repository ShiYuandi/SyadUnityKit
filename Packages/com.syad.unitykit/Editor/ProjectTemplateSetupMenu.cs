using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Syad.UnityKit.Networking;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Syad.UnityKit.Editor
{
    /// <summary>
    /// 把包内模板复制到使用项目的 Assets，并在脚本编译后创建对应场景对象。
    /// 已经存在的项目脚本不会被覆盖。
    /// </summary>
    public static class ProjectTemplateSetupMenu
    {
        private const string InputMenuPath =
            "Tools/SYAD Unity Kit/Input/创建输入系统模板";
        private const string UdpMenuPath =
            "Tools/SYAD Unity Kit/Networking/创建 UDP 接收器";
        private const string StateMachineMenuPath =
            "Tools/SYAD Unity Kit/StateMachine/创建状态机模板";

        private const string GeneratedRoot =
            "Assets/SyadUnityKit/Generated";
        private const string InputGeneratedDirectory =
            GeneratedRoot + "/Input";
        private const string NetworkingGeneratedDirectory =
            GeneratedRoot + "/Networking";
        private const string StateMachineGeneratedDirectory =
            GeneratedRoot + "/StateMachine";

        private const string InputCommandTypeName =
            "SyadUnityKit.Generated.SyadInputCommand";
        private const string InputSourceTypeName =
            "SyadUnityKit.Generated.SyadInputSource";
        private const string KeyboardInputTypeName =
            "SyadUnityKit.Generated.KeyboardInputSource";
        private const string UdpInputTypeName =
            "SyadUnityKit.Generated.UdpInputSource";
        private const string InputControllerTypeName =
            "SyadUnityKit.Generated.SyadInputController";
        private const string UdpControllerTypeName =
            "SyadUnityKit.Generated.SyadUdpReceiverController";
        private const string StateMachineControllerTypeName =
            "SyadUnityKit.Generated.SyadStateMachineController";

        private const string PendingOperationKey =
            "Syad.UnityKit.Editor.PendingTemplateOperation";
        private const string PendingScenePathKey =
            "Syad.UnityKit.Editor.PendingTemplateScenePath";
        private const string PendingWaitForCompilationKey =
            "Syad.UnityKit.Editor.PendingTemplateWaitForCompilation";
        private const string InputOperation = "Input";
        private const string UdpOperation = "Udp";
        private const string StateMachineOperation = "StateMachine";

        private static readonly UTF8Encoding Utf8WithoutBom =
            new UTF8Encoding(false);

        [MenuItem(InputMenuPath, false, 100)]
        private static void CreateInputTemplate()
        {
            if (!TryBeginOperation(InputOperation))
            {
                return;
            }

            TemplateFile[] files =
            {
                new TemplateFile(
                    "Input/SyadInputCommand.cs.txt",
                    InputGeneratedDirectory + "/SyadInputCommand.cs"),
                new TemplateFile(
                    "Input/SyadInputSource.cs.txt",
                    InputGeneratedDirectory + "/SyadInputSource.cs"),
                new TemplateFile(
                    "Input/KeyboardInputSource.cs.txt",
                    InputGeneratedDirectory + "/KeyboardInputSource.cs"),
                new TemplateFile(
                    "Input/UdpInputSource.cs.txt",
                    InputGeneratedDirectory + "/UdpInputSource.cs"),
                new TemplateFile(
                    "Input/SyadInputController.cs.txt",
                    InputGeneratedDirectory + "/SyadInputController.cs")
            };

            GenerateAndSchedule(InputOperation, files);
        }

        [MenuItem(UdpMenuPath, false, 110)]
        private static void CreateUdpTemplate()
        {
            if (!TryBeginOperation(UdpOperation))
            {
                return;
            }

            TemplateFile[] files =
            {
                new TemplateFile(
                    "Networking/SyadUdpReceiverController.cs.txt",
                    NetworkingGeneratedDirectory
                    + "/SyadUdpReceiverController.cs")
            };

            GenerateAndSchedule(UdpOperation, files);
        }

        [MenuItem(StateMachineMenuPath, false, 120)]
        private static void CreateStateMachineTemplate()
        {
            if (!TryBeginOperation(StateMachineOperation))
            {
                return;
            }

            TemplateFile[] files =
            {
                new TemplateFile(
                    "StateMachine/SyadState.cs.txt",
                    StateMachineGeneratedDirectory + "/SyadState.cs"),
                new TemplateFile(
                    "StateMachine/SyadIdleState.cs.txt",
                    StateMachineGeneratedDirectory + "/SyadIdleState.cs"),
                new TemplateFile(
                    "StateMachine/SyadStateMachineController.cs.txt",
                    StateMachineGeneratedDirectory
                    + "/SyadStateMachineController.cs")
            };

            GenerateAndSchedule(StateMachineOperation, files);
        }

        /// <summary>
        /// 新脚本完成编译并触发域重载后，继续创建之前请求的场景对象。
        /// </summary>
        [DidReloadScripts]
        private static void ContinuePendingOperationAfterCompilation()
        {
            string operation = SessionState.GetString(
                PendingOperationKey,
                string.Empty);
            if (string.IsNullOrEmpty(operation))
            {
                return;
            }

            EditorApplication.delayCall += () =>
                TryCompletePendingOperation(operation, true);
        }

        private static bool TryBeginOperation(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "SYAD 模板工具：请先退出 Play Mode，再创建模板。");
                return false;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning(
                    "SYAD 模板工具：请先保存当前场景，再创建模板对象。");
                return false;
            }

            string pendingOperation = SessionState.GetString(
                PendingOperationKey,
                string.Empty);
            if (!string.IsNullOrEmpty(pendingOperation))
            {
                Debug.LogWarning(
                    "SYAD 模板工具：另一个模板正在等待脚本编译，请稍后重试。");
                return false;
            }

            SessionState.SetString(PendingOperationKey, operation);
            SessionState.SetString(PendingScenePathKey, scene.path);
            return true;
        }

        private static void GenerateAndSchedule(
            string operation,
            IList<TemplateFile> files)
        {
            bool createdAnyFile;
            string error;
            if (!TryWriteTemplates(files, out createdAnyFile, out error))
            {
                ClearPendingOperation();
                Debug.LogError(error);
                return;
            }

            SessionState.SetBool(
                PendingWaitForCompilationKey,
                createdAnyFile);
            AssetDatabase.Refresh();

            if (createdAnyFile)
            {
                Debug.Log(
                    "SYAD 模板工具：脚本已经生成，正在等待 Unity 编译。"
                    + "编译完成后会自动创建场景对象。");
            }

            // 文件早已存在且类型已经编译时，不必等待下一次域重载。
            EditorApplication.delayCall += () =>
                TryCompletePendingOperation(operation, false);
        }

        private static bool TryWriteTemplates(
            IList<TemplateFile> files,
            out bool createdAnyFile,
            out string error)
        {
            createdAnyFile = false;
            error = null;

            string templateRoot;
            try
            {
                templateRoot = GetTemplateRoot();
            }
            catch (Exception exception)
            {
                error = "SYAD 模板工具：无法定位包内模板："
                    + exception.Message;
                return false;
            }

            for (int i = 0; i < files.Count; i++)
            {
                TemplateFile file = files[i];
                string sourcePath = Path.Combine(
                    templateRoot,
                    file.TemplateRelativePath.Replace('/', Path.DirectorySeparatorChar));
                string destinationPath = Path.GetFullPath(
                    file.AssetPath.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(sourcePath))
                {
                    error = "SYAD 模板工具：没有找到包内模板："
                        + sourcePath;
                    return false;
                }

                if (File.Exists(destinationPath))
                {
                    Debug.Log(
                        "SYAD 模板工具：已保留现有脚本，不会覆盖："
                        + file.AssetPath);
                    continue;
                }

                try
                {
                    string directory = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string content = File.ReadAllText(
                        sourcePath,
                        Encoding.UTF8);
                    File.WriteAllText(
                        destinationPath,
                        content,
                        Utf8WithoutBom);
                    createdAnyFile = true;
                    Debug.Log("SYAD 模板工具：已生成：" + file.AssetPath);
                }
                catch (Exception exception)
                {
                    error = "SYAD 模板工具：写入脚本失败："
                        + file.AssetPath
                        + "\n"
                        + exception.Message;
                    return false;
                }
            }

            return true;
        }

        private static string GetTemplateRoot()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(ProjectTemplateSetupMenu).Assembly);
            if (packageInfo == null
                || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                throw new InvalidOperationException(
                    "Unity Package Manager 没有返回 SYAD Unity Kit 的安装路径。");
            }

            return Path.Combine(
                packageInfo.resolvedPath,
                "Editor",
                "Templates~");
        }

        private static void TryCompletePendingOperation(
            string operation,
            bool calledAfterScriptReload)
        {
            string currentOperation = SessionState.GetString(
                PendingOperationKey,
                string.Empty);
            if (currentOperation != operation)
            {
                return;
            }

            if (EditorApplication.isCompiling
                || EditorApplication.isUpdating)
            {
                return;
            }

            string scenePath = SessionState.GetString(
                PendingScenePathKey,
                string.Empty);
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != scenePath)
            {
                ClearPendingOperation();
                Debug.LogWarning(
                    "SYAD 模板工具：等待编译期间当前场景发生变化，"
                    + "已取消创建对象。脚本文件会保留，请回到原场景重新执行菜单。");
                return;
            }

            bool completed = operation == InputOperation
                ? TryCreateInputSceneObject()
                : operation == UdpOperation
                    ? TryCreateUdpSceneObject()
                    : TryCreateStateMachineSceneObject();
            if (!completed)
            {
                bool waitingForCompilation = SessionState.GetBool(
                    PendingWaitForCompilationKey,
                    false);
                if (!calledAfterScriptReload && waitingForCompilation)
                {
                    // 新脚本刚写入时，等待 DidReloadScripts 再尝试。
                    return;
                }

                ClearPendingOperation();
                Debug.LogError(
                    "SYAD 模板工具：没有找到生成脚本中的预期组件类型。"
                    + "请先修复 Console 编译错误；如果你修改了模板类名或命名空间，"
                    + "请手动把脚本挂到场景对象。");
                return;
            }

            ClearPendingOperation();
        }

        private static bool TryCreateInputSceneObject()
        {
            Type commandType = FindType(InputCommandTypeName);
            Type inputSourceType = FindComponentType(InputSourceTypeName);
            Type keyboardType = FindComponentType(KeyboardInputTypeName);
            Type udpInputType = FindComponentType(UdpInputTypeName);
            Type controllerType = FindComponentType(InputControllerTypeName);
            if (commandType == null
                || inputSourceType == null
                || keyboardType == null
                || udpInputType == null
                || controllerType == null)
            {
                return false;
            }

            GameObject gameObject = FindSceneObjectWithComponent(controllerType);
            bool createdObject = gameObject == null;
            if (createdObject)
            {
                gameObject = new GameObject("SYAD Input System");
                Undo.RegisterCreatedObjectUndo(
                    gameObject,
                    "创建 SYAD 输入系统");
            }

            Component keyboard = gameObject.GetComponent(keyboardType);
            if (keyboard == null)
            {
                keyboard = Undo.AddComponent(gameObject, keyboardType);
            }

            Component controller = gameObject.GetComponent(controllerType);
            if (controller == null)
            {
                controller = Undo.AddComponent(gameObject, controllerType);
            }

            EnsureUdpInputSourceExists(udpInputType);
            AssignInputSources(controller, inputSourceType);

            FinishSceneObjectCreation(
                gameObject,
                createdObject
                    ? "SYAD Input：输入系统模板创建完成。"
                    : "SYAD Input：已找到现有输入系统，并补齐所需组件。"
            );
            return true;
        }

        private static bool TryCreateUdpSceneObject()
        {
            Type controllerType = FindComponentType(UdpControllerTypeName);
            if (controllerType == null)
            {
                return false;
            }

            GameObject gameObject = FindSceneObjectWithComponent(controllerType);
            bool createdObject = gameObject == null;
            if (createdObject)
            {
                gameObject = new GameObject("SYAD UDP Receiver");
                Undo.RegisterCreatedObjectUndo(
                    gameObject,
                    "创建 SYAD UDP 接收器");
            }

            UdpReceiverBehaviour receiver =
                gameObject.GetComponent<UdpReceiverBehaviour>();
            if (receiver == null)
            {
                receiver = Undo.AddComponent<UdpReceiverBehaviour>(gameObject);
            }

            Component controller = gameObject.GetComponent(controllerType);
            if (controller == null)
            {
                controller = Undo.AddComponent(gameObject, controllerType);
            }

            SerializedObject serializedController =
                new SerializedObject(controller);
            SerializedProperty receiverProperty =
                serializedController.FindProperty("_receiver");
            if (receiverProperty != null)
            {
                receiverProperty.objectReferenceValue = receiver;
                serializedController.ApplyModifiedProperties();
            }

            Type udpInputType = FindComponentType(UdpInputTypeName);
            if (udpInputType != null)
            {
                EnsureUdpInputSourceOnReceiver(
                    receiver,
                    udpInputType);

                Type inputControllerType =
                    FindComponentType(InputControllerTypeName);
                Type inputSourceType =
                    FindComponentType(InputSourceTypeName);
                if (inputControllerType != null
                    && inputSourceType != null)
                {
                    GameObject inputObject =
                        FindSceneObjectWithComponent(inputControllerType);
                    if (inputObject != null)
                    {
                        Component inputController =
                            inputObject.GetComponent(inputControllerType);
                        AssignInputSources(
                            inputController,
                            inputSourceType);
                    }
                }
            }

            FinishSceneObjectCreation(
                gameObject,
                createdObject
                    ? "SYAD UDP：接收器和处理模板创建完成，默认监听端口为 15000。"
                    : "SYAD UDP：已找到现有接收器，并补齐所需组件。"
            );
            return true;
        }

        private static bool TryCreateStateMachineSceneObject()
        {
            Type controllerType =
                FindComponentType(StateMachineControllerTypeName);
            if (controllerType == null)
            {
                return false;
            }

            GameObject gameObject =
                FindSceneObjectWithComponent(controllerType);
            bool createdObject = gameObject == null;
            if (createdObject)
            {
                gameObject = new GameObject("SYAD StateMachine");
                Undo.RegisterCreatedObjectUndo(
                    gameObject,
                    "创建 SYAD 状态机");
            }

            if (gameObject.GetComponent(controllerType) == null)
            {
                Undo.AddComponent(gameObject, controllerType);
            }

            FinishSceneObjectCreation(
                gameObject,
                createdObject
                    ? "SYAD StateMachine：状态机模板创建完成。"
                    : "SYAD StateMachine：已找到现有状态机对象。"
            );
            return true;
        }

        private static Component EnsureUdpInputSourceExists(Type udpInputType)
        {
            UdpReceiverBehaviour receiver =
                FindSceneComponent<UdpReceiverBehaviour>();
            return receiver == null
                ? null
                : EnsureUdpInputSourceOnReceiver(receiver, udpInputType);
        }

        private static Component EnsureUdpInputSourceOnReceiver(
            UdpReceiverBehaviour receiver,
            Type udpInputType)
        {
            Component udpInput = receiver.GetComponent(udpInputType);
            if (udpInput == null)
            {
                udpInput = Undo.AddComponent(
                    receiver.gameObject,
                    udpInputType);
            }

            SerializedObject serializedInput =
                new SerializedObject(udpInput);
            Undo.RecordObject(udpInput, "配置 SYAD UDP 输入源");
            SerializedProperty receiverProperty =
                serializedInput.FindProperty("_receiver");
            if (receiverProperty != null)
            {
                receiverProperty.objectReferenceValue = receiver;
                serializedInput.ApplyModifiedProperties();
            }

            return udpInput;
        }

        private static void AssignInputSources(
            Component controller,
            Type inputSourceType)
        {
            List<Component> sources =
                FindSceneComponents(inputSourceType);
            Undo.RecordObject(controller, "配置 SYAD 输入源");
            SerializedObject serializedController =
                new SerializedObject(controller);
            SerializedProperty sourcesProperty =
                serializedController.FindProperty("_inputSources");
            if (sourcesProperty == null || !sourcesProperty.isArray)
            {
                return;
            }

            sourcesProperty.arraySize = sources.Count;
            for (int i = 0; i < sources.Count; i++)
            {
                sourcesProperty.GetArrayElementAtIndex(i)
                    .objectReferenceValue = sources[i];
            }

            serializedController.ApplyModifiedProperties();
        }

        private static Type FindComponentType(string fullName)
        {
            Type type = FindType(fullName);
            return type != null && typeof(Component).IsAssignableFrom(type)
                ? type
                : null;
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static GameObject FindSceneObjectWithComponent(Type type)
        {
            List<Component> components = FindSceneComponents(type);
            return components.Count == 0
                ? null
                : components[0].gameObject;
        }

        private static TComponent FindSceneComponent<TComponent>()
            where TComponent : Component
        {
            TComponent[] components =
                Resources.FindObjectsOfTypeAll<TComponent>();
            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < components.Length; i++)
            {
                TComponent component = components[i];
                if (component.gameObject.scene == activeScene
                    && !EditorUtility.IsPersistent(component))
                {
                    return component;
                }
            }

            return null;
        }

        private static List<Component> FindSceneComponents(Type type)
        {
            List<Component> result = new List<Component>();
            Scene activeScene = SceneManager.GetActiveScene();
            UnityEngine.Object[] objects =
                Resources.FindObjectsOfTypeAll(type);
            for (int i = 0; i < objects.Length; i++)
            {
                Component component = objects[i] as Component;
                if (component != null
                    && component.gameObject.scene == activeScene
                    && !EditorUtility.IsPersistent(component))
                {
                    result.Add(component);
                }
            }

            return result;
        }

        private static void FinishSceneObjectCreation(
            GameObject gameObject,
            string message)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
            Debug.Log(message, gameObject);
        }

        private static void ClearPendingOperation()
        {
            SessionState.EraseString(PendingOperationKey);
            SessionState.EraseString(PendingScenePathKey);
            SessionState.EraseBool(PendingWaitForCompilationKey);
        }

        private sealed class TemplateFile
        {
            public readonly string TemplateRelativePath;
            public readonly string AssetPath;

            public TemplateFile(
                string templateRelativePath,
                string assetPath)
            {
                TemplateRelativePath = templateRelativePath;
                AssetPath = assetPath;
            }
        }
    }
}
