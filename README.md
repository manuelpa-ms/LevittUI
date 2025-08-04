# LevittUI - Home Automation MAUI App

A cross-platform .NET MAUI application that replaces the old home automation web UI by providing modern mobile and desktop interfaces for controlling smart home devices.

## Features

- 🌡️ **Temperature Monitoring**: View current temperature for all rooms
- 🎯 **Target Temperature Control**: Set and adjust target temperatures 
- ❄️ **Air Conditioning Control**: Turn A/C on/off for each room
- 🪟 **Blinds Control**: Control blinds position (up/down/partial)
- 📱 **Cross-Platform**: Works on Android, iOS, Windows, and macOS
- 🔄 **Auto-Refresh**: Automatically updates data every 30 seconds
- 🔐 **Secure Login**: Authentication with the existing home automation system

## Architecture

The app is built using the MVVM (Model-View-ViewModel) pattern and follows modern .NET MAUI best practices.

### Project Structure

```
LevittUI/
├── Models/              # Data models for home automation
├── Services/            # HTTP communication with the automation system
├── ViewModels/          # MVVM ViewModels with data binding
├── Views/               # XAML pages and UI
├── Converters/          # Value converters for UI binding
└── Resources/           # Images, fonts, and styles
```

### Key Components

- **HomeAutomationService**: Handles communication with the existing web API
- **Room Model**: Represents a room with temperature, A/C, and blind controls
- **LoginPage**: Authentication interface
- **MainPage**: Dashboard showing all rooms and controls

## API Integration

Based on reverse engineering the existing web UI at `http://192.168.1.130/main.app`, the app communicates using:

### Authentication
- Endpoint: `/main.app` 
- Credentials: Username: "Usuario", Password: "1111"
- Session management with SessionId parameter

### Data Polling
- Service: `getDp` (Get Data Point)
- URL Pattern: `/ajax.app?SessionId={sessionId}&service=getDp&plantItemId={id}&_={timestamp}`
- Returns JSON with device status and values

### Device Control
- HTTP POST to `/ajax.app` with device commands
- Parameters: sessionId, plantItemId, value

### Plant Item IDs (from Fiddler trace analysis)
Based on the captured network traffic, room controls map to these IDs:

**Living Room**
- Temperature Sensor: 1391
- Target Temperature: 775  
- A/C Control: 1398
- Blind Control: 816

**Bedroom 1**
- Temperature Sensor: 1405
- Target Temperature: 857
- A/C Control: 1412  
- Blind Control: 898

**Bedroom 2**
- Temperature Sensor: 1419
- Target Temperature: 940
- A/C Control: 954
- Blind Control: 1170

## Getting Started

### Prerequisites
- .NET 8.0 or later
- Visual Studio 2022 or Visual Studio Code with .NET MAUI extension
- For mobile development: Android SDK and/or Xcode

### Running the App

1. **Clone and build**:
   ```bash
   git clone <repository-url>
   cd LevittUI
   dotnet build
   ```

2. **Run on Windows**:
   ```bash
   dotnet run --framework net9.0-windows10.0.19041.0
   ```

3. **Run on Android**:
   ```bash
   dotnet run --framework net9.0-android
   ```

4. **Run on iOS** (macOS only):
   ```bash
   dotnet run --framework net9.0-ios
   ```

## Configuration

⚠️ **Security Notice**: This project does not contain any hardcoded IP addresses, usernames, or passwords. You must configure your home automation system details before using the app.

### Configuration Methods

The app supports three ways to configure your home automation system connection:

#### Method 1: Environment Variables (Recommended for Development)

Set these environment variables:

**Windows (PowerShell):**
```powershell
$env:LEVITT_SERVER_ADDRESS = "http://192.168.1.100"
$env:LEVITT_USERNAME = "admin"
$env:LEVITT_PASSWORD = "password"
```

**Windows (Command Prompt):**
```cmd
set LEVITT_SERVER_ADDRESS=http://192.168.1.100
set LEVITT_USERNAME=admin
set LEVITT_PASSWORD=password
```

**macOS/Linux:**
```bash
export LEVITT_SERVER_ADDRESS="http://192.168.1.100"
export LEVITT_USERNAME="admin"
export LEVITT_PASSWORD="password"
```

#### Method 2: Local Configuration File

Create a `config.txt` file in the platform-specific application data directory:

**Windows:**
```
%LOCALAPPDATA%\LevittUI\config.txt
```

**Android:**
- File is automatically placed in app's internal storage
- Path: `/data/data/[your.package.name]/files/config.txt`

**iOS:**
- File is automatically placed in app's Documents directory
- Accessible through the Files app

**File format for all platforms:**
```
ServerAddress=http://192.168.1.100
Username=admin
Password=password
```

#### Method 3: Runtime Configuration

If no configuration is found, the app will prompt you to enter:
- Server address (e.g., `http://192.168.1.100`)
- Username
- Password

These settings will be saved securely to the local configuration file for future use.

### Configuration Priority

The app checks configuration sources in this order:
1. Environment variables (highest priority)
2. Local configuration file
3. Runtime user input (lowest priority)

### Security Best Practices

- **Never commit configuration files** to version control
- Use environment variables for development
- On mobile devices, configuration is stored in app-specific secure storage
- Consider using a secure password manager for credential storage

## Usage

1. **Login**: Enter credentials on the login screen
2. **View Status**: See current temperature and device states for all rooms
3. **Control A/C**: Tap the A/C button to toggle on/off
4. **Adjust Temperature**: Use +/- buttons to change target temperature
5. **Control Blinds**: Tap the blinds button to cycle through positions
6. **Refresh**: Pull down to refresh or use the refresh button

## Implementation Notes

### Mock Data
The current implementation includes mock data generation for testing when the actual home automation system is not available. This allows development and testing of the UI without requiring connection to the physical system.

### Error Handling
- Network timeouts are handled gracefully
- Login failures show appropriate error messages
- Device control failures display user-friendly alerts

### Performance
- Uses async/await for all network operations
- Implements proper disposal of HTTP resources
- Efficient data binding with ObservableCollection

## Next Steps

1. **Real API Integration**: Replace mock data with actual API calls once the real endpoints are determined
2. **Device Discovery**: Implement automatic discovery of rooms and device IDs
3. **Offline Mode**: Cache data for basic functionality when disconnected
4. **Push Notifications**: Add real-time alerts for system events
5. **Advanced Controls**: Add scheduling, scenes, and automation rules
6. **User Preferences**: Save favorite settings and customizations

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test on multiple platforms
5. Submit a pull request

## License

This project is licensed under the MIT License.
