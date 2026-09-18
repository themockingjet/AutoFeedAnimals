using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoFeedAnimals
{
    internal sealed class FeedContainerRegistry
    {
        private const float FoodHintSafetyRefreshSeconds = 5f;

        private readonly AutoFeedAnimalsSettings _settings;
        private readonly FeedPerformanceMetrics _metrics;
        private readonly HashSet<Container> _containers = new HashSet<Container>();
        private readonly Dictionary<Container, int> _contentRevisions = new Dictionary<Container, int>();
        private readonly Dictionary<Container, ContainerFoodHint> _foodHints = new Dictionary<Container, ContainerFoodHint>();
        private bool _initialized;
        private float _nextRefresh;
        private int _membershipRevision;
        private int _contentRevision;

        internal FeedContainerRegistry(AutoFeedAnimalsSettings settings, FeedPerformanceMetrics metrics)
        {
            _settings = settings;
            _metrics = metrics;
        }

        internal int MembershipRevision => _membershipRevision;
        internal int ContentRevision => _contentRevision;

        internal void Register(Container container)
        {
            if (!IsPlayerContainer(container) || !_containers.Add(container))
            {
                return;
            }

            _membershipRevision++;
            _contentRevisions[container] = 0;
            _foodHints[container] = new ContainerFoodHint();
        }

        internal void NotifyChanged(Container container)
        {
            if (!IsPlayerContainer(container))
            {
                return;
            }

            if (_containers.Add(container))
            {
                _membershipRevision++;
                _contentRevisions[container] = 0;
                _foodHints[container] = new ContainerFoodHint();
            }

            _contentRevisions[container] = GetContentRevision(container) + 1;
            _contentRevision++;
            if (_foodHints.TryGetValue(container, out ContainerFoodHint? hint))
            {
                hint.Invalidate();
            }
        }

        internal void Unregister(Container container)
        {
            if (!_containers.Remove(container))
            {
                return;
            }

            _contentRevisions.Remove(container);
            _foodHints.Remove(container);
            _membershipRevision++;
        }

        internal bool Contains(Container container)
        {
            return _containers.Contains(container);
        }

        internal void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            Refresh();
        }

        internal void RefreshIfNeeded()
        {
            if (Time.time >= _nextRefresh)
            {
                Refresh();
            }
        }

        internal void FillNearby(Vector3 position, List<Container> nearby)
        {
            Initialize();
            RefreshIfNeeded();
            nearby.Clear();
            float rangeSquared = _settings.FeedRangeMeters * _settings.FeedRangeMeters;
            foreach (Container container in _containers)
            {
                if (container != null &&
                    (position - container.transform.position).sqrMagnitude <= rangeSquared)
                {
                    nearby.Add(container);
                }
            }
        }

        internal void CaptureContentRevisions(List<Container> nearby, List<int> revisions)
        {
            revisions.Clear();
            foreach (Container container in nearby)
            {
                revisions.Add(GetContentRevision(container));
            }
        }

        internal bool HasContentChanges(List<Container>? nearby, List<int>? revisions)
        {
            if (nearby == null || revisions == null || nearby.Count != revisions.Count)
            {
                return true;
            }

            for (int index = 0; index < nearby.Count; index++)
            {
                if (GetContentRevision(nearby[index]) != revisions[index])
                {
                    return true;
                }
            }

            return false;
        }

        internal bool MightContainFood(Container container, Dictionary<string, ItemDrop> foodTemplates)
        {
            if (foodTemplates.Count == 0 ||
                !_foodHints.TryGetValue(container, out ContainerFoodHint? hint))
            {
                return foodTemplates.Count > 0;
            }

            if (!hint.Initialized || Time.time >= hint.NextRefresh)
            {
                RefreshFoodHint(container, hint);
            }

            if (!hint.Initialized)
            {
                return true;
            }

            foreach (string itemName in hint.ItemNames)
            {
                if (foodTemplates.ContainsKey(itemName))
                {
                    return true;
                }
            }

            _metrics.RecordFoodHintSkip();
            return false;
        }

        internal bool HasAccess(Container container)
        {
            if (Game.instance == null)
            {
                return container.IsOwner();
            }

            return container.CheckAccess(Game.instance.GetPlayerProfile().GetPlayerID());
        }

        private void Refresh()
        {
            _nextRefresh = Time.time + 10f;
            foreach (Container container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
            {
                Register(container);
            }
        }

        private int GetContentRevision(Container container)
        {
            return _contentRevisions.TryGetValue(container, out int revision) ? revision : -1;
        }

        private void RefreshFoodHint(Container container, ContainerFoodHint hint)
        {
            Inventory? inventory = container.GetInventory();
            if (inventory == null)
            {
                hint.Initialized = false;
                return;
            }

            _metrics.RecordFoodHintRefresh();
            hint.ItemNames.Clear();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item != null && item.m_stack > 0 && item.m_shared != null)
                {
                    hint.ItemNames.Add(item.m_shared.m_name);
                }
            }

            hint.Initialized = true;
            hint.NextRefresh = Time.time + FoodHintSafetyRefreshSeconds;
        }

        private static bool IsPlayerContainer(Container? container)
        {
            if (container == null || container.GetInventory() == null ||
                !container.name.StartsWith("piece_", StringComparison.Ordinal))
            {
                return false;
            }

            ZNetView? nview = container.m_nview;
            return nview != null && nview.IsValid() && nview.GetZDO() != null;
        }

        private sealed class ContainerFoodHint
        {
            internal readonly HashSet<string> ItemNames = new HashSet<string>(StringComparer.Ordinal);
            internal bool Initialized;
            internal float NextRefresh;

            internal void Invalidate()
            {
                Initialized = false;
                NextRefresh = 0f;
                ItemNames.Clear();
            }
        }
    }
}
