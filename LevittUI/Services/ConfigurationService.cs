namespace LevittUI.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public string ServerAddress { get; private set; }
        public string DefaultUsername { get; private set; }
        public string DefaultPassword { get; private set; }

        public ConfigurationService()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            // Try to load from environment variables first
            ServerAddress = Environment.GetEnvironmentVariable("LEVITT_SERVER_ADDRESS") ?? DefaultConfiguration.FactoryServerAddress;
            DefaultUsername = Environment.GetEnvironmentVariable("LEVITT_USERNAME") ?? DefaultConfiguration.FactoryUsername;
            DefaultPassword = Environment.GetEnvironmentVariable("LEVITT_PASSWORD") ?? DefaultConfiguration.FactoryPassword;

            // If no environment variables are set, try to load from a local config file
            if (string.IsNullOrEmpty(ServerAddress) || string.IsNullOrEmpty(DefaultUsername) || string.IsNullOrEmpty(DefaultPassword))
            {
                LoadFromLocalConfig();
            }

            // Fallback to factory defaults if still empty (mobile platforms only)
            if (string.IsNullOrEmpty(ServerAddress))
                ServerAddress = DefaultConfiguration.FactoryServerAddress;
            if (string.IsNullOrEmpty(DefaultUsername))
                DefaultUsername = DefaultConfiguration.FactoryUsername;
            if (string.IsNullOrEmpty(DefaultPassword))
                DefaultPassword = DefaultConfiguration.FactoryPassword;
        }

        private void LoadFromLocalConfig()
        {
            try
            {
                var configPath = GetConfigFilePath();
                
                if (File.Exists(configPath))
                {
                    var lines = File.ReadAllLines(configPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("ServerAddress="))
                            ServerAddress = line.Substring("ServerAddress=".Length);
                        else if (line.StartsWith("Username="))
                            DefaultUsername = line.Substring("Username=".Length);
                        else if (line.StartsWith("Password="))
                            DefaultPassword = line.Substring("Password=".Length);
                    }
                }
            }
            catch (Exception)
            {
                // If config file can't be read, use defaults
                // Don't expose any error details to avoid security issues
            }
        }

        private string GetConfigFilePath()
        {
            // Get platform-appropriate application data directory
            string appDataPath;

#if ANDROID
            // Android: Use internal storage
            appDataPath = FileSystem.AppDataDirectory;
#elif IOS
            // iOS: Use Documents directory (backed up to iCloud)
            appDataPath = FileSystem.AppDataDirectory;
#else
            // Windows/Desktop: Use LocalApplicationData
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            appDataPath = Path.Combine(appDataPath, "LevittUI");
#endif

            // Ensure directory exists
            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            return Path.Combine(appDataPath, "config.txt");
        }

        public void SaveConfiguration(string serverAddress, string username, string password)
        {
            try
            {
                var configPath = GetConfigFilePath();
                var lines = new[]
                {
                    $"ServerAddress={serverAddress}",
                    $"Username={username}",
                    $"Password={password}"
                };
                
                File.WriteAllLines(configPath, lines);
                
                // Update current values
                ServerAddress = serverAddress;
                DefaultUsername = username;
                DefaultPassword = password;
            }
            catch (Exception)
            {
                // Silently fail to avoid exposing file system issues
            }
        }
    }
}
