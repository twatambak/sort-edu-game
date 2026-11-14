using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OndeVai.Platform
{
    /// <summary>Hands a generated CSV file to the current platform's share or download UI.</summary>
    public static class CsvExportShare
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int ShareCsvFile(string path);
#elif UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int DownloadCsvFile(string path);
#endif

        /// <summary>Opens the platform share sheet, downloads the file, or opens it locally.</summary>
        public static void Share(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A CSV file path is required.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("The CSV export file was not found.", path);

#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass shareClass = new("com.ondevai.csvshare.CsvShare"))
            {
                bool started = shareClass.CallStatic<bool>("shareCsv", activity, path, "Compartilhar dados CSV");
                if (!started)
                    throw new InvalidOperationException("No compatible app could share the CSV export.");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (ShareCsvFile(path) == 0)
                throw new InvalidOperationException("The iOS share sheet could not be opened.");
#elif UNITY_WEBGL && !UNITY_EDITOR
            if (DownloadCsvFile(path) == 0)
                throw new InvalidOperationException("The browser could not download the CSV export.");
#else
            Application.OpenURL(new Uri(Path.GetFullPath(path)).AbsoluteUri);
#endif
        }
    }
}
