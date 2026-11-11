using System;
using System.IO;

namespace SalaryMonitor.Core
{
    /// <summary>
    /// Chooses a writable settings file. The roaming profile is the normal
    /// location; a portable file beside the executable is used when the app is
    /// started by a restricted/low-integrity launcher that cannot write AppData.
    /// </summary>
    public static class SettingsPathResolver
    {
        public static string ResolveDefault()
        {
            return ResolveDefault(new Func<string, bool>(CanWriteDirectory));
        }

        public static string ResolveDefault(Func<string, bool> canWriteDirectory)
        {
            if (canWriteDirectory == null)
            {
                throw new ArgumentNullException("canWriteDirectory");
            }

            string roamingRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string roamingPath = Path.Combine(roamingRoot, "SalaryMonitor", "settings.json");
            string portablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "SalaryMonitor.settings.json");
            return Resolve(roamingPath, portablePath, canWriteDirectory);
        }

        /// <summary>
        /// Prefers an existing portable file, otherwise the normal roaming file.
        /// When roaming is not writable but the executable directory is, the
        /// existing roaming file is copied next to the executable once so the
        /// current settings are preserved.
        /// </summary>
        public static string Resolve(string roamingPath, string portablePath,
            Func<string, bool> canWriteDirectory)
        {
            if (string.IsNullOrEmpty(roamingPath))
            {
                throw new ArgumentException("roamingPath is required", "roamingPath");
            }
            if (string.IsNullOrEmpty(portablePath))
            {
                throw new ArgumentException("portablePath is required", "portablePath");
            }
            if (canWriteDirectory == null)
            {
                throw new ArgumentNullException("canWriteDirectory");
            }

            string roamingFull = Path.GetFullPath(roamingPath);
            string portableFull = Path.GetFullPath(portablePath);
            if (string.Equals(roamingFull, portableFull, StringComparison.OrdinalIgnoreCase))
            {
                return roamingFull;
            }

            bool roamingExists = File.Exists(roamingFull);
            bool portableExists = File.Exists(portableFull);
            bool roamingWritable = SafeCanWrite(canWriteDirectory, Path.GetDirectoryName(roamingFull));
            bool portableWritable = SafeCanWrite(canWriteDirectory, Path.GetDirectoryName(portableFull));

            if (portableExists && portableWritable)
            {
                return portableFull;
            }

            if (roamingWritable)
            {
                return roamingFull;
            }

            if (portableWritable)
            {
                if (!portableExists && roamingExists)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(portableFull));
                        File.Copy(roamingFull, portableFull, false);
                    }
                    catch
                    {
                        // Preserve the readable roaming file for this launch;
                        // saving will report a clear error if it is read-only.
                        return roamingFull;
                    }
                }
                return portableFull;
            }

            // Neither directory is writable. Prefer whichever file exists so
            // the app can still load the user's settings; saving will fail with
            // an actionable message rather than silently replacing them.
            if (portableExists)
            {
                return portableFull;
            }
            return roamingFull;
        }

        private static bool SafeCanWrite(Func<string, bool> canWriteDirectory, string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return false;
            }
            try
            {
                return canWriteDirectory(directory);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tests real write access by creating and removing a uniquely named
        /// probe file. This honors ACLs, read-only media and integrity levels
        /// without relying on exception text or elevation checks.
        /// </summary>
        public static bool CanWriteDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return false;
            }

            string probePath = Path.Combine(directory,
                ".salary-monitor-write-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                Directory.CreateDirectory(directory);
                using (FileStream stream = new FileStream(probePath, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None))
                {
                    stream.WriteByte(0);
                    stream.Flush();
                }
                File.Delete(probePath);
                return true;
            }
            catch
            {
                try
                {
                    if (File.Exists(probePath))
                    {
                        File.Delete(probePath);
                    }
                }
                catch
                {
                    // Best effort cleanup only.
                }
                return false;
            }
        }
    }
}
