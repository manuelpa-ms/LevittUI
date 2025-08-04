using System.Collections.ObjectModel;
using System.Windows.Input;
using LevittUI.Models;
using LevittUI.Services;

namespace LevittUI.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IHomeAutomationService _homeAutomationService;
        private bool _isRefreshing;
        private Room? _selectedRoom;
        private string _lastUpdated = "Never";
        private bool _isAcOn;
        private bool _isAcToggling;
        private bool _isBlindsMoving;

        public MainViewModel(IHomeAutomationService homeAutomationService)
        {
            _homeAutomationService = homeAutomationService;
            Title = "Home Automation";
            
            Rooms = new ObservableCollection<Room>();
            
            // Pre-populate with placeholder rooms
            InitializePlaceholderRooms();
            
            RefreshCommand = new Command(async () => await RefreshDataAsync());
            ToggleAcCommand = new Command<Room>(async (room) => await ToggleAcAsync(room));
            IncreaseTempCommand = new Command<Room>(async (room) => await ChangeTemperatureAsync(room, 1));
            DecreaseTempCommand = new Command<Room>(async (room) => await ChangeTemperatureAsync(room, -1));
            ToggleBlindsCommand = new Command<Room>(async (room) => await ToggleBlindsAsync(room));
            
            // House-wide commands (affect all rooms since they use shared controls)
            ToggleHouseAcCommand = new Command(async () => await ToggleHouseAcAsync());
            MoveBlindsUpCommand = new Command(async () => await MoveBlindsAsync("UP"));
            MoveBlindsDownCommand = new Command(async () => await MoveBlindsAsync("DOWN"));
            
            LogoutCommand = new Command(async () => await LogoutAsync());

            // Auto-refresh every 30 seconds
            StartAutoRefresh();
        }

        public ObservableCollection<Room> Rooms { get; }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
        }

        public string LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public bool IsAcOn
        {
            get => _isAcOn;
            set => SetProperty(ref _isAcOn, value);
        }

        public bool IsAcToggling
        {
            get => _isAcToggling;
            set => SetProperty(ref _isAcToggling, value);
        }

        public bool IsBlindsMoving
        {
            get => _isBlindsMoving;
            set => SetProperty(ref _isBlindsMoving, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleAcCommand { get; }
        public ICommand IncreaseTempCommand { get; }
        public ICommand DecreaseTempCommand { get; }
        public ICommand ToggleBlindsCommand { get; }
        public ICommand ToggleHouseAcCommand { get; }
        public ICommand MoveBlindsUpCommand { get; }
        public ICommand MoveBlindsDownCommand { get; }
        public ICommand LogoutCommand { get; }

        private void InitializePlaceholderRooms()
        {
            // Pre-populate with placeholder rooms so the UI shows loading state
            var placeholderRooms = new[]
            {
                new Room { Id = 1, Name = "Living Room", CurrentTemperature = double.NaN, TargetTemperature = double.NaN, BlindPosition = BlindPosition.Unknown, IsAcOn = false },
                new Room { Id = 2, Name = "Bedroom 1", CurrentTemperature = double.NaN, TargetTemperature = double.NaN, BlindPosition = BlindPosition.Unknown, IsAcOn = false },
                new Room { Id = 3, Name = "Bedroom 2", CurrentTemperature = double.NaN, TargetTemperature = double.NaN, BlindPosition = BlindPosition.Unknown, IsAcOn = false },
                new Room { Id = 4, Name = "Bedroom 3", CurrentTemperature = double.NaN, TargetTemperature = double.NaN, BlindPosition = BlindPosition.Unknown, IsAcOn = false }
            };

            foreach (var room in placeholderRooms)
            {
                Rooms.Add(room);
            }
        }

        public async Task InitializeAsync()
        {
            if (!_homeAutomationService.IsLoggedIn)
            {
                await Shell.Current.GoToAsync("login");
                return;
            }

            await RefreshDataAsync();
        }

        private async Task RefreshDataAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            IsRefreshing = true;

            try
            {
                var rooms = await _homeAutomationService.GetRoomsAsync();
                
                // Update existing rooms or add new ones
                foreach (var room in rooms)
                {
                    var existingRoom = Rooms.FirstOrDefault(r => r.Id == room.Id);
                    if (existingRoom != null)
                    {
                        // Update existing room data
                        existingRoom.CurrentTemperature = room.CurrentTemperature;
                        existingRoom.TargetTemperature = room.TargetTemperature;
                        existingRoom.IsAcOn = room.IsAcOn;
                        existingRoom.BlindPosition = room.BlindPosition;
                        existingRoom.LastUpdated = room.LastUpdated;
                    }
                    else
                    {
                        Rooms.Add(room);
                    }
                }
                
                // Update house-wide A/C status (same for all rooms)
                if (rooms.Count > 0)
                {
                    IsAcOn = rooms[0].IsAcOn;
                }
                
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("Error", $"Failed to refresh data: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        private async Task ToggleAcAsync(Room room)
        {
            if (room == null)
                return;

            try
            {
                var success = await _homeAutomationService.SetAirConditioningAsync(room.Id, !room.IsAcOn);
                if (success)
                {
                    room.IsAcOn = !room.IsAcOn;
                    await Task.Delay(1000); // Wait a bit then refresh
                    await RefreshDataAsync();
                }
                else
                {
                    await Application.Current?.MainPage?.DisplayAlert("Error", "Failed to toggle air conditioning", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task ChangeTemperatureAsync(Room room, int delta)
        {
            if (room == null)
                return;

            try
            {
                var newTemp = room.TargetTemperature + delta;
                
                // Reasonable temperature bounds
                if (newTemp < 16 || newTemp > 30)
                    return;

                var success = await _homeAutomationService.SetTargetTemperatureAsync(room.Id, newTemp);
                if (success)
                {
                    room.TargetTemperature = newTemp;
                    await Task.Delay(1000);
                    await RefreshDataAsync();
                }
                else
                {
                    await Application.Current?.MainPage?.DisplayAlert("Error", "Failed to change temperature", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task ToggleBlindsAsync(Room room)
        {
            if (room == null)
                return;

            try
            {
                var newPosition = room.BlindPosition switch
                {
                    BlindPosition.Up => BlindPosition.Down,
                    BlindPosition.Down => BlindPosition.Up,
                    _ => BlindPosition.Up
                };

                var success = await _homeAutomationService.SetBlindPositionAsync(room.Id, newPosition);
                if (success)
                {
                    room.BlindPosition = newPosition;
                    await Task.Delay(1000);
                    await RefreshDataAsync();
                }
                else
                {
                    await Application.Current?.MainPage?.DisplayAlert("Error", "Failed to control blinds", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                await _homeAutomationService.LogoutAsync();
                await Shell.Current.GoToAsync("login");
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void StartAutoRefresh()
        {
            Device.StartTimer(TimeSpan.FromSeconds(30), () =>
            {
                if (_homeAutomationService.IsLoggedIn && !IsBusy)
                {
                    Task.Run(async () => await RefreshDataAsync());
                }
                return true; // Continue the timer
            });
        }

        private async Task ToggleHouseAcAsync()
        {
            try
            {
                if (Rooms.Count == 0) return;
                
                // Get the current A/C state
                var currentState = IsAcOn;
                var newState = !currentState;
                
                // Show confirmation dialog
                var confirmed = await Application.Current?.MainPage?.DisplayAlert(
                    "Confirm A/C Control",
                    $"Do you want to turn the house air conditioning {(newState ? "ON" : "OFF")}?",
                    "Yes",
                    "No");
                
                if (confirmed != true)
                    return;
                
                // Set loading state
                IsAcToggling = true;
                
                // Send A/C command
                var success = await _homeAutomationService.SetAirConditioningAsync(Rooms[0].Id, newState);
                
                if (success)
                {
                    // Update all rooms to reflect the new A/C state
                    foreach (var room in Rooms)
                    {
                        room.IsAcOn = newState;
                    }
                    // Update house-wide A/C status
                    IsAcOn = newState;
                }
                else
                {
                    await Application.Current?.MainPage?.DisplayAlert("Error", "Failed to toggle house air conditioning", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // Clear loading state
                IsAcToggling = false;
            }
        }

        private async Task MoveBlindsAsync(string direction)
        {
            try
            {
                // Show confirmation dialog
                var confirmed = await Application.Current?.MainPage?.DisplayAlert(
                    "Confirm Blinds Control",
                    $"Do you want to move the house blinds {direction}?",
                    "Yes",
                    "No");
                
                if (confirmed != true)
                    return;
                
                // Set loading state
                IsBlindsMoving = true;
                
                // Send the blinds command using the new house-wide blinds control method
                var success = await _homeAutomationService.SetHouseBlindsAsync(direction);
                
                if (success)
                {
                    // Since we don't get status feedback, we can estimate the position
                    // but this is just for UI purposes
                    var estimatedPosition = direction == "UP" ? BlindPosition.Up : BlindPosition.Down;
                    
                    // Update all rooms to reflect the estimated blinds state
                    foreach (var room in Rooms)
                    {
                        room.BlindPosition = estimatedPosition;
                    }
                    
                    await Application.Current?.MainPage?.DisplayAlert("Success", $"Blinds moved {direction} successfully", "OK");
                }
                else
                {
                    await Application.Current?.MainPage?.DisplayAlert("Error", $"Failed to move blinds {direction}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // Clear loading state
                IsBlindsMoving = false;
            }
        }
    }
}
