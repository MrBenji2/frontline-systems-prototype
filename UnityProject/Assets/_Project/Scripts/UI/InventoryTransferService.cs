using System;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Milestone 7.5: Shared inventory transfer service for drag & drop and quick transfers.
    /// Provides a unified API for moving items between any two inventories.
    /// </summary>
    public static class InventoryTransferService
    {
        /// <summary>
        /// Result of a transfer attempt.
        /// </summary>
        public enum TransferResult
        {
            Success,
            PartialSuccess, // Some items transferred, not all
            FailedNoCapacity,
            FailedNoSlots,
            FailedNoWeight,
            FailedInvalidItem,
            FailedInsufficientSource
        }

        /// <summary>
        /// Interface for any inventory that can participate in transfers.
        /// </summary>
        public interface ITransferableInventory
        {
            bool CanAdd(string itemId, int count);
            bool TryAdd(string itemId, int count);
            bool TryRemove(string itemId, int count);
            int GetCount(string itemId);
            string InventoryName { get; }
        }

        /// <summary>
        /// Last transfer error message for UI display.
        /// </summary>
        public static string LastError { get; private set; } = "";

        /// <summary>
        /// Time when last error was set (for auto-clear).
        /// </summary>
        public static float LastErrorTime { get; private set; }

        /// <summary>
        /// Attempt to transfer items from one inventory to another.
        /// </summary>
        public static TransferResult TryTransfer(
            ITransferableInventory from,
            ITransferableInventory to,
            string itemId,
            int amount,
            out int actuallyTransferred)
        {
            actuallyTransferred = 0;
            LastError = "";

            if (from == null || to == null)
            {
                LastError = "Invalid inventory";
                LastErrorTime = Time.unscaledTime;
                return TransferResult.FailedInvalidItem;
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                LastError = "Invalid item";
                LastErrorTime = Time.unscaledTime;
                return TransferResult.FailedInvalidItem;
            }

            if (amount <= 0)
            {
                LastError = "Invalid amount";
                LastErrorTime = Time.unscaledTime;
                return TransferResult.FailedInvalidItem;
            }

            // Check source has enough.
            var available = from.GetCount(itemId);
            if (available <= 0)
            {
                LastError = $"No {itemId} in {from.InventoryName}";
                LastErrorTime = Time.unscaledTime;
                return TransferResult.FailedInsufficientSource;
            }

            var toTransfer = Mathf.Min(amount, available);

            // Try to transfer as many as possible.
            var transferred = 0;
            for (int i = 0; i < toTransfer; i++)
            {
                if (!to.CanAdd(itemId, 1))
                    break;

                if (!from.TryRemove(itemId, 1))
                    break;

                if (!to.TryAdd(itemId, 1))
                {
                    // Rollback - add back to source.
                    from.TryAdd(itemId, 1);
                    break;
                }

                transferred++;
            }

            actuallyTransferred = transferred;

            if (transferred == 0)
            {
                LastError = $"{to.InventoryName} is full";
                LastErrorTime = Time.unscaledTime;
                return TransferResult.FailedNoCapacity;
            }

            if (transferred < toTransfer)
            {
                LastError = $"Only {transferred}/{toTransfer} transferred";
                LastErrorTime = Time.unscaledTime;
                return TransferResult.PartialSuccess;
            }

            return TransferResult.Success;
        }

        /// <summary>
        /// Draw error message if one exists and hasn't expired.
        /// </summary>
        public static void DrawErrorIfAny()
        {
            if (string.IsNullOrEmpty(LastError))
                return;

            // Auto-clear after 3 seconds.
            if (Time.unscaledTime - LastErrorTime > 3f)
            {
                LastError = "";
                return;
            }

            var rect = new Rect((Screen.width - 300) * 0.5f, Screen.height - 60, 300, 30);
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
            GUI.Box(rect, "");
            GUI.backgroundColor = prevColor;
            GUI.Label(new Rect(rect.x + 10, rect.y + 6, rect.width - 20, 20), LastError);
        }

        /// <summary>
        /// Clear any error message.
        /// </summary>
        public static void ClearError()
        {
            LastError = "";
        }
    }

    /// <summary>
    /// Milestone 7.5: IMGUI-based drag state for inventory items.
    /// </summary>
    public static class InventoryDragState
    {
        public static bool IsDragging { get; private set; }
        public static string DragItemId { get; private set; } = "";
        public static int DragAmount { get; private set; }
        public static InventoryTransferService.ITransferableInventory DragSource { get; private set; }
        public static Vector2 DragStartPos { get; private set; }

        /// <summary>
        /// Start dragging an item.
        /// </summary>
        public static void BeginDrag(string itemId, int amount, InventoryTransferService.ITransferableInventory source)
        {
            IsDragging = true;
            DragItemId = itemId ?? "";
            DragAmount = amount;
            DragSource = source;
            DragStartPos = Event.current != null ? Event.current.mousePosition : Vector2.zero;
        }

        /// <summary>
        /// End dragging and attempt transfer to target.
        /// </summary>
        public static bool EndDrag(InventoryTransferService.ITransferableInventory target)
        {
            if (!IsDragging || DragSource == null || target == null || target == DragSource)
            {
                CancelDrag();
                return false;
            }

            var result = InventoryTransferService.TryTransfer(DragSource, target, DragItemId, DragAmount, out _);
            CancelDrag();
            return result == InventoryTransferService.TransferResult.Success ||
                   result == InventoryTransferService.TransferResult.PartialSuccess;
        }

        /// <summary>
        /// Cancel drag without transfer.
        /// </summary>
        public static void CancelDrag()
        {
            IsDragging = false;
            DragItemId = "";
            DragAmount = 0;
            DragSource = null;
        }

        /// <summary>
        /// Draw the drag ghost if dragging.
        /// </summary>
        public static void DrawDragGhost()
        {
            if (!IsDragging || string.IsNullOrEmpty(DragItemId))
                return;

            var mousePos = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            var rect = new Rect(mousePos.x + 10, mousePos.y + 10, 120, 24);

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
            GUI.Box(rect, "");
            GUI.backgroundColor = prevColor;

            var label = DragAmount > 1 ? $"{DragItemId} x{DragAmount}" : DragItemId;
            GUI.Label(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 16), label);
        }
    }
}
