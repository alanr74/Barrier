### Barrier Control

**URL**: Configurable per barrier

**Method**: POST

**Purpose**: Send pulse signal to hardware

## Testing

### Camera Integration Test

Run the camera integration test script:

```bash
./test-camera.ps1
```

This sends a camera detection payload to the `/api/camera` endpoint and logs the response.

### Testing Strategies

- **Manual Buttons**: Use UI barrier buttons to test individual pulse operations
- **Configurable Modes**: Test both `"db"` and `"camera"` pulse trigger modes
- **Error Scenarios**: Test with invalid URLs to verify retry and error logging
- **Multi-Whitelist**: Verify different auth credentials work per whitelist ID

## Architecture

The application follows SOLID principles with a clean layered architecture and dependency injection:

### Core Components

- **CameraController**: Handles camera API data ingestion and optional auto-pulse triggers
- **NumberPlateService**: Manages multi-auth whitelist fetching with per-ID credentials
- **BarrierViewModel**: Manages individual barriers with "Manual", "Cron", or "Camera" pulse sources
- **TransactionRepository**: Handles camera data storage with UTC→local time normalization

### Project Structure

```
├── Controllers/
│   └── CameraController.cs      # Camera API endpoint
├── Models/
│   ├── Transaction.cs           # Camera/barrier transaction data
│   └── CameraMessage.cs         # Camera detection payload
├── Services/
│   ├── NumberPlateService.cs    # Multi-auth number plate fetching
│   └── BarrierService.cs        # Hardware communication
├── ViewModels/
│   ├── MainWindowViewModel.cs   # App coordination
│   └── BarrierViewModel.cs      # Individual barrier management
└── appsettings.json             # Configuration with WhitelistCredentials
```

## Dependencies

- **Avalonia**: Cross-platform UI framework
- **ReactiveUI**: MVVM framework
- **Quartz.NET**: Job scheduling
- **Microsoft.Data.Sqlite**: Database operations
- **Microsoft.Extensions.Configuration**: Configuration management
- **Polly**: Retry and resilience policies

## Building and Running

1. Restore NuGet packages: `dotnet restore`
2. Build the project: `dotnet build`
3. Run the application: `dotnet run`
4. Configure `appsettings.json`
5. Test with: `./test-camera.ps1`

## State Management

Cline provides **Checkpoints** to save current project state (open files, configuration) for resuming sessions:

- **Save**: Use the "Checkpoint" button to save a named snapshot
- **Load**: Restore previous development state
- **Persistence**: Survives restarts/reinstalls

Checkpoints are recommended for preserving complex project states between sessions.
