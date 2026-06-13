using V2_Genesis.Models.Notifications;

namespace V2_Genesis.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateClientNotificationAsync(
            string? userId,
            string? userEmail,
            string title,
            string message,
            string? referenceNumber,
            string? premiseId,
            string? rollSource,
            string? sourceTable,
            string? url,
            string? createdBy = null);

        Task CreateAdminNotificationAsync(
            string title,
            string message,
            string? referenceNumber,
            string? premiseId,
            string? rollSource,
            string? sourceTable,
            string? url,
            string? createdBy = null);

        Task<int> GetUnreadCountAsync(string? userId, string? userEmail, bool isAdmin);

        Task<List<Notifications>> GetLatestAsync(string? userId, string? userEmail, bool isAdmin, int take = 10);

        Task MarkAsReadAsync(long id, string? userId, string? userEmail, bool isAdmin);
    }
}