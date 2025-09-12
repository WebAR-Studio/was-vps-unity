using WASVPS;
using UnityEngine;

public class ExampleVPS : MonoBehaviour
{
	/// <summary>
    /// VPS API class to control VPS Service
    /// </summary>
    public VPSLocalisationService VPS;

    private void Start()
    {
		// Subscribe to success and error vps result
		VPS.OnPositionUpdated += OnPositionUpdatedHandler;
		VPS.OnErrorHappend += OnErrorHappendHandler;

		// Create custom settings to VPS: generate url in constructor and set delay between requests
		SettingsWASVPS settings = new SettingsWASVPS(new string[] { "mariel" }, 5);

		settings.LocalizationTimeout = 3;
		settings.ApiKey = "your-api-key-here"; // Set your VPS API key

		// Start service
		VPS.StartVPS(settings);
	}
	
	/// <summary>
    /// Request finished successfully
    /// </summary>
    /// <param name="locationState"></param> 
	private void OnPositionUpdatedHandler(LocationState locationState)
	{
		Debug.LogFormat("[Event] Localisation successful! Receive position {0} and rotation {1}", locationState.Localisation.VpsPosition, locationState.Localisation.VpsRotation);
		
		// Log session statistics
		Debug.LogFormat("[Stats] Total: {0}, Success: {1}, Fails: {2}, Success in row: {3}, Success rate: {4:F1}%", 
			VPS.GetTotalResponsesCount(), VPS.GetSuccessLocalizationCount(), VPS.GetFailLocalizationCount(), 
			VPS.GetSuccessLocalizationInRow(), VPS.GetSuccessRate());
	}

	/// <summary>
	/// Request finished with error
	/// </summary>
	/// <param name="errorCode"></param>
	private void OnErrorHappendHandler(ErrorInfo errorCode)
    {
		Debug.LogFormat("[Event] Localisation error: {0}", errorCode.LogDescription());
		
		// Log session statistics
		Debug.LogFormat("[Stats] Total: {0}, Success: {1}, Fails: {2}, Success in row: {3}, Success rate: {4:F1}%", 
			VPS.GetTotalResponsesCount(), VPS.GetSuccessLocalizationCount(), VPS.GetFailLocalizationCount(), 
			VPS.GetSuccessLocalizationInRow(), VPS.GetSuccessRate());
	}

	/// <summary>
    /// In the end stop vps service
    /// </summary>
	private void OnDestroy()
	{
		VPS.StopVps();
	}
}