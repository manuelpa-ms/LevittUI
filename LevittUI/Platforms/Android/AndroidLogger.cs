using Android.Util;

namespace LevittUI.Platforms.Android
{
    public static class AndroidLogger
    {
        private const string TAG = "LevittUI";

        public static void LogInfo(string message)
        {
            Log.Info(TAG, message);
        }

        public static void LogError(string message)
        {
            Log.Error(TAG, message);
        }

        public static void LogDebug(string message)
        {
            Log.Debug(TAG, message);
        }

        public static void LogWarning(string message)
        {
            Log.Warn(TAG, message);
        }
    }
}
