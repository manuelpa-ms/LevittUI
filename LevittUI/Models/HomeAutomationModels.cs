using System.Text.Json.Serialization;
using LevittUI.ViewModels;

namespace LevittUI.Models
{
    // Authentication response model
    public class AuthResponse
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    // Data point response from getDp service
    public class DataPointResponse
    {
        [JsonPropertyName("plantItemId")]
        public int PlantItemId { get; set; }
        
        [JsonPropertyName("value")]
        public string? Value { get; set; }
        
        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    // Room model for UI
    public class Room : BaseViewModel
    {
        private double _currentTemperature = double.NaN;
        private double _targetTemperature = double.NaN;
        private bool _isAcOn;
        private BlindPosition _blindPosition = BlindPosition.Unknown;
        private DateTime _lastUpdated;

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public double CurrentTemperature
        {
            get => _currentTemperature;
            set => SetProperty(ref _currentTemperature, value);
        }
        
        public double TargetTemperature
        {
            get => _targetTemperature;
            set => SetProperty(ref _targetTemperature, value);
        }
        
        public bool IsAcOn
        {
            get => _isAcOn;
            set => SetProperty(ref _isAcOn, value);
        }
        
        public BlindPosition BlindPosition
        {
            get => _blindPosition;
            set => SetProperty(ref _blindPosition, value);
        }
        
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }
        
        // Plant item IDs for different sensors/controls
        public int TemperatureSensorId { get; set; }
        public int TargetTempSensorId { get; set; }
        public int AcControlId { get; set; }
        public int BlindControlId { get; set; }
    }

    public enum BlindPosition
    {
        Unknown = 0,
        Up = 1,
        Down = 2,
        Partial = 3
    }

    // Diagram page response
    public class DiagramPageResponse
    {
        [JsonPropertyName("items")]
        public List<DiagramItem> Items { get; set; } = new();
        
        [JsonPropertyName("backgroundImage")]
        public string BackgroundImage { get; set; } = string.Empty;
    }

    public class DiagramItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("x")]
        public double X { get; set; }
        
        [JsonPropertyName("y")]
        public double Y { get; set; }
        
        [JsonPropertyName("iconPath")]
        public string IconPath { get; set; } = string.Empty;
    }

    // Command request for controlling devices
    public class DeviceCommand
    {
        public int PlantItemId { get; set; }
        public string Command { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    // Alarm state response
    public class AlarmStateResponse
    {
        [JsonPropertyName("hasAlarms")]
        public bool HasAlarms { get; set; }
        
        [JsonPropertyName("alarmCount")]
        public int AlarmCount { get; set; }
    }
}
