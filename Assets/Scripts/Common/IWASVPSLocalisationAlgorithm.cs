using WASVPS;

/// <summary>
/// Interface for VPS (Visual Positioning System) localization algorithms
/// </summary>
public interface IWASVPSLocalisationAlgorithm { 
    /// <summary>
    /// Starts the localization algorithm
    /// </summary>
    public void Run();
    
    /// <summary>
    /// Stops the localization algorithm
    /// </summary>
    public void Stop();
    
    /// <summary>
    /// Pauses the localization algorithm
    /// </summary>
    public void Pause();
    
    /// <summary>
    /// Resumes the paused localization algorithm
    /// </summary>
    public void Resume();
    
    /// <summary>
    /// Gets the current location state from the algorithm
    /// </summary>
    /// <returns>Current location state</returns>
    LocationState GetLocationRequest();

    /// <summary>
    /// Event triggered when localization occurs
    /// </summary>
    public event System.Action<LocationState> OnLocalisationHappend;
    
    /// <summary>
    /// Event triggered when an error occurs during localization
    /// </summary>
    public event System.Action<ErrorInfo> OnErrorHappend;
    
    /// <summary>
    /// Event triggered when the correct angle is detected
    /// </summary>
    public event System.Action<bool> OnCorrectAngle;
}
