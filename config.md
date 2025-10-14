# Barrier Control System Configuration Guide

This document provides a comprehensive reference for all configuration options in the Barrier Control System. Configurations are loaded from `appsettings.json` and mapped to C# classes defined in `Config.cs`. The app uses Microsoft.Extensions.Configuration for JSON deserialization.

The root configuration is an `AppConfig` object containing nested sections for barriers, number plate APIs, credentials, and app behavior.

## Configuration Structure

```json
{
  "Barriers": {
    "Count": 3,
    "Barriers": {
      "Barrier1": {
        /* BarrierConfig */
      },
      "Barrier2": {
        /* BarrierConfig */
      }
    }
  },
  "NumberPlatesApiUrl": "...",
  "NumberPlatesCronExpression": "0 0 * * * ?",
  "WhitelistCredentials": [
    /* list of WhitelistCredential */
  ],
  "SendInitialPulse": false,
  "SkipInitialCronPulse": true,
  "PulseTriggerMode": "db",
  "PerformInitialApiStatusCheck": false,
  "DebugLogging": false,
  "AutostartNumberPlates": true,
  "StartOpenOnLaunch": true,
  "ScreenMode": "Dark",
  "DatabaseInitMode": "keep",
  "DebugMode": false
}
```

## Top-Level App Settings (`AppConfig`)

These settings control global application behavior, not tied to specific barriers.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Barriers` | `BarriersConfig` | `{}` | Configuration for barrier hardware, scheduling, and validation (see below). |
| `NumberPlatesApiUrl` | `string` | `""` | Base URL template for fetching authorized number plates. Uses `{id}` placeholder for whitelist IDs. E.g., `"http://localhost:5291/api/v1/{id}/activeplates"`. Appends ID to target whitelists. Required for plate validation. |
| `NumberPlatesCronExpression` | `string` | `"0 0 * * * ?"` | Quartz cron expression for periodic whitelist refresh (daily at midnight). Format: sec min hour day month dayofweek. |
| `WhitelistCredentials` | `List<WhitelistCredential>` | `[]` | Array of credentials for basic auth on number plate APIs. Each has `Id`, `Username`, `Password`. Example:<br>`[{"Id": "WL001", "Username": "admin", "Password": "pass"}]` |
| `SendInitialPulse` | `bool` | `true` | Whether to sequentially pulse all enabled barriers on app startup (skipped if `SkipInitialCronPulse` is true). Useful for testing but defaults to false to avoid accidental actuation. |
| `SkipInitialCronPulse` | `bool` | `false` | If true, skip the first cron execution for each barrier (avoids startup pulses if cron triggers soon after launch). |
| `PulseTriggerMode` | `string` | `"db"` | Mode for barrier pulsing: `"db"` (cron polls unsent DB transactions) or `"camera"` (immediate pulse on API receive, with validation). |
| `PerformInitialApiStatusCheck` | `bool` | `true` | Check barrier hardware API status via GET on startup and display in UI (green/red indicators). |
| `DebugLogging` | `bool` | `false` | Enable verbose debug logging (e.g., full API responses, retries). May impact performance; use for troubleshooting. |
| `AutostartNumberPlates` | `bool` | `true` | Automatically fetch number plate whitelists on app start (reduces errors on first camera detection). |
| `StartOpenOnLaunch` | `bool` | `false` | Show main window open on app launch instead of minimized to tray (defaults to background operation). |
| `ScreenMode` | `string` | `"System"` | UI theme: `"Light"`, `"Dark"`, or `"System"` (follows OS). |
| `DatabaseInitMode` | `string` | `"keep"` | DB table handling on startup: `"keep"` (preserve existing transactions) or `"recreate"` (drop and reinitialize). |
| `DebugMode` | `bool` | `false` | Enable debug mode: skips actual HTTP requests to barrier APIs, logging `[DEBUG]` instead. Useful for testing without hardware actuation. |
| `NoRelayCalls` | `bool` | `false` | Disable relay calls: same as `DebugMode` for pulsing, but only affects the relay HTTP requests (other processing continues normally). |

**Notes:**

- Boolean defaults in JSON are often omitted or explicit (e.g., `"DebugMode": false`).
- Crons use Quartz.NET format: `*` (any), `?` (no specificity), ranges like `0-59`.
- `PulseTriggerMode` affects flow: "db" validates/processes in cron; "camera" validates immediately but marks sent.
- `DebugMode` simulates pulse success to continue logic (validation, logging) without relay issues.

## Barriers Configuration (`BarriersConfig`)

Group for managing multiple barrier points.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Count` | `int` | - | Number of barriers (must match the number of keys in `Barriers`). Used for validation; does not affect logic (barrier names drive). |
| `Barriers` | `Dictionary<string, BarrierConfig>` | `{}` | Keyed by barrier name (e.g., `Barrier1`), each a `BarrierConfig` object (see below). Names must be unique and descriptive. |

**Notes:**

- Current JSON defines 3 barriers (`Barrier1`, `Barrier2`, `Barrier3`).
- Dynamic addition/removal requires code restart; barriers are created on startup from this config.

## Per-Barrier Configuration (`BarrierConfig`)

Each barrier (e.g., lane/gate) has settings below. Applies to all barriers in `Barriers`.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `CronExpression` | `string` | `""` | Quartz cron for polling unsent DB transactions in "db" mode. E.g., `"*/5 * * * * ?"` (every 5 seconds). Disabled if `IsEnabled` is false. |
| `ApiUrl` | `string` | `""` | HTTP endpoint for barrier pulsing (POST to trigger open). E.g., `"http://192.168.1.2/status.xml?pl1=1"`. Must support empty-body POST; used for status GETs. |
| `LaneId` | `int` | `0` | Unique ID (1, 2, 3, ...) for DB matching (`lane_id` in `transactions`). Cameras/separate apps insert with matching IDs; cron only processes matching lanes. |
| `Name` | `string` | `""` | Human-readable name for the barrier (e.g., "Main Entrance"). Displayed in logs and UI. |
| `Direction` | `int` | `0` | Default direction for camera transactions when JSON payload lacks `LogicalDirection`: `0` (outbound/always validate), `1` (inbound/validate plates). |
| `CameraSerial` | `string` | `""` | Serial identifier for the camera linked to this barrier (e.g., "CAM001"). Incoming camera data is routed to matching barrier. |
| `ApiDownBehavior` | `string` | `"UseHistoric"` | Fallback when number plate API fails: `"UseHistoric"` (use cached plates), `"DontOpen"` (block all inbound), `"OpenAny"` (allow all inbound). |
| `IsEnabled` | `bool` | `true` | Whether this barrier participates in cron scheduling (and startup pulses if enabled). Disabled barriers are created but skipped in jobs. |

**Notes:**

- Cron: Buildings on top-level `NumberPlatesCronExpression` for plates fetch; per-barrier for transaction polling.
- `ApiDownBehavior`: Only affects inbound (`direction=1`) validations; outbound (`direction=0`) always pulses. Logic replaces cached plates with "open all" or "block all" on API failure.
- Lane IDs: Ensure uniqueness across barriers; mismatch means transactions ignored by cron.

## Other Classes (Used Internally)

Referenced in `AppConfig` but defined in `Config.cs`.

- **`WhitelistCredential`**: Holds per-whitelist basic auth. Properties: `Id` (unique ID like "WL001"), `Username`, `Password` (strings).
- **`Config`**: Old/service config (e.g., `ApiUsername` for camera API auth, `DatabasePath`). May be legacy; focus on `AppConfig`.

## Full Example `appsettings.json`

```json
{
  "Barriers": {
    "Count": 3,
    "Barriers": {
      "Barrier1": {
        "CronExpression": "*/5 * * * * ?",
        "ApiUrl": "http://192.168.1.100/status.xml?pl1=1",
        "LaneId": 1,
        "ApiDownBehavior": "UseHistoric",
        "IsEnabled": true
      },
      "Barrier2": {
        "CronExpression": "*/10 * * * * ?",
        "ApiUrl": "http://192.168.1.101/status.xml?pl2=1",
        "LaneId": 2,
        "ApiDownBehavior": "DontOpen",
        "IsEnabled": true
      },
      "Barrier3": {
        "CronExpression": "*/7 * * * * ?",
        "ApiUrl": "http://192.168.1.102/status.xml?pl3=1",
        "LaneId": 3,
        "ApiDownBehavior": "OpenAny",
        "IsEnabled": false
      }
    }
  },
  "NumberPlatesApiUrl": "https://secure-api.example.com/api/v1/{id}/plates",
  "NumberPlatesCronExpression": "0 15 * * * ?",
  "WhitelistCredentials": [
    {
      "Id": "WL1",
      "Username": "user1",
      "Password": "secret1"
    },
    {
      "Id": "WL2",
      "Username": "user2",
      "Password": "secret2"
    }
  ],
  "SendInitialPulse": false,
  "SkipInitialCronPulse": true,
  "PulseTriggerMode": "db",
  "PerformInitialApiStatusCheck": true,
  "DebugLogging": false,
  "AutostartNumberPlates": true,
  "StartOpenOnLaunch": false,
  "ScreenMode": "System",
  "DatabaseInitMode": "keep",
  "DebugMode": false
}
```

## Validation and Pitfalls

- **Required Fields**: `Barriers.Barriers.{Name}.CronExpression`, `ApiUrl`, `LaneId` must be set; empty strings cause errors.
- **URLs**: Use HTTPS for security; test accessibility.
- **Creds**: Store securely (env vars in prod); avoid plain text.
- **Crons**: Test expressions (e.g., at https://crontab.guru for Quartz format).
- **Motion**: Changing `PulseTriggerMode` or `DebugMode` requires restart.
- **Testing**: Use `DebugMode: true` for dry-run testing without hardware.
- **Runtime**: Config loaded on start; follow README for testing (e.g., `./test-camera.ps1`).

For code references, see `Config.cs` classes and how `MainWindow.axaml.cs` deserializes into `AppConfig`.

---

_Last updated based on code analysis. Check `README.md` for hardware details and troubleshooting._
