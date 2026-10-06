using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace ChassieuVolleyTournament
{
    /// ---------------------------------------------------------------
    /// Central place for file locations (images, data folder, logs).
    /// Images are looked up next to the executable first, so the app
    /// works when launched outside Visual Studio.
    /// ---------------------------------------------------------------
    internal static class AppPaths
    {
        private static Image _logo;
        private static Image _field;
        private static Icon _icon;
        private static readonly object _logLock = new object();

        public static string BaseDir => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>%LOCALAPPDATA%\ChassieuVolleyTournament (created on demand).</summary>
        public static string DataDir
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ChassieuVolleyTournament");
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        public static Image Logo => _logo ?? (_logo = LoadImage("ChassieuLogo.png"));
        public static Image Field => _field ?? (_field = LoadImage("VolleyballField.jpg"));
        public static Icon AppIcon => _icon ?? (_icon = LoadIcon("ChassieuLogo.ico"));

        private static string FindImage(string fileName)
        {
            string[] candidates =
            {
                Path.Combine(BaseDir, "Images", fileName),
                Path.Combine(BaseDir, "..", "..", "Images", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Images", fileName)
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path)) return path;
            }
            return null;
        }

        /// Loads an image without keeping the file locked. Returns null if missing.
        private static Image LoadImage(string fileName)
        {
            try
            {
                string path = FindImage(fileName);
                if (path == null) { Log("Image introuvable : " + fileName); return null; }

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var img = Image.FromStream(stream))
                {
                    return new Bitmap(img);
                }
            }
            catch (Exception ex)
            {
                Log("Erreur chargement image " + fileName + " : " + ex.Message);
                return null;
            }
        }

        private static Icon LoadIcon(string fileName)
        {
            try
            {
                string path = FindImage(fileName);
                return path != null ? new Icon(path) : null;
            }
            catch (Exception ex)
            {
                Log("Erreur chargement icone " + fileName + " : " + ex.Message);
                return null;
            }
        }

        /// ---------------------------------------------------------
        /// Appends a line to %LOCALAPPDATA%\ChassieuVolleyTournament\log.txt
        /// Never throws.
        /// ---------------------------------------------------------
        public static void Log(string message)
        {
            try
            {
                lock (_logLock)
                {
                    string path = Path.Combine(DataDir, "log.txt");
                    File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
