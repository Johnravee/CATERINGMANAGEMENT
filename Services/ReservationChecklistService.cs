using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services.Data;
using System.Collections.ObjectModel;

namespace CATERINGMANAGEMENT.Services
{
    /// <summary>
    /// Orchestrates building the checklist PDF without saving anything to DB.
    /// - Resolves reservation with joins, menu orders, and assigned workers.
    /// - Accepts selected equipment items and an optional design image path.
    /// - Delegates to ReservationChecklistPdfGenerator.
    /// </summary>
    public class ReservationChecklistService
    {
        private readonly ReservationService _reservationService = new();
        private readonly SchedulingService _schedulingService = new();

        public async Task GenerateChecklistPdfAsync(
            long reservationId,
            IEnumerable<SelectedEquipmentItem> selectedEquipments,
            string? designImagePath = null,
            string? callTime = null)
        {
            var reservation = await _reservationService.GetReservationWithJoinsAsync(reservationId);
            if (reservation == null)
            {
                AppLogger.Error("Reservation not found.");
                return;
            }

            // Menu orders -> menu items
            var menuOrders = await _reservationService.GetReservationMenuOrdersAsync(reservationId);
            var menuItems = menuOrders
                .Where(m => m.menu_options != null)
                .Select(m => m.menu_options!)
                .ToList();

            // Assigned workers from grouped view (fallback to empty if none)
            var schedulesPage = await _schedulingService.GetPagedGroupedSchedulesAsync(1);
            var workersStr = schedulesPage.Schedules
                .FirstOrDefault(s => s.ReservationId == reservationId)?.AssignedWorkers;

            var assignedWorkers = new List<Worker>();
            if (!string.IsNullOrWhiteSpace(workersStr))
            {
                foreach (var part in workersStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    assignedWorkers.Add(new Worker { Name = part });
                }
            }

            DocumentsGenerator.ReservationChecklistPdfGenerator.Generate(
                reservation,
                selectedEquipments,
                menuItems,
                assignedWorkers,
                designImagePath,
                callTime,
                $"Checklist_{reservation.ReceiptNumber}_{reservation.EventDate:yyyyMMdd}"
            );
        }
    }
}
