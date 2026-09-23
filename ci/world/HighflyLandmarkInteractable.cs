using UnityEngine;

namespace Highfly.World
{
    [DisallowMultipleComponent]
    public sealed class HighflyLandmarkInteractable : MonoBehaviour, IInteractable
    {
        public string landmarkId = "landmark";
        public string displayName = "Lugar";
        public HighflyLandmarkType type = HighflyLandmarkType.Generic;

        public string GetInteractPrompt()
        {
            switch (type)
            {
                case HighflyLandmarkType.Inn: return "USAR • Entrar a " + displayName;
                case HighflyLandmarkType.Smithy: return "USAR • Entrar a " + displayName;
                case HighflyLandmarkType.Market: return "USAR • Comerciar en " + displayName;
                case HighflyLandmarkType.Guild: return "USAR • Entrar al " + displayName;
                default: return "USAR • " + displayName;
            }
        }

        public void Interact(GameObject player)
        {
            var world = HighflyWorldState.Instance;
            if (world == null) return;

            bool first = !world.IsDiscovered(landmarkId);
            world.MarkDiscovered(landmarkId);

            switch (type)
            {
                case HighflyLandmarkType.Inn:
                    world.Notice(first
                        ? $"Descubriste {displayName}. Interior explorable pendiente de conexión."
                        : $"{displayName}: entrada detectada.");
                    break;

                case HighflyLandmarkType.Smithy:
                    world.Notice(first
                        ? $"Descubriste {displayName}. Interior de forja pendiente de conexión."
                        : $"{displayName}: entrada detectada.");
                    break;

                case HighflyLandmarkType.Market:
                {
                    int sold = 0;
                    if (world.Spend(HighflyResourceType.Wood, 2)) sold += 2;
                    if (world.Spend(HighflyResourceType.Herb, 2)) sold += 2;
                    if (sold > 0)
                    {
                        int gold = sold * 3;
                        world.Add(HighflyResourceType.Gold, gold);
                        world.Notice($"{displayName}: vendiste recursos por {gold} de Oro.");
                    }
                    else
                    {
                        world.Notice($"{displayName}: traé madera o hierbas para comerciar.");
                    }
                    break;
                }

                default:
                    world.Notice(first ? $"Descubriste {displayName}." : displayName);
                    break;
            }
        }
    }
}
