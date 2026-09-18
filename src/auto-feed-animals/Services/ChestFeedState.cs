using System.Collections.Generic;

namespace AutoFeedAnimals
{
    internal sealed class ChestFeedState
    {
        internal Container? Container;
        internal ItemDrop? Food;
        internal float SearchTimer;
        internal float ContainerRefreshTimer;
        internal List<Container>? NearbyContainers;
        internal Dictionary<string, ItemDrop>? FoodTemplates;

        internal void Clear()
        {
            Container = null;
            Food = null;
        }
    }
}
