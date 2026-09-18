using BepInEx.Logging;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AutoFeedAnimals
{
    internal sealed class AnimalFeedService
    {
        private const float InitialRetryBackoffSeconds = 0.5f;
        private const float MaxRetryBackoffSeconds = 5f;
        private const float PathResultCacheSeconds = 0.25f;

        private readonly AutoFeedAnimalsSettings _settings;
        private readonly FeedContainerRegistry _registry;
        private readonly ManualLogSource _logger;
        private readonly FeedPerformanceMetrics _metrics;
        private readonly ConditionalWeakTable<MonsterAI, ChestFeedState> _feedStates = new ConditionalWeakTable<MonsterAI, ChestFeedState>();

        internal AnimalFeedService(
            AutoFeedAnimalsSettings settings,
            FeedContainerRegistry registry,
            ManualLogSource logger,
            FeedPerformanceMetrics metrics)
        {
            _settings = settings;
            _registry = registry;
            _logger = logger;
            _metrics = metrics;
        }

        internal void ClearChestTargets()
        {
            foreach (MonsterAI monsterAi in UnityEngine.Object.FindObjectsByType<MonsterAI>(FindObjectsSortMode.None))
            {
                if (_feedStates.TryGetValue(monsterAi, out ChestFeedState state))
                {
                    state.ResetForSettings(_registry.MembershipRevision, _registry.ContentRevision);
                }
            }
        }

        internal bool UpdateChestFeeding(MonsterAI monsterAi, Humanoid humanoid, float dt, ref bool result)
        {
            if (!CanUseChestFeeding(monsterAi))
            {
                _feedStates.Remove(monsterAi);
                return false;
            }

            ChestFeedState state = _feedStates.GetOrCreateValue(monsterAi);
            if (state.Container == null || state.Food == null)
            {
                return false;
            }

            Container container = state.Container;
            Vector3 target = container.transform.position;
            if (!IsWithinFeedRange(monsterAi.transform.position, target))
            {
                state.Clear();
                result = false;
                return false;
            }

            if (!_settings.IgnorePathing)
            {
                bool hasPath;
                if (!state.TryUsePathResult(target, dt, out hasPath))
                {
                    hasPath = HasPathTo(monsterAi, target);
                    state.SetPathResult(target, hasPath, PathResultCacheSeconds);
                }

                if (!hasPath)
                {
                    state.Clear();
                    result = false;
                    return true;
                }

                bool reached = monsterAi.MoveTo(dt, target, monsterAi.m_consumeRange, false);
                monsterAi.LookAt(target);
                if (!reached &&
                    ((monsterAi.transform.position - target).sqrMagnitude >
                         monsterAi.m_consumeRange * monsterAi.m_consumeRange ||
                     !monsterAi.IsLookingAt(target, 35f, false)))
                {
                    result = true;
                    return true;
                }

                monsterAi.StopMoving();
                if (!HasPathTo(monsterAi, target))
                {
                    state.Clear();
                    result = false;
                    return true;
                }
            }

            Inventory inventory = container.GetInventory();
            if (inventory == null || !_registry.HasAccess(container))
            {
                state.Clear();
                result = false;
                return true;
            }

            ItemDrop foodTemplate = state.Food;
            ItemDrop.ItemData? item = null;
            _metrics.RecordInventorySnapshot();
            foreach (ItemDrop.ItemData candidate in inventory.GetAllItems())
            {
                if (candidate != null && candidate.m_stack > 0 && candidate.m_shared != null &&
                    foodTemplate.m_itemData != null && foodTemplate.m_itemData.m_shared != null &&
                    candidate.m_shared.m_name == foodTemplate.m_itemData.m_shared.m_name &&
                    !_settings.Filter.IsDisallowedFood(candidate, foodTemplate) &&
                    monsterAi.CanConsume(candidate))
                {
                    item = candidate;
                    break;
                }
            }

            if (item == null || !inventory.RemoveOneItem(item))
            {
                state.Clear();
                result = false;
                return true;
            }

            _metrics.RecordSuccessfulFeed();
            monsterAi.m_onConsumedItem?.Invoke(foodTemplate);
            humanoid?.m_consumeItemEffects?.Create(monsterAi.transform.position, Quaternion.identity, null, 1f, -1, default(ZDOID));
            monsterAi.m_animator?.SetTrigger("consume");
            state.Clear();
            result = true;
            return true;
        }

        internal void TryAcquireChestTarget(MonsterAI monsterAi, float dt)
        {
            if (!CanUseChestFeeding(monsterAi) || monsterAi.m_consumeTarget != null)
            {
                return;
            }

            ChestFeedState state = _feedStates.GetOrCreateValue(monsterAi);
            if (state.Container != null && state.Food != null)
            {
                return;
            }

            if (state.MembershipRevision != _registry.MembershipRevision)
            {
                state.MembershipRevision = _registry.MembershipRevision;
                state.ContentRevision = _registry.ContentRevision;
                state.RetryBackoff = 0f;
                state.ContainerRefreshTimer = 5f;
                state.NearbyContainers?.Clear();
                state.NearbyContainerRevisions?.Clear();
            }

            if (state.ContentRevision != _registry.ContentRevision)
            {
                _metrics.RecordContentChangeCheck();
                if (_registry.HasContentChanges(state.NearbyContainers, state.NearbyContainerRevisions))
                {
                    state.RetryBackoff = 0f;
                    _metrics.RecordRelevantContentChange();
                }

                state.ContentRevision = _registry.ContentRevision;
            }

            state.SearchTimer += dt;
            if (state.SearchTimer < monsterAi.m_consumeSearchInterval + state.RetryBackoff)
            {
                return;
            }

            _metrics.ReportIfDue(Time.time, _logger);
            _metrics.RecordSearchAttempt();
            _registry.Initialize();
            _registry.RefreshIfNeeded();
            if (state.MembershipRevision != _registry.MembershipRevision)
            {
                state.MembershipRevision = _registry.MembershipRevision;
                state.ContentRevision = _registry.ContentRevision;
                state.RetryBackoff = 0f;
                state.ContainerRefreshTimer = 5f;
                state.NearbyContainers?.Clear();
                state.NearbyContainerRevisions?.Clear();
            }

            state.SearchTimer = 0f;
            state.Clear();
            state.ContainerRefreshTimer += dt;
            bool refreshNearby = state.NearbyContainers == null;
            if (state.NearbyContainers == null)
            {
                state.NearbyContainers = new List<Container>();
            }

            if (refreshNearby || state.ContainerRefreshTimer >= 5f)
            {
                _registry.FillNearby(monsterAi.transform.position, state.NearbyContainers);
                if (state.NearbyContainerRevisions == null)
                {
                    state.NearbyContainerRevisions = new List<int>();
                }

                _registry.CaptureContentRevisions(state.NearbyContainers, state.NearbyContainerRevisions);
                state.ContentRevision = _registry.ContentRevision;
                state.ContainerRefreshTimer = 0f;
                _metrics.RecordNearbyRefresh();
            }

            if (!TrySelectChestFood(monsterAi, _settings.FeedRangeMeters, state))
            {
                _metrics.RecordFailedSearch();
                state.RetryBackoff = state.RetryBackoff <= 0f
                    ? InitialRetryBackoffSeconds
                    : Mathf.Min(state.RetryBackoff * 2f, MaxRetryBackoffSeconds);
            }
            else
            {
                state.RetryBackoff = 0f;
            }
        }

        internal bool ShouldProtectContainer(WearNTear wearNTear, HitData hit)
        {
            if (!_settings.AutoFeedingEnabled || !_settings.ProtectFeedContainers || wearNTear == null || hit == null)
            {
                return false;
            }

            _registry.Initialize();
            Container? container = wearNTear.GetComponent<Container>() ??
                wearNTear.GetComponentInParent<Container>();
            if (container == null || !_registry.Contains(container))
            {
                return false;
            }

            Character? attacker = hit.GetAttacker();
            MonsterAI? monsterAi = attacker == null ? null : attacker.GetComponent<MonsterAI>();
            Tameable? tameable = monsterAi?.m_tamable;
            return tameable != null && !tameable.IsTamed();
        }

        private bool TrySelectChestFood(MonsterAI monsterAi, float maxRange, ChestFeedState state)
        {
            if (!CanUseChestFeeding(monsterAi))
            {
                return false;
            }

            Vector3 animalPosition = monsterAi.transform.position;
            Container? nearestContainer = null;
            ItemDrop? selectedFood = null;
            float nearestDistanceSquared = maxRange * maxRange;

            if (state.NearbyContainers == null)
            {
                return false;
            }

            foreach (Container container in state.NearbyContainers)
            {
                if (container == null)
                {
                    continue;
                }

                _metrics.RecordCandidateContainer();
                if (container.m_nview == null || !container.m_nview.IsValid() ||
                    !container.m_nview.IsOwner() || !_registry.HasAccess(container))
                {
                    continue;
                }

                float distanceSquared = (animalPosition - container.transform.position).sqrMagnitude;
                if (distanceSquared > nearestDistanceSquared ||
                    (!_settings.IgnorePathing && !HasPathTo(monsterAi, container.transform.position)))
                {
                    continue;
                }

                Inventory inventory = container.GetInventory();
                if (inventory == null)
                {
                    continue;
                }

                if (state.FoodTemplates == null)
                {
                    state.FoodTemplates = BuildFoodTemplates(monsterAi);
                }
                Dictionary<string, ItemDrop> foodTemplates = state.FoodTemplates!;

                if (!_registry.MightContainFood(container, foodTemplates))
                {
                    continue;
                }

                _metrics.RecordInventorySnapshot();
                foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                {
                    if (item == null || item.m_stack <= 0 || item.m_shared == null ||
                        !foodTemplates.TryGetValue(item.m_shared.m_name, out ItemDrop? food) ||
                        _settings.Filter.IsDisallowedFood(item, food) || !monsterAi.CanConsume(item))
                    {
                        continue;
                    }

                    nearestContainer = container;
                    selectedFood = food;
                    nearestDistanceSquared = distanceSquared;
                    break;
                }
            }

            if (nearestContainer == null || selectedFood == null)
            {
                return false;
            }

            state.Container = nearestContainer;
            state.Food = selectedFood;
            if (!_settings.IgnorePathing)
            {
                state.SetPathResult(nearestContainer.transform.position, true, PathResultCacheSeconds);
            }

            return true;
        }

        private bool CanUseChestFeeding(MonsterAI monsterAi)
        {
            return _settings.AutoFeedingEnabled && monsterAi.m_nview != null && monsterAi.m_nview.IsValid() &&
                monsterAi.m_nview.IsOwner() && monsterAi.m_tamable != null &&
                monsterAi.m_tamable.IsHungry() && !_settings.Filter.IsDisallowedAnimal(monsterAi.name);
        }

        private bool HasPathTo(MonsterAI monsterAi, Vector3 target)
        {
            if (_settings.IgnorePathing)
            {
                return true;
            }

            _metrics.RecordPathCheck();
            return monsterAi.HavePath(target);
        }

        private bool IsWithinFeedRange(Vector3 position, Vector3 target)
        {
            return (position - target).sqrMagnitude <= _settings.FeedRangeMeters * _settings.FeedRangeMeters;
        }

        private static Dictionary<string, ItemDrop> BuildFoodTemplates(MonsterAI monsterAi)
        {
            Dictionary<string, ItemDrop> templates = new Dictionary<string, ItemDrop>(System.StringComparer.Ordinal);
            foreach (ItemDrop food in monsterAi.m_consumeItems)
            {
                if (food?.m_itemData?.m_shared != null)
                {
                    templates[food.m_itemData.m_shared.m_name] = food;
                }
            }

            return templates;
        }
    }
}
