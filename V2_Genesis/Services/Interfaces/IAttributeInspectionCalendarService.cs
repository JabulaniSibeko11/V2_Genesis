namespace V2_Genesis.Services.Attributes;

public interface IAttributeInspectionCalendarService
{
    Task<List<DateTime>> GetAvailableSlotsAsync(
        int processorUserId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<bool> IsSlotAvailableAsync(
        int processorUserId,
        DateTime slotStart,
        long? excludeInspectionRequestId = null,
        CancellationToken cancellationToken = default);
}
