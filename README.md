# Barrier Control System

## Recent Changes

- Migrated from .NET 9 preview to .NET 8 for stability
- Removed unit tests project to simplify the codebase
- Enhanced logging for invalid plate validation with specific reasons (not found, expired, not yet valid)

A C# Avalonia-based desktop application for managing automated barrier controls with number plate validation and scheduling.

## Overview

This application controls multiple barriers that open/close based on vehicle transactions. It integrates with external APIs for number plate data and uses scheduled jobs to automate barrier operations. The system supports different fallback behaviors when the number plate API is unavailable.

## Features

- **Multi-Barrier Management**: Control multiple barriers with individual configurations
- **Automated Scheduling**: Cron-based jobs for barrier operations and data fetching
- **Number Plate Validation**: Check incoming vehicles against authorized number plates with date/time ranges
- **API Integration**: Fetch number plate data from external APIs
- **Fallback Modes**: Configurable behavior when APIs are down
- **Real-time Monitoring**: Live logging and status indicators
- **Manual Controls**: UI buttons for manual operations

## Architecture

The application follows SOLID principles with a clean layered architecture and dependency injection:

### Layers

- **Presentation (ViewModels)**: MVVM view models handling UI logic and data binding
- **Application (Services)**: Business logic services with dependency injection
- **Domain (Models)**: Data models and domain entities
- **Infrastructure (Repositories)**: Data access abstractions

### SOLID Principles Implementation

#### **Single Responsibility**

- Each service handles one specific concern (logging, scheduling, number plates, barriers)
- Repositories focus solely on data access
- ViewModels manage UI state and coordinate services

#### **Open/Closed**

- Interfaces allow extension without modifying existing code
- Services can be swapped with different implementations

#### **Liskov Substitution**

- All implementations properly substitute their interfaces
- BarrierViewModel can work with any IBarrierService implementation

#### **Interface Segregation**

- Focused interfaces that clients actually need
- `INumberPlateService`, `IBarrierService`, `ISchedulingService`, `ILoggingService`, `ITransactionRepository`

#### **Dependency Inversion**

- High-level modules (ViewModels) depend on abstractions (interfaces)
- Low-level modules (Services) implement interfaces
- Dependencies injected through constructors

### Core Components

- **MainWindowViewModel**: Orchestrates application startup, configuration, and service coordination
- **BarrierViewModel**: Manages individual barrier state and operations
- **Services**: Business logic implementations with interfaces
- **Repositories**: Data access layer with SQLite
- **Models**: Domain entities (Transaction, NumberPlateEntry)

### Key Interfaces & Implementations

- `INumberPlateService` → `NumberPlateService`: Manages authorized plates, validation, API fallback logic
- `IBarrierService` → `BarrierService`: Handles barrier hardware communication
- `ISchedulingService` → `SchedulingService`: Quartz.NET job orchestration
- `ILoggingService` → `LoggingService`: Centralized logging with UI integration
- `ITransactionRepository` → `TransactionRepository`: Transaction data access

### Dependency Injection

Services are manually injected in `MainWindow.xaml.cs`. In production, use a DI container like:

- Microsoft.Extensions.DependencyInjection
- Autofac
- Ninject

### Project Structure

```
├── Models/                 # Domain entities
│   ├── Transaction.cs
│   └── NumberPlateEntry.cs
├── Repositories/           # Data access layer
│   ├── ITransactionRepository.cs
│   └── TransactionRepository.cs
├── Services/              # Business logic services
│   ├── ILoggingService.cs & LoggingService.cs
│   ├── INumberPlateService.cs & NumberPlateService.cs
│   ├── IBarrierService.cs & BarrierService.cs
│   └── ISchedulingService.cs & SchedulingService.cs
├── ViewModels/            # MVVM view models
│   ├── MainWindowViewModel.cs
│   └── BarrierViewModel.cs
├── Config.cs              # Configuration classes
└── appsettings.json       # Application settings
```

## Configuration

### appsettings.json Structure

```json
{
  "Barriers": {
    "Count": 3,
    "Barriers": {
      "Barrier1": {
        "CronExpression": "*/5 * * * * ?",
        "ApiUrl": "http://192.168.1.2/status.xml?pl1=1",
        "LaneId": 1
      }
    }
  },
  "NumberPlatesApiUrl": "https://api.example.com/numberplates",
  "NumberPlatesCronExpression": "0 0 * * * ?",
  "ApiDownBehavior": "UseHistoric"
}
```

### API Down Behaviors

- **UseHistoric**: Continue using previously fetched number plate data
- **DontOpen**: Clear number plate data, don't open barriers for any vehicles
- **OpenAny**: Allow all vehicles to pass (bypass validation)

## Operation Flow

### Automatic Mode (Cron Jobs)

1. **Barrier Pulse Jobs**: Run on configured cron schedules
2. **Number Plate Fetch Job**: Runs hourly by default
3. **Transaction Processing**:
   - Fetch next unprocessed transaction for the barrier's lane
   - Check if transaction is "In" direction (Direction = 1)
   - Validate number plate against authorized list and date/time range
   - Send pulse to barrier hardware if valid

### Manual Mode

- **Fetch Number Plates**: Button to manually trigger API data fetch
- **Send Pulse**: Individual barrier control buttons

### Flow Diagram

```mermaid
graph TD
    A[Start Cron Job for Barrier] --> B[Get Next Unprocessed Transaction]
    B --> C{Transaction Exists?}
    C -->|No| D[Log: No pending transactions, skip pulse]
    C -->|Yes| E[Update Last Processed Date]
    E --> F{Is Direction 'In' (1)?}
    F -->|No (Out)| G[Log: Out transaction, send pulse]
    F -->|Yes| H[Is AllowAnyPlate?]
    H -->|Yes| G
    H -->|No| I[Validate Plate: IsValidPlate?]
    I -->|Yes| G
    I -->|No| J[Get Validation Reason]
    J --> K[Log: Invalid plate with reason, skip pulse]
    G --> L[Send Pulse to Barrier API]
    L --> M{Success?}
    M -->|Yes| N[Log: Pulse sent successfully<br/>Set Indicator Green]
    M -->|No| O[Log: Pulse failed<br/>Set Indicator Red]
    N --> P[Mark Transaction as Sent]
    O --> P
    P --> Q[End Job]
    D --> Q
    K --> Q
```

## Database Schema

### Transactions Table

```sql
CREATE TABLE transactions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    created DATETIME NOT NULL,
    datetime DATETIME NOT NULL,
    ocr_plate TEXT NOT NULL,
    ocr_accuracy INTEGER NOT NULL,
    direction INTEGER NOT NULL,  -- 0=Out, 1=In
    lane_id INTEGER NOT NULL,
    camera_id INTEGER NOT NULL,
    image1 TEXT,
    image2 TEXT,
    image3 TEXT,
    sent INTEGER NOT NULL DEFAULT 0,
    sent_datetime DATETIME
);
```

## API Endpoints

### Number Plates API

**URL**: Configurable via `NumberPlatesApiUrl`

**Method**: GET

**Response**: JSON array of number plate entries

```json
[
  {
    "plate": "ABC123",
    "start": "2025-09-23T10:00:00",
    "finish": "2025-09-23T18:00:00"
  }
]
```

### Barrier Control API

**URL**: Configurable per barrier via `ApiUrl`

**Method**: POST

**Purpose**: Send pulse signal to physical barrier hardware

## Scheduling

Uses Quartz.NET for job scheduling with cron expressions:

- Barrier jobs: Individual cron schedules per barrier
- Number plate fetch: Hourly by default ("0 0 \* \* \* ?")

## UI Components

- **Barrier Controls**: Enable/disable toggles and manual pulse buttons
- **Number Plate Fetch**: Manual data refresh button
- **Log Console**: Real-time operation logging with auto-scroll
- **Status Indicators**: Visual feedback for barrier states

## Dependencies

- **Avalonia**: Cross-platform UI framework
- **ReactiveUI**: MVVM framework
- **Quartz.NET**: Job scheduling
- **Microsoft.Data.Sqlite**: Database operations
- **Microsoft.Extensions.Configuration**: Configuration management

## Building and Running

1. Restore NuGet packages
2. Build the project
3. Run the application
4. Configure `appsettings.json` as needed
5. The app will initialize the database and start scheduled jobs

## Sample Data

The application includes 25 sample transactions for testing:

- Mixed directions (In/Out)
- Various lanes and cameras
- Realistic timestamps and number plates

## Logging

All operations are logged to the UI console with timestamps:

- API fetch results
- Transaction processing
- Barrier operations
- Error conditions
- Detailed invalid plate reasons (e.g., "Plate 'ABC123' not found in authorized list")

## Error Handling

- API failures with configurable fallback modes
- Database connection issues
- Invalid configurations
- Network timeouts

## Security Considerations

- API URLs should use HTTPS in production
- Validate API responses for expected formats
- Consider authentication for API endpoints
- Secure database file access

## Future Enhancements

- User authentication and authorization
- Advanced reporting and analytics
- Integration with more camera systems
- Mobile companion app
- Cloud synchronization
