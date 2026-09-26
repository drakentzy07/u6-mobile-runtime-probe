using System;
using UnityEngine;

namespace Highfly.SkillLab
{
    // Loot Lab compatibility shim for the Golden mobile/player stack.
    public static class HighflySkillLabMode
    {
        public static bool IsActive
        {
            get
            {
                string url = Application.absoluteURL ?? string.Empty;
                return url.IndexOf("loot-monster-lab", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       url.IndexOf("lootlab=1", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
    }

    // PlayerController.Mobile references this optional LAB mobility layer.
    // Loot Lab intentionally uses the proven base jump only.
    public sealed class HighflyAerialMobility : MonoBehaviour
    {
        public static HighflyAerialMobility Instance => null;
        public void RequestJump() { }
    }
}
