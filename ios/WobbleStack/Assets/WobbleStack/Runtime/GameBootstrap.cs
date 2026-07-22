using UnityEngine;

namespace WobbleStack.Runtime
{
    internal static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGame()
        {
            if (Object.FindFirstObjectByType<WobbleStackGame>() != null)
            {
                return;
            }

            GameObject gameObject = new GameObject("Wobble Stack Game");
            gameObject.AddComponent<WobbleStackGame>();
        }
    }
}
