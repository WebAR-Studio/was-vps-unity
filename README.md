# WASVPS SDK (Unity3D)

This is **WASVPS** (Web AR Studio Visual Positioning System) SDK for Unity3D engine. Main features are:
- High-precision global user position localization for your AR apps
- Easy to use public API and prefabs
- Supports Android and iOS target platforms
- Integration with ARFoundation (ARCore and ARKit)
- Comprehensive logging and debugging tools

## Requirements

- Unity 2022.3+
- ARKit or ARCore supported device
- Git LFS for large assets

## Installation

### Option 1: Clone Repository
```bash
git clone https://github.com/WebAR-Studio/was-vps-unity.git
cd was-vps-unity
```

**Note:** Requires installed [Git-LFS](https://git-lfs.github.com) for large assets.

### Option 2: Unity Package Manager
Add this repository to your Unity project via Package Manager:
```
https://github.com/WebAR-Studio/was-vps-unity.git?path=/Assets
```

## Quick Start

1. **Open the project** in Unity 2022.3+
2. **Load the example scene**: `Assets/Scenes/TestScene.unity`
3. **Configure your API key** in the VPS component:
   - Select the VPS GameObject
   - In the Inspector, find "API Settings"
   - Enter your VPS API key
4. **Run the scene** in Editor or build for your device

## Examples

The SDK includes example scenes and code:

- **TestScene**: Basic VPS setup with UI controls
- **StageScene**: Production-ready scene setup
- **ExampleVPS.cs**: Code example showing how to integrate VPS in your scripts

### Example Usage

```csharp
using WASVPS;

public class MyVPSController : MonoBehaviour
{
    public VPSLocalisationService vpsService;
    
    void Start()
    {
        // Subscribe to VPS events
        vpsService.OnPositionUpdated += OnPositionUpdated;
        vpsService.OnErrorHappend += OnErrorOccurred;
        
        // Create custom settings
        SettingsWASVPS settings = new SettingsWASVPS(
            new string[] { "your-location-id" }, 
            5  // fails count to reset
        );
        
        settings.ApiKey = "your-api-key-here";
        settings.LocalizationTimeout = 2.0f;
        
        // Start VPS
        vpsService.StartVPS(settings);
    }
    
    private void OnPositionUpdated(LocationState locationState)
    {
        Debug.Log($"Localized! Position: {locationState.Localisation.VpsPosition}");
    }
    
    private void OnErrorOccurred(ErrorInfo error)
    {
        Debug.LogError($"VPS Error: {error.LogDescription()}");
    }
}
```

## Features

### 🎯 **Visual Positioning System**
- Real-time camera-based localization
- GPS integration for outdoor positioning
- Multiple location support
- Session management with automatic reset

### 🔧 **Developer Tools**
- **Free Flight Mode**: Test VPS without real camera (Editor only)
- **Mock Provider**: Use static images for testing
- **Debug Logging**: Comprehensive request/response logging
- **Image Saving**: Save sent images locally for debugging

### 📊 **Monitoring & Analytics**
- Success/failure rate tracking
- Response time metrics
- Detailed error reporting

## Configuration

### VPS Settings

Configure VPS behavior through the `VPSLocalisationService` component:

| Property | Description | Default |
|----------|-------------|---------|
| **Start On Awake** | Auto-start VPS when scene loads | false |
| **Use Mock** | Use mock provider for testing | false |
| **Force Mock in Editor** | Always use mock in Unity Editor | true |
| **Send GPS** | Include GPS data in requests | false |
| **Fails Count To Reset** | Failed attempts before session reset | 5 |
| **Location Ids** | Target location identifiers | [] |
| **Api Key** | VPS server authentication key | "" |
| **Save Images Locally** | Save sent images for debugging | false |
| **Save Logs In File** | Write logs to file | false |

### Features
- Camera movement simulation
- Localization testing
- Pose visualization
- Customizable movement speed and sensitivity

## Debugging

### Logging
The SDK provides comprehensive logging:

```csharp
// Enable verbose logging
VPSLogger.SetLogLevel(LogLevel.VERBOSE);

// Logs include:
// - Request URLs and headers
// - JSON metadata being sent
// - Response bodies and headers
// - Error details
```

## Troubleshooting

### Build Issues
- Ensure ARFoundation packages are installed
- Check platform-specific settings
- Verify API key is set correctly

---

**Made with ❤️ for AR developers**
