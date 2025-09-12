namespace WASVPS
{
    /// <summary>
    /// Represents the current state of the VPS service
    /// </summary>
    public enum WASVPSState
    {
        /// <summary>VPS service is not initialized</summary>
        NotInitialized,
        /// <summary>VPS service is ready but not running</summary>
        Ready,
        /// <summary>VPS service is currently running</summary>
        Running,
        /// <summary>VPS service is paused</summary>
        Paused,
        /// <summary>VPS service is stopped</summary>
        Stopped,
        /// <summary>VPS service encountered an error</summary>
        Error
    }
}