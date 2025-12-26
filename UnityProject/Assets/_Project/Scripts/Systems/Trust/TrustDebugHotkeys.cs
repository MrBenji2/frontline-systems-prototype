using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontline.Trust
{
    public sealed class TrustDebugHotkeys : MonoBehaviour
    {
        private string _lastGrantedCertId = "";

        private void Update()
        {
            if (TrustService.Instance == null)
                return;

            // Example gate attempts (safe, debug-only).
            if (Input.GetKeyDown(KeyCode.F1))
                Attempt("log.withdraw_high");
            if (Input.GetKeyDown(KeyCode.F2))
                Attempt("eng.dismantle_allied");
            if (Input.GetKeyDown(KeyCode.F3))
                Attempt("veh.pull_lighttransport");

            var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!ctrl || !shift)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Grant("logistics_2_operator");
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Grant("engineering_3_demolitions");
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ToggleActive(_lastGrantedCertId);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ForceExpiryByVersionBump(_lastGrantedCertId);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SwitchFaction();
                return;
            }
        }

        private void Attempt(string permission)
        {
            var svc = TrustService.Instance;
            var ok = PermissionGate.Can(svc.State, permission, out var reason);
            Debug.Log(ok
                ? $"[Trust] ALLOW '{permission}'"
                : $"[Trust] DENY '{permission}': {reason}");
        }

        private void Grant(string certId)
        {
            var svc = TrustService.Instance;
            if (svc.GrantCertification(certId))
                _lastGrantedCertId = certId;
            PrintSummary();
        }

        private void ToggleActive(string certId)
        {
            var svc = TrustService.Instance;
            if (string.IsNullOrWhiteSpace(certId) || !svc.State.certsById.TryGetValue(certId, out var cs) || cs == null)
            {
                PrintSummary();
                return;
            }

            // Use active flag toggling as the minimal revoke/restore action.
            svc.SetCertificationActive(certId, !cs.isActive);
            PrintSummary();
        }

        private void ForceExpiryByVersionBump(string certId)
        {
            var svc = TrustService.Instance;
            if (!svc.DevIncrementDefinitionVersion(certId))
            {
                PrintSummary();
                return;
            }

            svc.RecomputeExpiration();
            PrintSummary();
        }

        private void SwitchFaction()
        {
            var svc = TrustService.Instance;
            var next = svc.State.faction switch
            {
                FactionId.Wardens => FactionId.Colonials,
                FactionId.Colonials => FactionId.Wardens,
                _ => FactionId.Wardens,
            };

            svc.SetFaction(next);
            PrintSummary();
        }

        private void PrintSummary()
        {
            var s = TrustService.Instance.State;

            var active = new List<string>();
            var expired = new List<string>();

            foreach (var kv in s.certsById)
            {
                var cs = kv.Value;
                if (cs == null)
                    continue;
                if (cs.isExpired)
                    expired.Add($"{cs.certId}(t{cs.tier})");
                else if (cs.isActive)
                    active.Add($"{cs.certId}(t{cs.tier})");
            }

            active.Sort();
            expired.Sort();

            Debug.Log($"[Trust] faction={s.faction} trustScore={s.trustScore} rankId={s.rankId} active=[{string.Join(", ", active)}] expired=[{string.Join(", ", expired)}]");
        }
    }
}

