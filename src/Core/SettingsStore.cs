using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SalaryMonitor.Core
{
    public sealed class SettingsLoadResult
    {
        public AppSettings Settings { get; private set; }
        public bool UsedDefaults { get; private set; }
        public string ErrorMessage { get; private set; }

        public SettingsLoadResult(AppSettings settings, bool usedDefaults, string errorMessage)
        {
            this.Settings = settings;
            this.UsedDefaults = usedDefaults;
            this.ErrorMessage = errorMessage;
        }
    }

    public sealed class SettingsSaveResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }

        private SettingsSaveResult(bool success, string errorMessage)
        {
            this.Success = success;
            this.ErrorMessage = errorMessage;
        }

        public static SettingsSaveResult Ok()
        {
            return new SettingsSaveResult(true, string.Empty);
        }

        public static SettingsSaveResult Fail(string message)
        {
            return new SettingsSaveResult(false, message ?? "未知错误");
        }
    }

    /// <summary>
    /// JSON storage with forgiving load and atomic save. A corrupt or invalid
    /// file never prevents the app from starting; defaults are used instead.
    /// </summary>
    public sealed class SettingsStore
    {
        private readonly string _settingsPath;

        public string SettingsPath
        {
            get { return _settingsPath; }
        }

        public SettingsStore(string settingsPath)
        {
            if (string.IsNullOrEmpty(settingsPath))
            {
                throw new ArgumentException("settingsPath is required", "settingsPath");
            }
            _settingsPath = settingsPath;
        }

        public static string GetDefaultSettingsPath()
        {
#if UI_TEST_BUILD
            string overrideDirectory = Environment.GetEnvironmentVariable("SALARY_MONITOR_CONFIG_DIR");
            if (!string.IsNullOrEmpty(overrideDirectory))
            {
                return Path.Combine(overrideDirectory, "SalaryMonitor", "settings.json");
            }
            string testRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(testRoot, "SalaryMonitor", "settings.json");
#else
            return SettingsPathResolver.ResolveDefault();
#endif
        }

        public SettingsLoadResult Load()
        {
            if (!File.Exists(_settingsPath))
            {
                return new SettingsLoadResult(AppSettings.CreateDefault(), true, null);
            }

            try
            {
                byte[] data = File.ReadAllBytes(_settingsPath);
                if (data.Length == 0)
                {
                    throw new SerializationException("配置文件为空");
                }

                AppSettings settings;
                using (MemoryStream stream = new MemoryStream(data))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(AppSettings));
                    settings = serializer.ReadObject(stream) as AppSettings;
                }

                if (settings == null)
                {
                    throw new SerializationException("配置文件内容无法识别");
                }

                if (settings.Version < 1 || settings.Version > AppSettings.CurrentVersion)
                {
                    throw new SerializationException("配置文件版本不受支持");
                }

                if (settings.Version < AppSettings.CurrentVersion)
                {
                    // v1 files predate the actual-workday flag; the missing bool
                    // deserializes as false, so they migrate without losing data.
                    settings.Version = AppSettings.CurrentVersion;
                }

                ValidationResult validation = SettingsValidator.Validate(settings);
                if (!validation.IsValid)
                {
                    throw new SerializationException(validation.FirstMessage);
                }

                return new SettingsLoadResult(settings, false, null);
            }
            catch (Exception ex)
            {
                return new SettingsLoadResult(
                    AppSettings.CreateDefault(),
                    true,
                    "配置文件不可用，已使用默认设置：" + ex.Message);
            }
        }

        private static void ReplaceByCopy(string tempPath, string destinationPath)
        {
            File.Copy(tempPath, destinationPath, true);
            File.Delete(tempPath);
        }

        public SettingsSaveResult Save(AppSettings settings)
        {
            if (settings == null)
            {
                return SettingsSaveResult.Fail("设置数据为空");
            }

            ValidationResult validation = SettingsValidator.Validate(settings);
            if (!validation.IsValid)
            {
                return SettingsSaveResult.Fail(validation.FirstMessage);
            }

            AppSettings copy = settings.Clone();
            copy.Version = AppSettings.CurrentVersion;

            string directory = Path.GetDirectoryName(_settingsPath);
            string tempPath = _settingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] json;
                using (MemoryStream stream = new MemoryStream())
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(AppSettings));
                    serializer.WriteObject(stream, copy);
                    json = stream.ToArray();
                }

                using (FileStream file = new FileStream(tempPath, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None))
                {
                    file.Write(json, 0, json.Length);
                    file.Flush(true);
                }

                if (File.Exists(_settingsPath))
                {
                    try
                    {
                        File.Replace(tempPath, _settingsPath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceByCopy(tempPath, _settingsPath);
                    }
                    catch (IOException)
                    {
                        ReplaceByCopy(tempPath, _settingsPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Some sandboxed/redirected profiles reject File.Replace
                        // even while ordinary writes are allowed.
                        ReplaceByCopy(tempPath, _settingsPath);
                    }
                }
                else
                {
                    File.Move(tempPath, _settingsPath);
                }

                return SettingsSaveResult.Ok();
            }
            catch (UnauthorizedAccessException ex)
            {
                return SettingsSaveResult.Fail(
                    "保存设置失败：没有写入配置文件的权限。\n配置位置：" + _settingsPath +
                    "\n请将程序移到有写入权限的目录，或从文件资源管理器直接启动。\n" + ex.Message);
            }
            catch (Exception ex)
            {
                return SettingsSaveResult.Fail("保存设置失败：" + ex.Message);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Best effort cleanup only.
                }
            }
        }
    }
}
