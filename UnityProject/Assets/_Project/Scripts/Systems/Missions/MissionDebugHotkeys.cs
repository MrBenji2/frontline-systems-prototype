using Frontline.Trust;
using UnityEngine;

namespace Frontline.Missions
{
    /// <summary>
    /// Debug hotkeys for testing the mission system.
    /// </summary>
    public sealed class MissionDebugHotkeys : MonoBehaviour
    {
        [SerializeField] private KeyCode completeRifleTrainingKey = KeyCode.F9;
        [SerializeField] private KeyCode resetMissionsKey = KeyCode.F10;
        [SerializeField] private KeyCode grantRiflemanKey = KeyCode.F11;

        private void Update()
        {
            // F9: Complete rifle training mission
            if (Input.GetKeyDown(completeRifleTrainingKey))
            {
                var missionService = MissionService.Instance;
                if (missionService != null)
                {
                    if (missionService.DevCompleteMission("training_basic_rifle"))
                    {
                        Debug.Log("[MissionDebug] Completed training_basic_rifle mission");
                    }
                    else
                    {
                        Debug.LogWarning("[MissionDebug] Failed to complete training_basic_rifle");
                    }
                }
            }

            // F10: Reset all missions
            if (Input.GetKeyDown(resetMissionsKey))
            {
                var missionService = MissionService.Instance;
                if (missionService != null)
                {
                    missionService.DevResetAllMissions();
                    Debug.Log("[MissionDebug] Reset all mission progress");
                }

                // Also reset certifications to recruit_basic only
                var trustService = TrustService.Instance;
                if (trustService != null)
                {
                    // Revoke infantry certification
                    trustService.RevokeCertification("infantry_1_rifleman");
                    Debug.Log("[MissionDebug] Revoked infantry_1_rifleman certification");
                }
            }

            // F11: Directly grant rifleman certification (bypass mission)
            if (Input.GetKeyDown(grantRiflemanKey))
            {
                var trustService = TrustService.Instance;
                if (trustService != null)
                {
                    if (trustService.GrantCertification("infantry_1_rifleman"))
                    {
                        Debug.Log("[MissionDebug] Granted infantry_1_rifleman certification directly");
                    }
                }
            }
        }

        private void OnGUI()
        {
            // Show debug hotkey hints in corner
            var missionService = MissionService.Instance;
            var trustService = TrustService.Instance;

            if (missionService == null || trustService == null)
                return;

            var y = Screen.height - 100f;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.LowerLeft
            };
            GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);

            GUI.Label(new Rect(10, y, 300, 20), "[F9] Complete Rifle Training", style);
            y += 15;
            GUI.Label(new Rect(10, y, 300, 20), "[F10] Reset Missions + Certs", style);
            y += 15;
            GUI.Label(new Rect(10, y, 300, 20), "[F11] Grant Rifleman Cert", style);
            y += 15;

            // Show current cert status
            var hasRifleman = trustService.State.certsById.TryGetValue("infantry_1_rifleman", out var cert)
                              && cert != null && cert.isActive;
            var certStatus = hasRifleman ? "<color=#88FF88>UNLOCKED</color>" : "<color=#FFAAAA>LOCKED</color>";
            GUI.Label(new Rect(10, y, 300, 20), $"Rifleman Cert: {certStatus}", style);

            GUI.contentColor = Color.white;
        }
    }
}
