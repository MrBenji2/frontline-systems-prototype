using Frontline.Crafting;
using UnityEngine;

namespace Frontline.World
{
    public sealed class CraftingStation : MonoBehaviour
    {
        [SerializeField] private CraftingStationType stationType = CraftingStationType.Workbench;
        [SerializeField] private string displayName = "Workbench";

        public CraftingStationType StationType => stationType;
        public string DisplayName => displayName;

        public void Configure(CraftingStationType type, string name)
        {
            stationType = type;
            displayName = string.IsNullOrWhiteSpace(name) ? type.ToString() : name;
        }
    }
}

