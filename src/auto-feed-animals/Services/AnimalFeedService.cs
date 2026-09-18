using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AutoFeedAnimals
{
    internal sealed class AnimalFeedService
    {
        private readonly AutoFeedAnimalsSettings _settings;
        private readonly FeedContainerRegistry _registry;
        private readonly ConditionalWeakTable<MonsterAI, ChestFeedState> _feedStates = new ConditionalWeakTable<MonsterAI, ChestFeedState>();

        internal AnimalFeedService(AutoFeedAnimalsSettings settings, FeedContainerRegistry registry)
        {
            _settings = settings;
            _registry = registry;
        }

        internal void ClearChestTargets()
        {
            foreach (MonsterAI monsterAi in UnityEngine.Object.FindObjectsByType<MonsterAI>(FindObjectsSortMode.None))
            {
                if (_feedStates.TryGetValue(monsterAi, out ChestFeedState state))
                {
                    state.Clear();
                    state.NearbyContainers = null;
                    state.FoodTemplates = null;
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
                if (!HasPathTo(monsterAi, target))
                {
                    state.Clear();
                    result = false;
                    return true;
                }

                bool reached = monsterAi.MoveTo(dt, target, monsterAi.m_consumeRange, false);
                monsterAi.LookAt(target);
                if (!reached &&
                    (Vector3.Distance(monsterAi.transform.position, target) > monsterAi.m_consumeRange ||
                     !monsterAi.IsLookingAt(target, 35f, false)))
                {
                    result = true;
                    return true;
                }

                monsterAi.StopMoving();
            }

            Inventory inventory = container.GetInventory();
            if (inventory == null || !_registry.HasAccess(container))
            {
                state.Clear();
                result = false;
                return true;
            }

            ItemDrop.ItemData? item = null;
            foreach (ItemDrop.ItemData candidate in inventory.GetAllItems())
            {
                if (candidate != null && candidate.m_stack > 0 && candidate.m_shared != null &&
                    state.Food.m_itemData != null && state.Food.m_itemData.m_shared != null &&
                    candidate.m_shared.m_name == state.Food.m_itemData.m_shared.m_name &&
                    !_settings.Filter.IsDisallowedFood(candidate, state.Food) &&
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

            monsterAi.m_onConsumedItem?.Invoke(state.Food);
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

            state.SearchTimer += dt;
            if (state.SearchTimer < monsterAi.m_consumeSearchInterval)
            {
                return;
            }

            state.SearchTimer = 0f;
            state.Clear();
            state.ContainerRefreshTimer += dt;
            if (state.NearbyContainers == null || state.ContainerRefreshTimer >= 5f)
            {
                state.NearbyContainers = _registry.FindNearby(monsterAi.transform.position);
                state.ContainerRefreshTimer = 0f;
            }
            TrySelectChestFood(monsterAi, _settings.FeedRangeMeters, state);
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
            Tameable? tameable = attacker == null ? null : attacker.GetComponent<Tameable>();
            return tameable != null && !tameable.IsTamed();
        }

        private bool TrySelectChestFood(MonsterAI monsterAi, float maxRange, ChestFeedState state)
        {
            if (!CanUseChestFeeding(monsterAi))
            {
                return false;
            }

            _registry.Initialize();
            _registry.RefreshIfNeeded();
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
                if (container == null || container.m_nview == null || !container.m_nview.IsValid() ||
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
            return _settings.IgnorePathing || monsterAi.HavePath(target);
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
