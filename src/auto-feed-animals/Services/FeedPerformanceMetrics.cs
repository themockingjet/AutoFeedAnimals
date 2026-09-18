using BepInEx.Logging;

namespace AutoFeedAnimals
{
    internal sealed class FeedPerformanceMetrics
    {
        private const float ReportIntervalSeconds = 30f;
        private float _nextReport = -1f;

        private long _searchAttempts;
        private long _candidateContainers;
        private long _nearbyRefreshes;
        private long _inventorySnapshots;
        private long _pathChecks;
        private long _failedSearches;
        private long _successfulFeeds;
        private long _contentChangeChecks;
        private long _relevantContentChanges;
        private long _foodHintRefreshes;
        private long _foodHintSkips;

        internal void RecordSearchAttempt()
        {
            _searchAttempts++;
        }

        internal void RecordCandidateContainer()
        {
            _candidateContainers++;
        }

        internal void RecordNearbyRefresh()
        {
            _nearbyRefreshes++;
        }

        internal void RecordInventorySnapshot()
        {
            _inventorySnapshots++;
        }

        internal void RecordPathCheck()
        {
            _pathChecks++;
        }

        internal void RecordFailedSearch()
        {
            _failedSearches++;
        }

        internal void RecordSuccessfulFeed()
        {
            _successfulFeeds++;
        }

        internal void RecordContentChangeCheck()
        {
            _contentChangeChecks++;
        }

        internal void RecordRelevantContentChange()
        {
            _relevantContentChanges++;
        }

        internal void RecordFoodHintRefresh()
        {
            _foodHintRefreshes++;
        }

        internal void RecordFoodHintSkip()
        {
            _foodHintSkips++;
        }

        internal void ReportIfDue(float now, ManualLogSource logger)
        {
            if (_nextReport < 0f)
            {
                _nextReport = now + ReportIntervalSeconds;
                return;
            }

            if (now < _nextReport)
            {
                return;
            }

            logger.LogDebug(
                $"Chest feeding metrics ({ReportIntervalSeconds:0}s): " +
                $"searches={_searchAttempts}, failed={_failedSearches}, feeds={_successfulFeeds}, " +
                $"nearbyRefreshes={_nearbyRefreshes}, candidates={_candidateContainers}, " +
                $"inventorySnapshots={_inventorySnapshots}, pathChecks={_pathChecks}, " +
                $"contentChecks={_contentChangeChecks}, relevantChanges={_relevantContentChanges}, " +
                $"hintRefreshes={_foodHintRefreshes}, hintSkips={_foodHintSkips}.");
            _nextReport = now + ReportIntervalSeconds;
            _searchAttempts = 0;
            _candidateContainers = 0;
            _nearbyRefreshes = 0;
            _inventorySnapshots = 0;
            _pathChecks = 0;
            _failedSearches = 0;
            _successfulFeeds = 0;
            _contentChangeChecks = 0;
            _relevantContentChanges = 0;
            _foodHintRefreshes = 0;
            _foodHintSkips = 0;
        }
    }
}
