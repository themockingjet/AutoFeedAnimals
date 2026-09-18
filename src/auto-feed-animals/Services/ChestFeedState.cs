using UnityEngine;
using System.Collections.Generic;

namespace AutoFeedAnimals
{
    internal sealed class ChestFeedState
    {
        internal Container? Container;
        internal ItemDrop? Food;
        internal float SearchTimer;
        internal float ContainerRefreshTimer;
        internal float RetryBackoff;
        internal int MembershipRevision = -1;
        internal int ContentRevision = -1;
        internal bool HasPathResult;
        internal bool PathResult;
        internal Vector3 PathTarget;
        internal float PathResultTimer;
        internal List<Container>? NearbyContainers;
        internal List<int>? NearbyContainerRevisions;
        internal Dictionary<string, ItemDrop>? FoodTemplates;

        internal void Clear()
        {
            Container = null;
            Food = null;
            HasPathResult = false;
            PathResultTimer = 0f;
        }

        internal void ResetForSettings(int membershipRevision, int contentRevision)
        {
            Clear();
            SearchTimer = 0f;
            ContainerRefreshTimer = 5f;
            RetryBackoff = 0f;
            MembershipRevision = membershipRevision;
            ContentRevision = contentRevision;
            NearbyContainers?.Clear();
            NearbyContainerRevisions?.Clear();
            FoodTemplates = null;
        }

        internal void SetPathResult(Vector3 target, bool result, float cacheDuration)
        {
            HasPathResult = true;
            PathResult = result;
            PathTarget = target;
            PathResultTimer = cacheDuration;
        }

        internal bool TryUsePathResult(Vector3 target, float dt, out bool result)
        {
            if (!HasPathResult || PathResultTimer <= dt || PathTarget != target)
            {
                result = false;
                return false;
            }

            PathResultTimer -= dt;
            result = PathResult;
            return true;
        }
    }
}
