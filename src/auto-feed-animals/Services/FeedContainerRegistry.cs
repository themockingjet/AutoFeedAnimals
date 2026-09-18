using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoFeedAnimals
{
    internal sealed class FeedContainerRegistry
    {
        private readonly AutoFeedAnimalsSettings _settings;
        private readonly HashSet<Container> _containers = new HashSet<Container>();
        private bool _initialized;
        private float _nextRefresh;

        internal FeedContainerRegistry(AutoFeedAnimalsSettings settings)
        {
            _settings = settings;
        }

        internal void Register(Container container)
        {
            if (IsPlayerContainer(container))
            {
                _containers.Add(container);
            }
        }

        internal void Unregister(Container container)
        {
            _containers.Remove(container);
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

        internal List<Container> FindNearby(Vector3 position)
        {
            Initialize();
            RefreshIfNeeded();
            List<Container> nearby = new List<Container>();
            float rangeSquared = _settings.FeedRangeMeters * _settings.FeedRangeMeters;
            foreach (Container container in _containers)
            {
                if (container != null &&
                    (position - container.transform.position).sqrMagnitude <= rangeSquared)
                {
                    nearby.Add(container);
                }
            }

            return nearby;
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
    }
}
