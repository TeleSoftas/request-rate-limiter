namespace RequestRateLimiter.Core
{
    internal static class PathHelper
    {
        public static string SanitizePath(string path)
        {
            if (path.EndsWith('/'))
                path = path.Remove(path.Length - 1);
            return path;
        }
    }
}
