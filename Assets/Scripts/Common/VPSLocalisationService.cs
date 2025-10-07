using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace WASVPS
{
    /// <summary>
    /// Main VPS (Visual Positioning System) service that handles localization using camera and GPS data.
    /// Provides both real VPS functionality and free flight simulation mode for testing.
    /// </summary>
    public class VPSLocalisationService : MonoBehaviour
    {
        private const string DefaultUrl = "https://was-vps.web-ar.xyz";

        [Tooltip("Start VPS in OnAwake")]
        public bool StartOnAwake;

        [Header("Providers")]
        [Tooltip("Which camera, GPS and tracking use for runtime")]
        public ServiceProvider RuntimeProvider;
        [Tooltip("Which camera, GPS and tracking use for mock data")]
        public ServiceProvider MockProvider;
        private ServiceProvider _provider;

        private SettingsWASVPS _currentSettings;

        [Tooltip("Use mock provider when VPS service has started")]
        public bool UseMock = false;
        [Tooltip("Always use mock provider in Editor, even if UseMock is false")]
        public bool ForceMockInEditor = true;

        [SerializeField]
        private KeyCode toggleFreeFlightMode = KeyCode.Tab;

        [Header("Default VPS Settings")]
        [Tooltip("Enable GPS data transmission to VPS server")]
        public bool SendGPS;
        [Tooltip("Localization fails count to reset VPS session")]
        public int FailsCountToReset = 5;

        [Header("Location Settings")]
        [Tooltip("Array of location IDs for VPS localization")]
        public string[] locationsIds;
        
        [Header("API Settings")]
        [Tooltip("API key for VPS server authentication")]
        public string apiKey;

        [Header("Debug")]
        [Tooltip("Save images in request localy before sending them to server")]
        [SerializeField]
        private bool saveImagesLocaly;
        [SerializeField]
        private bool saveLogsInFile;

        private FreeFlightSimulationAlgorithm _freeFlightSimulationAlgorithm;

        /// <summary>
        /// Event triggered when a localization error occurs during VPS processing
        /// </summary>
        public event Action<ErrorInfo> OnErrorHappend;

        /// <summary>
        /// Event triggered when successful localization is achieved and position is updated
        /// </summary>
        public event Action<LocationState> OnPositionUpdated;

        /// <summary>
        /// Event triggered when the device angle changes from correct to incorrect orientation or vice versa
        /// </summary>
        public event Action<bool> OnCorrectAngle;

        /// <summary>
        /// Event triggered when VPS service state changes
        /// </summary>
        public event Action<WASVPSState, WASVPSState> OnStateChanged;

        private bool _isDefaultAlgorithm = true;
        private IWASVPSLocalisationAlgorithm _algorithm;
        private WASVPSState _currentState = WASVPSState.NotInitialized;

        /// <summary>
        /// Initializes the VPS service on start, validates configuration and optionally starts VPS automatically
        /// </summary>
        /// <returns>Coroutine enumerator for Unity's coroutine system</returns>
        private IEnumerator Start()
        {
            if (!ValidateConfiguration())
                yield break;

            SetState(WASVPSState.Ready);

            if (StartOnAwake)
                StartVPS();
        }

        /// <summary>
        /// Handles application pause events and resets tracking when resuming in editor
        /// </summary>
        /// <param name="pause">True if application is pausing, false if resuming</param>
        private void OnApplicationPause(bool pause)
        {
            if (pause || !Application.isEditor)
                return;

            ResetTracking();
        }

        /// <summary>
        /// Starts the VPS service using default settings based on configured location IDs and fail count
        /// </summary>
        public void StartVPS()
        {
            SettingsWASVPS defaultSettings = new SettingsWASVPS(locationsIds, FailsCountToReset);
            defaultSettings.ApiKey = apiKey;
            StartVPS(defaultSettings);
        }

        /// <summary>
        /// Starts the VPS service with custom settings and initializes GPS and session management
        /// </summary>
        /// <param name="settings">Custom VPS settings including location IDs and fail count threshold</param>
        public void StartVPS(SettingsWASVPS settings)
        {
            VPSLogger.Log(LogLevel.DEBUG, "StartVPS called");
            
            if (!ValidateSettings(settings))
            {
                SetState(WASVPSState.Error);
                return;
            }

            if (_provider == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Provider is null, cannot start VPS");
                SetState(WASVPSState.Error);
                return;
            }

            StopVps();
            _provider.InitGps(SendGPS);
            _provider.ResetSessionId();

            _currentSettings = settings;

            SwitchLocalizationAlgorithm(_isDefaultAlgorithm);

            if (_algorithm == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Failed to initialize VPS algorithm");
                SetState(WASVPSState.Error);
                return;
            }

            _algorithm.Run();
            SetState(WASVPSState.Running);
        }

        /// <summary>
        /// Configures event listeners for the specified localization algorithm
        /// </summary>
        /// <param name="localisationAlgorithm">The algorithm to configure listeners for</param>
        private void ConfigureAlgorithmListeners(IWASVPSLocalisationAlgorithm localisationAlgorithm)
        {
            if (localisationAlgorithm == null)
            {
                VPSLogger.Log(LogLevel.DEBUG, "Cannot configure listeners for null algorithm");
                return;
            }

            localisationAlgorithm.OnErrorHappend += (e) => 
            {
                VPSLogger.Log(LogLevel.ERROR, $"Localization error: {e?.Message ?? "Unknown error"}");
                OnErrorHappend?.Invoke(e);
            };
            
            localisationAlgorithm.OnLocalisationHappend += (ls) => 
            {
                VPSLogger.Log(LogLevel.DEBUG, "Localization successful");
                OnPositionUpdated?.Invoke(ls);
            };
            
            localisationAlgorithm.OnCorrectAngle += (correct) => 
            {
                OnCorrectAngle?.Invoke(correct);
            };
        }

        /// <summary>
        /// Switches between default VPS algorithm and free flight simulation algorithm
        /// </summary>
        /// <param name="isDefault">True to use default VPS algorithm, false for free flight simulation</param>
        private void SwitchLocalizationAlgorithm(bool isDefault)
        {
            VPSLogger.Log(LogLevel.DEBUG, $"SwitchLocalizationAlgorithm called: isDefault={isDefault}, _isDefaultAlgorithm={_isDefaultAlgorithm}");
            
            if (_isDefaultAlgorithm == isDefault)
            {
                VPSLogger.Log(LogLevel.DEBUG, $"Algorithm already set to {(isDefault ? "default VPS" : "free flight simulation")}");
                // Ensure _algorithm is not null even if already set
                if (_algorithm == null)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Algorithm is null despite being already set. Reinitializing...");
                    if (isDefault)
                    {
                        if (_currentSettings != null)
                        {
                            VPSLogger.Log(LogLevel.DEBUG, "Set WASVPSLocalisationAlgorithm");
                            try
                            {
                                _algorithm = new WASVPSLocalisationAlgorithm(DefaultUrl, this, _provider, _currentSettings, SendGPS);
                                ConfigureAlgorithmListeners(_algorithm);
                            }
                            catch (Exception ex)
                            {
                                VPSLogger.Log(LogLevel.ERROR, ex.Message);
                            }
                            
                        }
                        else
                        {
                            VPSLogger.Log(LogLevel.ERROR, "Cannot reinitialize: settings are null");
                        }
                    }
                    else
                    {
                        if (_freeFlightSimulationAlgorithm != null)
                        {
                            _freeFlightSimulationAlgorithm.SetSettings(_currentSettings);
                            _algorithm = _freeFlightSimulationAlgorithm;
                        }
                        else
                        {
                            VPSLogger.Log(LogLevel.ERROR, "Cannot reinitialize: free flight algorithm not found");
                        }
                    }
                }
                return;
            }

            _isDefaultAlgorithm = isDefault;
            VPSLogger.Log(LogLevel.DEBUG, $"Switching to {(isDefault ? "default VPS" : "free flight simulation")} algorithm");

            StopAllCoroutines();
            _freeFlightSimulationAlgorithm?.gameObject.SetActive(!_isDefaultAlgorithm);
            
            if (_isDefaultAlgorithm)
            {
                if (_currentSettings == null)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Cannot create VPS algorithm: settings are null");
                    SetState(WASVPSState.Error);
                    _algorithm = null;
                    return;
                }

                VPSLogger.Log(LogLevel.DEBUG, "Creating new WASVPSLocalisationAlgorithm");
                try
                {
                    _algorithm = new WASVPSLocalisationAlgorithm(DefaultUrl, this, _provider, _currentSettings, SendGPS);
                    ConfigureAlgorithmListeners(_algorithm);
                    VPSLogger.Log(LogLevel.DEBUG, "WASVPSLocalisationAlgorithm created successfully");
                }
                catch (System.Exception ex)
                {
                    VPSLogger.Log(LogLevel.ERROR, $"Failed to create WASVPSLocalisationAlgorithm: {ex.Message}");
                    _algorithm = null;
                    SetState(WASVPSState.Error);
                    return;
                }
            }
            else
            {
                if (_freeFlightSimulationAlgorithm == null)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Cannot switch to free flight simulation: algorithm not found");
                    SetState(WASVPSState.Error);
                    _algorithm = null;
                    return;
                }

                VPSLogger.Log(LogLevel.DEBUG, "Setting up free flight simulation algorithm");
                _freeFlightSimulationAlgorithm.SetSettings(_currentSettings);
                _algorithm = _freeFlightSimulationAlgorithm;
                VPSLogger.Log(LogLevel.DEBUG, "Free flight simulation algorithm set successfully");
            }
            
            VPSLogger.Log(LogLevel.DEBUG, $"Algorithm switch completed. _algorithm is {( _algorithm != null ? "not null" : "null")}");
        }

        /// <summary>
        /// Stops the currently running VPS service and terminates the localization algorithm
        /// </summary>
        public void StopVps()
        {
            _algorithm?.Stop();
            SetState(WASVPSState.Stopped);
        }

        /// <summary>
        /// Pauses the VPS service temporarily while preserving the current session state
        /// </summary>
        public void PauseVPS()
        {
            if (_currentState == WASVPSState.Running)
            {
                _algorithm?.Pause();
                SetState(WASVPSState.Paused);
            }
        }

        /// <summary>
        /// Resumes the paused VPS service and continues with the existing session
        /// </summary>
        public void ResumeVPS()
        {
            if (_currentState == WASVPSState.Paused)
            {
                _algorithm?.Resume();
                SetState(WASVPSState.Running);
            }
        }

        /// <summary>
        /// Retrieves the most recent localization result from the VPS algorithm
        /// </summary>
        /// <returns>Latest location state or null if VPS service is not running</returns>
        public LocationState GetLatestPose()
        {
            if (_algorithm == null)
            {
                VPSLogger.Log(LogLevel.DEBUG, "VPS algorithm is not initialized. Use StartVPS before");
                return null;
            }
            
            return _algorithm.GetLocationRequest();
        }

        /// <summary>
        /// Checks if at least one successful localization has been achieved in the current session
        /// </summary>
        /// <returns>True if localized, false otherwise</returns>
        public bool IsLocalized()
        {
            return _provider?.GetTracking()?.IsLocalized() ?? false;
        }

        /// <summary>
        /// Resets the current tracking state and AR Foundation tracking system
        /// </summary>
        public void ResetTracking()
        {
            if (!_provider)
                return;

            _provider.GetARFoundationApplyer()?.ResetTracking();
            _provider.GetTracking().ResetTracking();
            VPSLogger.Log(LogLevel.NONE, "Tracking reset");
        }

        /// <summary>
        /// Gets information about the current VPS session
        /// </summary>
        /// <returns>Current session information or null if no active session</returns>
        public SessionInfo GetCurrentSessionInfo()
        {
            return _provider?.GetSessionInfo();
        }

        /// <summary>
        /// Gets the total number of localization responses
        /// </summary>
        /// <returns>Total responses count or 0 if no session</returns>
        public int GetTotalResponsesCount()
        {
            return _provider?.GetTotalResponsesCount() ?? 0;
        }

        /// <summary>
        /// Gets the number of successful localizations
        /// </summary>
        /// <returns>Success count or 0 if no session</returns>
        public int GetSuccessLocalizationCount()
        {
            return _provider?.GetSuccessLocalizationCount() ?? 0;
        }

        /// <summary>
        /// Gets the number of failed localizations
        /// </summary>
        /// <returns>Fail count or 0 if no session</returns>
        public int GetFailLocalizationCount()
        {
            return _provider?.GetFailLocalizationCount() ?? 0;
        }

        /// <summary>
        /// Gets the number of successful localizations in a row
        /// </summary>
        /// <returns>Success in row count or 0 if no session</returns>
        public int GetSuccessLocalizationInRow()
        {
            return _provider?.GetSuccessLocalizationInRow() ?? 0;
        }

        /// <summary>
        /// Gets the success rate as a percentage
        /// </summary>
        /// <returns>Success rate percentage or 0 if no responses</returns>
        public float GetSuccessRate()
        {
            return _provider?.GetSuccessRate() ?? 0f;
        }

        /// <summary>
        /// Initializes the VPS service on awake, configures debug settings and selects appropriate provider
        /// </summary>
        private void Awake()
        {
            DebugUtils.SaveImagesLocaly = saveImagesLocaly;
            VPSLogger.WriteLogsInFile = saveLogsInFile;

            _freeFlightSimulationAlgorithm = FindObjectOfType<FreeFlightSimulationAlgorithm>(true);
            if (_freeFlightSimulationAlgorithm == null)
            {
                Debug.Log("FreeFlightSimulationAlgorithm not found. FreeFlightMode is not available");
            }
            ConfigureAlgorithmListeners(_freeFlightSimulationAlgorithm);

            // check what provider should VPS use
            var isMockMode = UseMock || Application.isEditor && ForceMockInEditor;
            _provider = isMockMode ? MockProvider : RuntimeProvider;

            if (!_provider)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't load provider! Select provider for VPS service!");
                return;
            }

            var providers = GetComponentsInChildren<ServiceProvider>();
            foreach (var provider in providers)
            {
                provider.gameObject.SetActive(false);
            }
            _provider.gameObject.SetActive(true);
        }

        /// <summary>
        /// Handles input for toggling between VPS and free flight simulation modes
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(toggleFreeFlightMode))
            {
                SwitchLocalizationAlgorithm(!_isDefaultAlgorithm);
            }
        }

        /// <summary>
        /// Gets the current state of the VPS service
        /// </summary>
        /// <returns>Current VPS state</returns>
        public WASVPSState GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// Checks if the VPS service is ready to start
        /// </summary>
        /// <returns>True if ready, false otherwise</returns>
        public bool IsReady()
        {
            return _currentState == WASVPSState.Ready || _currentState == WASVPSState.Stopped;
        }

        /// <summary>
        /// Checks if the VPS service is currently running
        /// </summary>
        /// <returns>True if running, false otherwise</returns>
        public bool IsRunning()
        {
            return _currentState == WASVPSState.Running;
        }

        /// <summary>
        /// Validates the basic configuration of the VPS service
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        private bool ValidateConfiguration()
        {
            if (!_provider)
            {
                VPSLogger.Log(LogLevel.ERROR, "VPS Provider is not assigned");
                return false;
            }

            if (locationsIds == null || locationsIds.Length == 0)
            {
                VPSLogger.Log(LogLevel.ERROR, "LocationsIds array is null or empty. It must have at least one value");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the VPS settings before starting the service
        /// </summary>
        /// <param name="settings">Settings to validate</param>
        /// <returns>True if settings are valid, false otherwise</returns>
        private bool ValidateSettings(SettingsWASVPS settings)
        {
            if (settings == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "VPS Settings cannot be null");
                return false;
            }

            if (settings.LocationIds == null || settings.LocationIds.Length == 0)
            {
                VPSLogger.Log(LogLevel.ERROR, "Settings LocationIds array is null or empty");
                return false;
            }

            if (settings.FailsCountToResetSession < 1)
            {
                VPSLogger.Log(LogLevel.ERROR, "FailsCountToResetSession must be at least 1");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the VPS service state and triggers state change event
        /// </summary>
        /// <param name="newState">New state to set</param>
        private void SetState(WASVPSState newState)
        {
            if (_currentState != newState)
            {
                WASVPSState previousState = _currentState;
                _currentState = newState;
                OnStateChanged?.Invoke(previousState, newState);
                VPSLogger.Log(LogLevel.DEBUG, $"VPS State changed from {previousState} to {newState}");
            }
        }
    }
}