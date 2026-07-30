using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Syad.UnityKit.RuntimeConfig
{
    /// <summary>
    /// 从显式根目录安全读取运行时文本或 JSON 配置。
    /// 本类不保存配置，也不负责应用窗口、网络或设备参数。
    /// </summary>
    public sealed class RuntimeConfigLoader
    {
        private readonly string _rootDirectory;
        private readonly string _rootPrefix;

        /// <summary>配置文件允许读取的根目录绝对路径。</summary>
        public string RootDirectory
        {
            get { return _rootDirectory; }
        }

        /// <summary>
        /// 使用指定目录创建加载器。传入路径可以不存在，但必须是本地文件系统路径。
        /// </summary>
        public RuntimeConfigLoader(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("配置根目录不能为空。", nameof(rootDirectory));
            }

            Uri rootUri;
            if (Uri.TryCreate(rootDirectory, UriKind.Absolute, out rootUri))
            {
                if (!rootUri.IsFile)
                {
                    throw new NotSupportedException(
                        "RuntimeConfigLoader 第一版只支持本地文件系统路径，不支持 URL 或压缩包路径："
                        + rootDirectory);
                }

                if (rootDirectory.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    rootDirectory = rootUri.LocalPath;
                }
            }

            _rootDirectory = Path.GetFullPath(rootDirectory);
            _rootPrefix = AppendDirectorySeparator(_rootDirectory);
        }

        /// <summary>
        /// 使用 Unity 的 StreamingAssets 目录创建加载器。
        /// 仅适用于 StreamingAssets 可通过 System.IO 直接读取的平台。
        /// </summary>
        public static RuntimeConfigLoader CreateForStreamingAssets()
        {
            return new RuntimeConfigLoader(Application.streamingAssetsPath);
        }

        /// <summary>判断指定相对路径的配置文件是否存在。</summary>
        public bool Exists(string relativePath)
        {
            return File.Exists(GetFullPath(relativePath));
        }

        /// <summary>读取 UTF-8 文本配置；失败时抛出包含完整路径的异常。</summary>
        public string LoadText(string relativePath)
        {
            string fullPath = GetFullPath(relativePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("没有找到运行时配置文件：" + fullPath, fullPath);
            }

            return File.ReadAllText(fullPath, Encoding.UTF8);
        }

        /// <summary>尝试读取文本配置，并以返回值表示是否成功。</summary>
        public bool TryLoadText(string relativePath, out string text, out string error)
        {
            try
            {
                text = LoadText(relativePath);
                error = null;
                return true;
            }
            catch (Exception exception) when (IsExpectedLoadException(exception))
            {
                text = null;
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// 读取 JSON 并使用 Unity JsonUtility 转换为强类型配置对象。
        /// 配置类型应为可序列化的普通类，并使用字段保存数据。
        /// </summary>
        public TConfig LoadJson<TConfig>(string relativePath) where TConfig : class
        {
            string fullPath = GetFullPath(relativePath);
            string json = LoadText(relativePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException("JSON 配置文件为空：" + fullPath);
            }

            try
            {
                TConfig config = JsonUtility.FromJson<TConfig>(json);
                if (config == null)
                {
                    throw new InvalidDataException(
                        "JSON 配置没有生成 " + typeof(TConfig).FullName + " 对象：" + fullPath);
                }

                return config;
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("JSON 配置格式无效：" + fullPath, exception);
            }
        }

        /// <summary>尝试读取强类型 JSON 配置，并返回便于记录日志的错误信息。</summary>
        public bool TryLoadJson<TConfig>(
            string relativePath,
            out TConfig config,
            out string error) where TConfig : class
        {
            try
            {
                config = LoadJson<TConfig>(relativePath);
                error = null;
                return true;
            }
            catch (Exception exception) when (IsExpectedLoadException(exception))
            {
                config = null;
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// 将相对配置路径解析为根目录下的绝对路径，并拒绝绝对路径和目录越界。
        /// </summary>
        public string GetFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("配置相对路径不能为空。", nameof(relativePath));
            }

            if (Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("配置路径必须是相对于根目录的路径：" + relativePath, nameof(relativePath));
            }

            string fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, relativePath));
            if (!fullPath.StartsWith(_rootPrefix, GetPathComparison()))
            {
                throw new InvalidOperationException("配置路径不能离开根目录：" + relativePath);
            }

            return fullPath;
        }

        private static string AppendDirectorySeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }

        private static StringComparison GetPathComparison()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return StringComparison.OrdinalIgnoreCase;
#else
            return StringComparison.Ordinal;
#endif
        }

        private static bool IsExpectedLoadException(Exception exception)
        {
            return exception is ArgumentException
                || exception is InvalidDataException
                || exception is IOException
                || exception is InvalidOperationException
                || exception is NotSupportedException
                || exception is UnauthorizedAccessException;
        }
    }
}
