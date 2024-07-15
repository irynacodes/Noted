namespace Noted.Helpers
{
    public static class FileHelper
    {
        public static void CreateFile(string path)
        {
            if (!File.Exists(path))
            {
                using var fs = File.Create(path);
            }
        }
    }
}