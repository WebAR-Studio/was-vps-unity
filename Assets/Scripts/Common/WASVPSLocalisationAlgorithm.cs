using System.Collections;
using WASVPS.JSON;
using UnityEngine;

namespace WASVPS
{
    /// <summary>
    /// Internal management VPS
    /// </summary>
    public class WASVPSLocalisationAlgorithm : IWASVPSLocalisationAlgorithm
    {
        private readonly VPSLocalisationService _localisationService;
        private readonly ServiceProvider _provider;
        private LocationState _locationState;
        private readonly SettingsWASVPS _settings;

        private readonly IWASVPSRequest _requestVps = new UnityWebRequestVPS();

        /// <summary>
        /// Event localisation error
        /// </summary>
        public event System.Action<ErrorInfo> OnErrorHappend;

        /// <summary>
        /// Event localisation success
        /// </summary>
        public event System.Action<LocationState> OnLocalisationHappend;

        /// <summary>
        /// Event of change angle from correct to incorrect and back
        /// </summary>
        public event System.Action<bool> OnCorrectAngle;

        private readonly float _neuronTime;

        private int _currentFailsCount;
        private bool _isLocalization;
        private bool _isCorrectAngle = true;
        private bool _isPaused;

        #region Metrics

        private int _attemptCount;
        private const string FullLocalizationStopWatch = "FullLocalizationStopWatch";
        private const string TotalWaitingTime = "TotalWaitingTime";

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vps_service">Parent GameObject, for start coroutine</param>
        /// <param name="vpsServise"></param>
        /// <param name="vpsProvider">Provider to get camera, gps and tracking</param>
        /// <param name="vpsSettings">Settings</param>
        /// <param name="url"></param>
        /// <param name="sendGps"></param>
        public WASVPSLocalisationAlgorithm(string url, VPSLocalisationService vpsServise, ServiceProvider vpsProvider, SettingsWASVPS vpsSettings, bool sendGps)
        {
            _requestVps.SetUrl(url);
            _requestVps.SetApiKey(vpsSettings.ApiKey);
            _localisationService = vpsServise;

            _provider = vpsProvider;
            
            var failsToReset1 = vpsSettings.FailsCountToResetSession;

            var gps = _provider.GetGps();
            if (gps != null)
                gps.SetEnable(sendGps);

            _settings = vpsSettings;

            _locationState = new LocationState();

            _neuronTime = 0;

            _currentFailsCount = 0;
            _isLocalization = true;
            _isPaused = false;

            OnErrorHappend += (error) => ResetIfFails(failsToReset1);
        }

        public void Run()
        {
            _localisationService.StartCoroutine(LocalisationRoutine());
        }

        public void Stop()
        {
            _localisationService.StopAllCoroutines();
            ARFoundationCamera.semaphore.Free();
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Get latest available Location state (updated in LocalisationRoutine())
        /// </summary>
        /// <returns></returns>
        public LocationState GetLocationRequest()
        {
            return _locationState;
        }

        /// <summary>
        /// Main cycle. Check readiness every service, send request, apply the resulting localization if success
        /// </summary>
        /// <returns>The routine.</returns>
        private IEnumerator LocalisationRoutine()
        {
//#if !UNITY_EDITOR
//            bool isARInitialized = false;
//            yield return _localisationService.StartCoroutine(ARAvailabilityChecking.StartChecking((status) => isARInitialized = status));

//            if (!isARInitialized)
//            {
//                ErrorInfo error = new ErrorInfo(ErrorCode.AR_NOT_SUPPORTED, "AR is not supported on current device");
//                OnErrorHappend?.Invoke(error);
//                VPSLogger.Log(LogLevel.ERROR, error.LogDescription());
//                yield break;
//            }
//#endif

            _attemptCount = 0;
            MetricsCollector.Instance.StartStopwatch(FullLocalizationStopWatch);

            Texture2D image;
            string meta;

            var camera = _provider.GetCamera();
            if (camera == null)
            {
                var error = new ErrorInfo(ErrorCode.NO_CAMERA, "Camera is not available");
                OnErrorHappend?.Invoke(error);
                VPSLogger.Log(LogLevel.ERROR, error.LogDescription());
                yield break;
            }

            camera.Init(new WASVPSTextureRequirement[] { _provider.GetTextureRequirement() });

            var tracking = _provider.GetTracking();
            if (tracking == null)
            {
                var error = new ErrorInfo(ErrorCode.TRACKING_NOT_AVALIABLE, "Tracking is not available");
                OnErrorHappend?.Invoke(error);
                VPSLogger.Log(LogLevel.ERROR, error.LogDescription());
                yield break;
            }

            var arRFoundationApplyer = _provider.GetARFoundationApplyer();

            while (true)
            {
                while (_isPaused)
                    yield return null;

                while (!camera.IsCameraReady())
                    yield return null;

                MetricsCollector.Instance.StartStopwatch(TotalWaitingTime);

                do
                {
                    if (_isCorrectAngle != CheckTakePhotoConditions(tracking.GetLocalTracking().Rotation.eulerAngles, _settings))
                    {
                        _isCorrectAngle = !_isCorrectAngle;
                        OnCorrectAngle?.Invoke(_isCorrectAngle);
                    }
                    if (!_isCorrectAngle)
                        yield return null;
                } while (!_isCorrectAngle);

                MetricsCollector.Instance.StopStopwatch(TotalWaitingTime);
                VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", TotalWaitingTime, MetricsCollector.Instance.GetStopwatchSecondsAsString(TotalWaitingTime));

                var metaMsg = DataCollector.CollectData(_provider, _settings.LocationIds);
                meta = DataCollector.Serialize(metaMsg);

                image = camera.GetFrame(_provider.GetTextureRequirement());

                if (DebugUtils.SaveImagesLocaly)
                {
                    VPSLogger.Log(LogLevel.VERBOSE, "Saving image before sending...");
                    DebugUtils.SaveDebugImage(image);
                    DebugUtils.SaveJson(metaMsg);
                }

                VPSLogger.Log(LogLevel.DEBUG, "Sending VPS Request...");
                _localisationService.StartCoroutine(_requestVps.SendVpsRequest(image, meta, () => ReceiveResponce(tracking, arRFoundationApplyer)));

                if (_isLocalization)
                    yield return new WaitForSeconds(_settings.LocalizationTimeout - _neuronTime); 
                else
                    yield return new WaitForSeconds(_settings.CalibrationTimeout - _neuronTime);
            }
        }

        private void ReceiveResponce(IWASVPSTracking tracking, ARFoundationApplyer arRFoundationApplyer)
        {
            if (_isPaused)
                return;

            _attemptCount++;

            _locationState = _requestVps.GetLocationState(); 

            if (_locationState.Status == LocalisationStatus.VPS_READY)
            {
#region Metrics
                if (!tracking.IsLocalized())
                {
                    MetricsCollector.Instance.StopStopwatch(FullLocalizationStopWatch);

                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", FullLocalizationStopWatch, MetricsCollector.Instance.GetStopwatchSecondsAsString(FullLocalizationStopWatch));
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] SerialAttemptCount {0}", _attemptCount);
                }

#endregion

                bool changeLocationId = tracking.Localize(_locationState.Localisation.LocationId);
                if (changeLocationId)
                {
                    _provider.ResetSessionId();
                }

                _locationState.Localisation = arRFoundationApplyer?.ApplyVpsTransform(_locationState.Localisation, _isLocalization);

                _isLocalization = false;
                _currentFailsCount = 0;

                // Record successful localization in session statistics
                _provider.GetSessionInfo()?.SuccessLocalization();

                OnLocalisationHappend?.Invoke(_locationState);
                VPSLogger.Log(LogLevel.NONE, "VPS localization successful");
                
                // Log session statistics
                var session = _provider.GetSessionInfo();
                if (session != null)
                {
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "Session Stats - Total: {0}, Success: {1}, Fails: {2}, Success in row: {3}, Success rate: {4:F1}%", 
                        session.ResponsesCount, session.SuccessLocalizationCount, session.FailLocalizationCount, 
                        session.SuccessLocalizationInRow, (float)session.SuccessLocalizationCount / session.ResponsesCount * 100f);
                }
            }
            else
            {
                // Record failed localization in session statistics
                _provider.GetSessionInfo()?.FailLocalization();

                OnErrorHappend?.Invoke(_locationState.Error);
                VPSLogger.LogFormat(LogLevel.NONE, "VPS Request Failed: {0}", _locationState.Error.LogDescription());
                
                // Log session statistics
                var session = _provider.GetSessionInfo();
                if (session != null)
                {
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "Session Stats - Total: {0}, Success: {1}, Fails: {2}, Success in row: {3}, Success rate: {4:F1}%", 
                        session.ResponsesCount, session.SuccessLocalizationCount, session.FailLocalizationCount, 
                        session.SuccessLocalizationInRow, session.ResponsesCount > 0 ? (float)session.SuccessLocalizationCount / session.ResponsesCount * 100f : 0f);
                }
            }
        }

        private void ResetIfFails(int countToReset)
        {
            if (_isLocalization)
                return;

            _currentFailsCount++;
            if (_currentFailsCount < countToReset) return;
            
            _currentFailsCount = 0;
            _provider.ResetSessionId();
            _isLocalization = true;
        }

        private bool CheckTakePhotoConditions(Vector3 curAngle, SettingsWASVPS settings)
        {
            return (curAngle.x < settings.MaxAngleX || curAngle.x > 360 - settings.MaxAngleX) &&
            (curAngle.z < settings.MaxAngleZ || curAngle.z > 360 - settings.MaxAngleZ);
        }
    }
}