using UnityEngine;

namespace Frontline.PlayerCard
{
    /// <summary>
    /// Debug hotkeys for the Player Card system.
    /// </summary>
    public sealed class PlayerCardDebugHotkeys : MonoBehaviour
    {
        [SerializeField] private KeyCode addKillsKey = KeyCode.K;
        [SerializeField] private KeyCode addTrustKey = KeyCode.T;
        [SerializeField] private KeyCode resetStatsKey = KeyCode.F12;

        private void Update()
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            // K: Add 5 kills
            if (Input.GetKeyDown(addKillsKey) && Input.GetKey(KeyCode.LeftShift))
            {
                for (int i = 0; i < 5; i++)
                    statsService.RecordKill(false);
                Debug.Log("[PlayerCardDebug] Added 5 kills");
            }

            // T: Add 10 trust
            if (Input.GetKeyDown(addTrustKey) && Input.GetKey(KeyCode.LeftShift))
            {
                var trustService = Trust.TrustService.Instance;
                if (trustService != null)
                {
                    trustService.State.trustScore += 10;
                    trustService.State.rankId = trustService.EvaluateRankId(trustService.State.trustScore);
                    Debug.Log($"[PlayerCardDebug] Added 10 trust. New score: {trustService.State.trustScore}, Rank: {trustService.State.rankId}");
                }
            }

            // F12: Reset all stats
            if (Input.GetKeyDown(resetStatsKey))
            {
                statsService.DevResetStats();
                Debug.Log("[PlayerCardDebug] Reset all player stats");
            }
        }

        private void OnGUI()
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            // Show debug hints in corner
            var y = Screen.height - 160f;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.LowerLeft
            };
            GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);

            GUI.Label(new Rect(10, y, 300, 20), "[P] Player Card", style);
            y += 15;
            GUI.Label(new Rect(10, y, 300, 20), "[Shift+K] Add 5 Kills", style);
            y += 15;
            GUI.Label(new Rect(10, y, 300, 20), "[Shift+T] Add 10 Trust", style);
            y += 15;
            GUI.Label(new Rect(10, y, 300, 20), "[F12] Reset Stats", style);

            GUI.contentColor = Color.white;
        }
    }
}
