using System;
using System.Collections.Generic;

namespace AutoFeedAnimals
{
    internal sealed class FeedFilter
    {
        private readonly HashSet<string> _disallowedFeedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _disallowedAnimalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal FeedFilter(string disallowFeed, string disallowAnimal)
        {
            Update(disallowFeed, disallowAnimal);
        }

        internal void Update(string disallowFeed, string disallowAnimal)
        {
            _disallowedFeedNames.Clear();
            _disallowedAnimalNames.Clear();
            AddEntries(_disallowedFeedNames, disallowFeed);
            AddEntries(_disallowedAnimalNames, disallowAnimal);
        }

        internal bool IsDisallowedAnimal(string animalName)
        {
            return Matches(_disallowedAnimalNames, animalName);
        }

        internal bool IsDisallowedFood(ItemDrop.ItemData item, ItemDrop foodTemplate)
        {
            return Matches(_disallowedFeedNames, item.m_shared?.m_name) ||
                Matches(_disallowedFeedNames, foodTemplate.name);
        }

        private static void AddEntries(HashSet<string> destination, string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return;
            }

            foreach (string entry in csv.Split(','))
            {
                string trimmed = entry.Trim();
                if (trimmed.Length > 0)
                {
                    destination.Add(trimmed);
                }
            }
        }

        private static bool Matches(HashSet<string> filter, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string candidateValue = value!;
            foreach (string entry in filter)
            {
                if (candidateValue.StartsWith(entry, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
