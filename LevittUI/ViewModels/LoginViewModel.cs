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

        public LoginViewModel(IHomeAutomationService homeAutomationService, IConfigurationService configurationService)
        {
            _homeAutomationService = homeAutomationService;
            _configurationService = configurationService;
            Title = "Home Automation Login";
            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
            
            // Load default values from configuration (if any)
            LoadDefaults();
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

        public ICommand LoginCommand { get; }

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

            try
            {
                var success = await _homeAutomationService.LoginAsync(Username, Password);
                
                if (success)
                {
                    StatusMessage = "Login successful!";
                    
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
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection error: Please check server address and network connection.";
            }
            finally
            {
                IsBusy = false;
                ((Command)LoginCommand).ChangeCanExecute();
            }
        }
    }
}
