# Essentials

Essentials is a Space Engineers dedicated server plugin for Magnetar.

## Prerequisites

- [Space Engineers Dedicated Server](https://store.steampowered.com/app/298740/Space_Engineers_Dedicated_Server/)
- [Magnetar](https://magnetar.se) - the Space Engineers server with plugin support
- [.NET Framework 4.8.1 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481)
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Python 3.12 or newer, for `setup.py`

## Setup

1. Clone the repository.
2. Run `setup.py` to rename the template artifacts and auto-detect local reference paths.
3. If auto-detection fails, set `Magnetar` and `Dedicated64` in `Directory.Build.props`.
4. Build `Essentials.sln` in `Release`.
5. Deploy the server plugin through Magnetar.

## Project Layout

- `ServerPlugin` contains the Magnetar plugin entry point and deployment scripts.
- `Shared` contains common plugin code, configuration, logging, and Harmony patches.
- `Essentials.xml` is the MagnetarHub plugin registration template.

## Configuration

Server configuration lives in `Shared/Config` and uses Magnetar `PluginSdk`.
The `PluginConfig` defaults are applied by the dedicated server plugin.

## Compatibility

Use the `EnsureCode` attribute on Harmony patch methods to safely skip loading the plugin when patched game code changes after a Space Engineers update.
The logged hash can be copied back into the attribute after validating a new game version.
Set `SE_PLUGIN_DISABLE_METHOD_VERIFICATION` to any value on the server host to disable method verification.

## Publishing

Register server plugins in [MagnetarHub](https://github.com/viktor-ferenczi/MagnetarHub), so they become available in Magnetar.
