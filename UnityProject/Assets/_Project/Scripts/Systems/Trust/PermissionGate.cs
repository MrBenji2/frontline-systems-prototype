using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontline.Trust
{
    public static class PermissionGate
    {
        public static bool Can(PlayerTrustState state, string permission, out string reason)
        {
            reason = "Missing certification";

            if (state == null)
            {
                reason = "Missing certification";
                return false;
            }

            if (string.IsNullOrWhiteSpace(permission))
            {
                reason = "Missing certification";
                return false;
            }

            var svc = TrustService.Instance;
            if (svc == null)
            {
                reason = "Missing certification";
                return false;
            }

            var defsWithPermission = svc.GetCertsGrantingPermission(permission);
            if (defsWithPermission.Count == 0)
            {
                reason = "Missing certification";
                return false;
            }

            var foundInactive = false;
            var foundExpired = false;

            foreach (var certId in defsWithPermission)
            {
                if (!state.certsById.TryGetValue(certId, out var cs) || cs == null)
                    continue;

                if (cs.isExpired)
                {
                    foundExpired = true;
                    continue;
                }

                if (!cs.isActive)
                {
                    foundInactive = true;
                    continue;
                }

                reason = "";
                return true;
            }

            if (foundExpired)
            {
                reason = "Certification expired";
                return false;
            }

            if (foundInactive)
            {
                reason = "Certification inactive";
                return false;
            }

            reason = "Missing certification";
            return false;
        }

        public static bool Require(string permission)
        {
            var svc = TrustService.Instance;
            if (svc == null)
                return false;

            var state = svc.State;
            var ok = Can(state, permission, out var reason);
            if (!ok)
            {
                svc.AuditLog.Add(state.playerId, state.faction, "PERMISSION_DENIED",
                    $"{{\"permission\":\"{Escape(permission)}\",\"reason\":\"{Escape(reason)}\"}}");
                Debug.LogWarning($"Permission denied: '{permission}' ({reason})");
            }

            return ok;
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

