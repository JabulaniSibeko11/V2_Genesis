using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Models.Notifications;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task CreateClientNotificationAsync(
            string? userId,
            string? userEmail,
            string title,
            string message,
            string? referenceNumber,
            string? premiseId,
            string? rollSource,
            string? sourceTable,
            string? url,
            string? createdBy = null)
        {
            var notification = new Notifications
            {
                UserID = userId,
                UserEmail = userEmail,
                TargetRole = "Client",
                Title = title,
                Message = message,
                NotificationType = "Success",
                ReferenceNumber = referenceNumber,
                PremiseID = premiseId,
                RollSource = rollSource,
                SourceTable = sourceTable,
                Url = url,
                IsRead = false,
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task CreateAdminNotificationAsync(
            string title,
            string message,
            string? referenceNumber,
            string? premiseId,
            string? rollSource,
            string? sourceTable,
            string? url,
            string? createdBy = null)
        {
            var notification = new Notifications
            {
                TargetRole = "AllAdmins",
                Title = title,
                Message = message,
                NotificationType = "Info",
                ReferenceNumber = referenceNumber,
                PremiseID = premiseId,
                RollSource = rollSource,
                SourceTable = sourceTable,
                Url = url,
                IsRead = false,
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(
            string? userId,
            string? userEmail,
            bool isAdmin)
        {
            var query = _db.Notifications.AsQueryable();

            if (isAdmin)
            {
                query = query.Where(x => x.TargetRole == "AllAdmins" || x.TargetRole == "Admin");
            }
            else
            {
                query = query.Where(x =>
                    x.TargetRole == "Client" &&
                    (
                        (!string.IsNullOrWhiteSpace(userId) && x.UserID == userId) ||
                        (!string.IsNullOrWhiteSpace(userEmail) && x.UserEmail == userEmail)
                    ));
            }

            return await query.CountAsync(x => !x.IsRead);
        }

        public async Task<List<Notifications>> GetLatestAsync(
            string? userId,
            string? userEmail,
            bool isAdmin,
            int take = 10)
        {
            var query = _db.Notifications.AsQueryable();

            if (isAdmin)
            {
                query = query.Where(x => x.TargetRole == "AllAdmins" || x.TargetRole == "Admin");
            }
            else
            {
                query = query.Where(x =>
                    x.TargetRole == "Client" &&
                    (
                        (!string.IsNullOrWhiteSpace(userId) && x.UserID == userId) ||
                        (!string.IsNullOrWhiteSpace(userEmail) && x.UserEmail == userEmail)
                    ));
            }

            return await query
                .OrderByDescending(x => x.CreatedDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(
            long id,
            string? userId,
            string? userEmail,
            bool isAdmin)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(x => x.IDNotifications == id);

            if (notification == null)
                return;

            var canRead = isAdmin
                ? notification.TargetRole == "AllAdmins" || notification.TargetRole == "Admin"
                : notification.TargetRole == "Client" &&
                  (
                      (!string.IsNullOrWhiteSpace(userId) && notification.UserID == userId) ||
                      (!string.IsNullOrWhiteSpace(userEmail) && notification.UserEmail == userEmail)
                  );

            if (!canRead)
                return;

            notification.IsRead = true;
            notification.ReadDate = DateTime.Now;

            await _db.SaveChangesAsync();
        }
    }
}