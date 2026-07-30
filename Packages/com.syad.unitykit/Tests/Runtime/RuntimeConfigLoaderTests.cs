using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Syad.UnityKit.RuntimeConfig;

namespace Syad.UnityKit.Tests
{
    public sealed class RuntimeConfigLoaderTests
    {
        private string _testRoot;
        private RuntimeConfigLoader _loader;

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "SyadUnityKit.RuntimeConfigTests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_testRoot);
            _loader = new RuntimeConfigLoader(_testRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testRoot) && Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }

        [Test]
        public void LoadText_ReadsUtf8File()
        {
            WriteFile("Configs/message.txt", "中文配置内容");

            string text = _loader.LoadText("Configs/message.txt");

            Assert.That(text, Is.EqualTo("中文配置内容"));
            Assert.That(_loader.Exists("Configs/message.txt"), Is.True);
        }

        [Test]
        public void LoadJson_ReturnsStrongTypedConfig()
        {
            WriteFile(
                "Configs/runtime.json",
                "{\"applicationName\":\"展馆演示\",\"width\":1920,\"fullscreen\":false}");

            TestConfig config = _loader.LoadJson<TestConfig>("Configs/runtime.json");

            Assert.That(config.applicationName, Is.EqualTo("展馆演示"));
            Assert.That(config.width, Is.EqualTo(1920));
            Assert.That(config.fullscreen, Is.False);
        }

        [Test]
        public void TryLoadJson_MissingFileReturnsError()
        {
            TestConfig config;
            string error;

            bool loaded = _loader.TryLoadJson("Configs/missing.json", out config, out error);

            Assert.That(loaded, Is.False);
            Assert.That(config, Is.Null);
            Assert.That(error, Does.Contain("没有找到运行时配置文件"));
        }

        [Test]
        public void TryLoadJson_EmptyFileReturnsError()
        {
            WriteFile("Configs/empty.json", string.Empty);

            TestConfig config;
            string error;
            bool loaded = _loader.TryLoadJson("Configs/empty.json", out config, out error);

            Assert.That(loaded, Is.False);
            Assert.That(config, Is.Null);
            Assert.That(error, Does.Contain("JSON 配置文件为空"));
        }

        [Test]
        public void GetFullPath_RejectsParentDirectoryTraversal()
        {
            string traversalPath = Path.Combine("..", "outside.json");

            Assert.Throws<InvalidOperationException>(() => _loader.GetFullPath(traversalPath));
        }

        [Test]
        public void GetFullPath_RejectsAbsolutePath()
        {
            string absolutePath = Path.Combine(_testRoot, "runtime.json");

            Assert.Throws<ArgumentException>(() => _loader.GetFullPath(absolutePath));
        }

        [Test]
        public void Constructor_RejectsUrlRoot()
        {
            Assert.Throws<NotSupportedException>(
                () => new RuntimeConfigLoader("https://example.com/configs"));
        }

        private void WriteFile(string relativePath, string content)
        {
            string fullPath = Path.Combine(_testRoot, relativePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content, Encoding.UTF8);
        }

        [Serializable]
        private sealed class TestConfig
        {
            public string applicationName;
            public int width;
            public bool fullscreen;
        }
    }
}
