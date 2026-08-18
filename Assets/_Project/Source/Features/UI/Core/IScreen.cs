// Base contract for every screen in the game.
// All screens show/hide with animations - never instant pop in/out.

using Cysharp.Threading.Tasks;

namespace GalacticEmpire.Feature.UI.Core
{
    public interface IScreen
    {
        // Show this screen with entrance animation
        UniTask ShowAsync();

        // Hide this screen with exit animation
        UniTask HideAsync();

        // True if the screen is currently visible
        bool IsVisible { get; }
    }
}
