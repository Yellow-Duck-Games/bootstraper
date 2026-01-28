# Bootstrapper

Unity infrastructure that ensures a services scene loads before any other scene. Services are automatically moved to DontDestroyOnLoad and accessible from any scene via `FindObjectOfType`.

## Installation

### Package Manager (Git URL)
1. Window → Package Manager
2. "+" → Add package from git URL
3. Paste: `https://github.com/yourname/unity-bootstrapper.git`

### Local
Copy the `com.yourname.bootstrapper` folder to your project's `Packages/` directory.

## Setup

1. Edit → Project Settings → Bootstrapper
2. **Services Scene** — select your services scene (create an empty scene if needed)
3. **Default Scene** — scene to load after services in build (usually MainMenu)

## Usage

### Basic Principle

1. Create a scene for services (e.g. `Services.unity`)
2. Add GameObjects with your services to it
3. Set this scene in Project Settings → Bootstrapper
4. When the game starts (Play or build), services automatically move to DontDestroyOnLoad

### Accessing Services

From any scene:
```csharp
var audio = FindObjectOfType<AudioService>();
audio.PlayMusic(clip);
```

### Service Generator

Project Settings → Bootstrapper includes a generator for common services:

**Core (ready to use):**
- AudioService — music and sound effects
- SceneService — scene loading with progress events
- SaveService — save/load via PlayerPrefs + JSON
- SettingsService — game settings (volume, resolution)

**Core (stubs):**
- InputService — input (implement for Input System or Legacy)
- UIService — UI screen management

**Player (stubs):**
- PlayerProgressService — player progress
- InventoryService — inventory
- CurrencyService — currencies

**Online (stubs):**
- AuthService — authentication
- NetworkService — network requests
- LeaderboardService — leaderboards
- AnalyticsService — analytics
- AdsService — ads
- IAPService — in-app purchases

### Generator Workflow

1. Check the services you need
2. Click "Generate" — scripts and GameObjects are created on the Services scene
3. Green label "✓ Exists" — service is ready
4. Yellow label "● Script only" — script exists but GameObject not created

### Deleting Services

1. Uncheck the unwanted service
2. Click "Delete" — removes script and GameObject

## How It Works

**In Editor:**
- When pressing Play, the current scene is remembered
- Services scene opens automatically
- After initialization, loads the scene from which Play was pressed

**In Build:**
- Services must be first in Build Settings (index 0)
- After initialization, loads Default Scene

## Example Project Structure
```
Assets/
  Scenes/
    Services.unity      ← services (first in Build Settings)
    MainMenu.unity      ← Default Scene
    Gameplay.unity
  Scripts/
    Services/
      AudioService.cs
      SaveService.cs
      ...
```

## Requirements

- Unity 2021.3+