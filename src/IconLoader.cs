using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace AgentSupervisor
{
    /// <summary>
    /// Utility class for loading icons with a fallback strategy.
    /// </summary>
    public static class IconLoader
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Creates an <see cref="Icon"/> from a <see cref="Bitmap"/>, properly releasing the intermediate
        /// HICON handle to avoid a GDI resource leak.
        /// </summary>
        internal static Icon CreateIconFromBitmap(Bitmap bitmap)
        {
            var hIcon = bitmap.GetHicon();
            try
            {
                return (Icon)Icon.FromHandle(hIcon).Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        /// <summary>
        /// Tries to load an icon from the application's icon resource directory at its default size.
        /// Returns <c>null</c> if the file does not exist or cannot be loaded.
        /// </summary>
        /// <param name="fileName">The icon file name (e.g. <see cref="Constants.AppIconFileName"/>).</param>
        /// <returns>The loaded icon, or <c>null</c> on failure.</returns>
        public static Icon? TryLoadDefault(string fileName)
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.IconResourcePath, fileName);
            try
            {
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }

                Logger.LogWarning($"{fileName} not found at {iconPath}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load icon {fileName}", ex);
                return null;
            }
        }

        /// <summary>
        /// Tries to load an icon from the application's icon resource directory.
        /// Returns <c>null</c> if the file does not exist or cannot be loaded.
        /// </summary>
        /// <param name="fileName">The icon file name (e.g. <see cref="Constants.AppIconFileName"/>).</param>
        /// <param name="size">The desired width and height of the icon in pixels.</param>
        /// <returns>The loaded icon, or <c>null</c> on failure.</returns>
        public static Icon? TryLoad(string fileName, int size)
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.IconResourcePath, fileName);
            try
            {
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath, size, size);
                }

                Logger.LogWarning($"{fileName} not found at {iconPath}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load icon {fileName}", ex);
                return null;
            }
        }

        /// <summary>
        /// Loads an icon from the application's icon resource directory, returning a fallback icon if the file
        /// is missing or loading fails.
        /// </summary>
        /// <param name="fileName">The icon file name (e.g. <see cref="Constants.AppIconFileName"/>).</param>
        /// <param name="size">The desired width and height of the icon in pixels.</param>
        /// <param name="fallbackFactory">
        /// A factory function invoked when the icon file does not exist or cannot be loaded.
        /// Receives the requested size and must return a non-null <see cref="Icon"/>.
        /// </param>
        /// <returns>The loaded icon, or the result of <paramref name="fallbackFactory"/> on failure.</returns>
        public static Icon LoadWithFallback(string fileName, int size, Func<int, Icon> fallbackFactory)
        {
            return TryLoad(fileName, size) ?? fallbackFactory(size);
        }
    }
}
