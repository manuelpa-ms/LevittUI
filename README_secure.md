# LevittUI - Home Automation MAUI App

A cross-platform .NET MAUI application for controlling Siemens home automation systems.

## Features

- 🌡️ **Temperature Monitoring**: View current and target temperatures for multiple rooms
- ❄️ **Air Conditioning Control**: House-wide A/C on/off control with confirmation dialogs
- 🪟 **Blinds Control**: House-wide blinds up/down control with confirmation dialogs
- 📱 **Cross-Platform**: Runs on Android, iOS, Windows, and macOS
- 🔒 **Secure Configuration**: No hardcoded credentials in source code

## Configuration

The application requires configuration of your home automation server details. There are several ways to provide this configuration:

### Method 1: Environment Variables (Recommended for Development)

Set the following environment variables:

```bash
LEVITT_SERVER_ADDRESS=192.168.1.130
LEVITT_USERNAME=your_username
LEVITT_PASSWORD=your_password
```

### Method 2: Local Configuration File

The app will create a configuration file at:
- **Windows**: `%LOCALAPPDATA%\LevittUI\config.txt`
- **macOS/Linux**: `~/.local/share/LevittUI/config.txt`

The file format is:
```
ServerAddress=192.168.1.130
Username=your_username
Password=your_password
```

### Method 3: Enter Details in App

If no configuration is found, you can enter the server address, username, and password directly in the login screen. The app will save these for future use.

## Building the Project

1. **Prerequisites**:
   - .NET 9.0 SDK
   - Visual Studio 2022 or Visual Studio Code with C# extension
   - For mobile deployment: Android SDK and/or Xcode

2. **Clone and Build**:
   ```bash
   git clone <repository-url>
   cd LevittUI
   dotnet restore
   dotnet build
   ```

3. **Run on Windows**:
   ```bash
   dotnet run --framework net9.0-windows10.0.19041.0
   ```

## Security Notes

- **Never commit credentials**: All sensitive configuration is handled through environment variables or local config files
- **Local storage**: Credentials are stored locally on the device in the user's application data folder
- **HTTPS**: Consider using HTTPS for your home automation server if possible
- **Network security**: Ensure your home automation system is properly secured on your network

## Home Automation System Compatibility

This application is designed for Siemens home automation systems that expose a web interface with the following endpoints:
- `/main.app` - Main application interface
- `/dialog.app` - Control dialog interface
- `/getDp` - Data point retrieval

## Room Configuration

The application is pre-configured for a specific room layout. To modify for your setup, update the room mappings in `HomeAutomationService.cs`:

```csharp
private readonly Dictionary<int, Room> _roomMappings = new()
{
    // Add your room configurations here
};
```

## Usage

1. **Login**: Enter credentials on the login screen
2. **View Status**: See current temperature and device states for all rooms
3. **Control A/C**: Tap the A/C button to toggle on/off with confirmation
4. **Control Blinds**: Use UP/DOWN buttons to move blinds with confirmation
5. **Refresh**: Pull down to refresh or wait for auto-refresh (30 seconds)

## Implementation Details

### API Integration
Based on reverse engineering the web UI, the app uses:
- Form-based authentication at `/main.app`
- Three-step control process: GET wait → GET dialog → POST command
- Browser-style multipart form data for device commands
- JSON polling for sensor data via `/ajax.app` service

### Architecture
- **MVVM Pattern**: Clean separation of concerns
- **Dependency Injection**: Services registered in MauiProgram.cs
- **Observable Collections**: Real-time UI updates
- **Value Converters**: Type-safe data binding

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes (ensure no credentials are hardcoded)
4. Submit a pull request

## License

[Add your license information here]

## Support

For issues and questions, please use the GitHub issue tracker.
