using NUnit.Framework;
using Syad.UnityKit.RuntimeSettings;
using UnityEngine;

namespace Syad.UnityKit.Tests
{
    public sealed class RuntimeSettingsTests
    {
        [Test]
        public void DataModel_JsonDeserializesNestedSettings()
        {
            const string json = "{"
                + "\"window\":{"
                + "\"enabled\":true,\"x\":100,\"y\":50,"
                + "\"width\":1280,\"height\":720,"
                + "\"fullscreen\":false,\"borderless\":true,"
                + "\"alwaysOnTop\":true,\"bringToFrontOnce\":false},"
                + "\"cursor\":{\"hidden\":true,\"confineToWindow\":true},"
                + "\"qualityLevel\":3}";

            ApplicationRuntimeSettings settings =
                JsonUtility.FromJson<ApplicationRuntimeSettings>(json);

            Assert.That(settings.window.width, Is.EqualTo(1280));
            Assert.That(settings.window.height, Is.EqualTo(720));
            Assert.That(settings.window.borderless, Is.True);
            Assert.That(settings.window.alwaysOnTop, Is.True);
            Assert.That(settings.cursor.hidden, Is.True);
            Assert.That(settings.cursor.confineToWindow, Is.True);
            Assert.That(settings.qualityLevel, Is.EqualTo(3));
        }

        [Test]
        public void Validator_AcceptsValidSettings()
        {
            ApplicationRuntimeSettings settings = CreateValidSettings();

            string error;
            bool valid = RuntimeSettingsValidator.TryValidate(settings, 6, out error);

            Assert.That(valid, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void Validator_RejectsInvalidResolution()
        {
            ApplicationRuntimeSettings settings = CreateValidSettings();
            settings.window.width = 0;

            string error;
            bool valid = RuntimeSettingsValidator.TryValidate(settings, 6, out error);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.Contain("窗口宽度和高度必须大于 0"));
        }

        [Test]
        public void Validator_RejectsMissingCursorObject()
        {
            ApplicationRuntimeSettings settings = CreateValidSettings();
            settings.cursor = null;

            string error;
            bool valid = RuntimeSettingsValidator.TryValidate(settings, 6, out error);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.Contain("缺少 cursor 对象"));
        }

        [Test]
        public void Validator_RejectsQualityLevelOutsideProjectRange()
        {
            ApplicationRuntimeSettings settings = CreateValidSettings();
            settings.qualityLevel = 6;

            string error;
            bool valid = RuntimeSettingsValidator.TryValidate(settings, 6, out error);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.Contain("超出当前项目的画质等级范围"));
        }

        [Test]
        public void Bootstrap_DefaultsToStandardConfigPath()
        {
            GameObject gameObject = new GameObject("Runtime Settings Bootstrap Test");
            gameObject.SetActive(false);

            try
            {
                RuntimeSettingsBootstrap bootstrap =
                    gameObject.AddComponent<RuntimeSettingsBootstrap>();

                Assert.That(
                    bootstrap.ConfigRelativePath,
                    Is.EqualTo(RuntimeSettingsBootstrap.DefaultConfigRelativePath));
                Assert.That(bootstrap.IsLoaded, Is.False);
                Assert.That(bootstrap.IsApplied, Is.False);
                Assert.That(bootstrap.CurrentSettings, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static ApplicationRuntimeSettings CreateValidSettings()
        {
            return new ApplicationRuntimeSettings
            {
                window = new WindowRuntimeSettings
                {
                    enabled = true,
                    width = 1920,
                    height = 1080
                },
                cursor = new CursorRuntimeSettings(),
                qualityLevel = -1
            };
        }
    }
}
