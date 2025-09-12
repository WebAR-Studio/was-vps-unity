using UuidExtensions;

namespace WASVPS
{
    /// <summary>
    /// Tracks statistics and information about the current VPS (Visual Positioning System) session.
    /// This class maintains counters for localization attempts, successes, failures, and consecutive successes.
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Unique identifier for this VPS session
        /// </summary>
        public string Id;
        
        /// <summary>
        /// Total number of localization responses received (both successful and failed)
        /// </summary>
        public int ResponsesCount;
        
        /// <summary>
        /// Number of successful localizations in this session
        /// </summary>
        public int SuccessLocalizationCount;
        
        /// <summary>
        /// Number of failed localizations in this session
        /// </summary>
        public int FailLocalizationCount;
        
        /// <summary>
        /// Number of consecutive successful localizations (resets to 0 on failure)
        /// </summary>
        public int SuccessLocalizationInRow;

        /// <summary>
        /// Initializes a new VPS session with a unique ID and resets all counters to zero
        /// </summary>
        public SessionInfo()
        {
            Id = Uuid7.Guid().ToString();
            ResponsesCount = 0;
            SuccessLocalizationCount = 0;
            FailLocalizationCount = 0;
            SuccessLocalizationInRow = 0;
        }

        /// <summary>
        /// Records a successful localization attempt.
        /// Increments total responses, success count, and consecutive success count.
        /// </summary>
        public void SuccessLocalization()
        {
            ResponsesCount += 1;
            SuccessLocalizationCount += 1;
            SuccessLocalizationInRow += 1;
        }

        /// <summary>
        /// Records a failed localization attempt.
        /// Increments total responses and fail count, resets consecutive success count to 0.
        /// </summary>
        public void FailLocalization()
        {
            ResponsesCount += 1;
            FailLocalizationCount += 1;
            SuccessLocalizationInRow = 0;
        }
    }
}
