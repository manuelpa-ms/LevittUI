using System.Windows.Input;
using LevittUI.Services;

namespace LevittUI.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IHomeAutomationService _homeAutomationService;
        private readonly IConfigurationService _configurationService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _serverAddress = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _hasAttemptedAutoLogin = false;

        public LoginViewModel(IHomeAutomationService homeAutomationService, IConfigurationService configurationService)
        {
            _homeAutomationService = homeAutomationService;
            _configurationService = configurationService;
            Title = "Home Automation Login";
            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
            
            // Load default values from configuration (if any)
            LoadDefaults();
            
            // Attempt auto-login if credentials are available and enabled
            _ = Task.Run(async () => await TryAutoLoginAsync());
        }

        private void LoadDefaults()
        {
            Username = _configurationService.DefaultUsername;
            Password = _configurationService.DefaultPassword;
            ServerAddress = _configurationService.ServerAddress;
            
            // Show helpful message if using factory defaults
#if ANDROID || IOS
            if (!string.IsNullOrEmpty(DefaultConfiguration.FactoryServerAddress) && 
                ServerAddress == DefaultConfiguration.FactoryServerAddress)
            {
                // Factory defaults are loaded for mobile - user can modify if needed
            }
#endif
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set => SetProperty(ref _serverAddress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsAutoLoginEnabled
        {
            get => _configurationService.IsAutoLoginEnabled;
            set 
            { 
                if (_configurationService.IsAutoLoginEnabled != value)
                {
                    _configurationService.SetAutoLoginEnabled(value);
                    OnPropertyChanged();
                }
            }
        }

        public ICommand LoginCommand { get; }

        /// <summary>
        /// Allows manual triggering of auto-login functionality
        /// </summary>
        public async Task RetryAutoLoginAsync()
        {
            _hasAttemptedAutoLogin = false;
            await TryAutoLoginAsync();
        }

        public async Task TryAutoLoginAsync()
        {
            // Prevent multiple auto-login attempts
            if (_hasAttemptedAutoLogin || !_configurationService.IsAutoLoginEnabled)
                return;

            _hasAttemptedAutoLogin = true;

            // Wait a moment to let the UI initialize
            await Task.Delay(500);

            // Check if we have any credentials available (factory defaults or saved)
            if (!string.IsNullOrWhiteSpace(Username) && 
                !string.IsNullOrWhiteSpace(Password) && 
                !string.IsNullOrWhiteSpace(ServerAddress))
            {
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.DispatchAsync(async () =>
                    {
                        StatusMessage = "Auto-connecting with available credentials...";
                        await Task.Delay(100); // Small delay to show the message
                        await LoginAsync();
                    });
                }
            }
            else
            {
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.DispatchAsync(() =>
                    {
                        StatusMessage = "Please enter your credentials to connect.";
                    });
                }
            }
        }

        private async Task LoginAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ServerAddress))
            {
                StatusMessage = "Please fill in all fields.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Connecting...";

            // Add diagnostic logging
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Attempting login to server: {ServerAddress} with user: {Username}");

            try
            {
                var success = await _homeAutomationService.LoginAsync(Username, Password);
                
                if (success)
                {
                    StatusMessage = "Login successful!";
                    System.Diagnostics.Debug.WriteLine("[LoginViewModel] Login successful");
                    
                    // Save configuration for next time (optional - user choice)
                    if (_configurationService is ConfigurationService configService)
                    {
                        configService.SaveConfiguration(ServerAddress, Username, Password);
                    }
                    
                    // Navigate to main page
                    await Shell.Current.GoToAsync("main");
                }
                else
                {
                    StatusMessage = "Login failed. Please check your credentials.";
                    System.Diagnostics.Debug.WriteLine("[LoginViewModel] Login failed - authentication error");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Connection error: Please check server address and network connection";
                StatusMessage = ex.Message;
                
                // Detailed logging for debugging
                System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Login exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Full stack trace: {ex.StackTrace}");
            }
            finally
            {
                IsBusy = false;
                ((Command)LoginCommand).ChangeCanExecute();
            }
        }
    }
}
