using Microsoft.EntityFrameworkCore;
using V2_Genesis.Data;
using V2_Genesis.Services.Attributes;

namespace V2_Genesis.Services.Implementations;

public sealed class AttributeInspectionCalendarService
    : IAttributeInspectionCalendarService
{
    private static readonly TimeSpan WorkingStart = new(8, 0, 0);
    private static readonly TimeSpan WorkingEnd = new(16, 0, 0);
    private const int SlotMinutes = 60;

    private readonly AttributesDbContext _db;

    public AttributeInspectionCalendarService(AttributesDbContext db)
    {
        _db = db;
    }

    public async Task<List<DateTime>> GetAvailableSlotsAsync(
        int processorUserId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        if (to <= from)
            return new List<DateTime>();

        // We do not allow a client to choose a slot in the past.
        if (from < DateTime.Now)
            from = DateTime.Now;

        var blocks = await _db.AttrInspectionCalendarBlocks
            .AsNoTracking()
            .Where(x =>
                x.UserId == processorUserId &&
                x.IsActive &&
                x.BlockedFrom < to &&
                x.BlockedTo > from)
            .ToListAsync(cancellationToken);

        var bookings = await _db.AttrInspectionRequests
            .AsNoTracking()
            .Where(x =>
                x.RequestedByUserId == processorUserId &&
                x.ConfirmedDateTime != null &&
                x.Status != "Expired" &&
                x.Status != "Cancelled" &&
                x.ConfirmedDateTime < to &&
                x.ConfirmedDateTime >= from.AddMinutes(-SlotMinutes))
            .Select(x => x.ConfirmedDateTime!.Value)
            .ToListAsync(cancellationToken);

        var result = new List<DateTime>();

        for (var date = from.Date;
             date <= to.Date;
             date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            for (var start = date.Add(WorkingStart);
                 start.AddMinutes(SlotMinutes) <= date.Add(WorkingEnd);
                 start = start.AddMinutes(SlotMinutes))
            {
                var end = start.AddMinutes(SlotMinutes);

                if (start <= DateTime.Now || start < from || start >= to)
                    continue;

                var blocked = blocks.Any(x =>
                    x.BlockedFrom < end &&
                    x.BlockedTo > start);

                var booked = bookings.Any(x =>
                    x < end &&
                    x.AddMinutes(SlotMinutes) > start);

                if (!blocked && !booked)
                    result.Add(start);
            }
        }

        return result;
    }

    public async Task<bool> IsSlotAvailableAsync(
        int processorUserId,
        DateTime slotStart,
        long? excludeInspectionRequestId = null,
        CancellationToken cancellationToken = default)
    {
        slotStart = new DateTime(
            slotStart.Year,
            slotStart.Month,
            slotStart.Day,
            slotStart.Hour,
            slotStart.Minute,
            0);

        if (slotStart <= DateTime.Now)
            return false;

        if (slotStart.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        if (slotStart.TimeOfDay < WorkingStart ||
            slotStart.AddMinutes(SlotMinutes).TimeOfDay > WorkingEnd)
            return false;

        // Keep the client aligned to the generated 60-minute slots.
        if (slotStart.Minute != 0)
            return false;

        var slotEnd = slotStart.AddMinutes(SlotMinutes);

        var blocked = await _db.AttrInspectionCalendarBlocks
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == processorUserId &&
                x.IsActive &&
                x.BlockedFrom < slotEnd &&
                x.BlockedTo > slotStart,
                cancellationToken);

        if (blocked)
            return false;

        var booked = await _db.AttrInspectionRequests
            .AsNoTracking()
            .AnyAsync(x =>
                x.RequestedByUserId == processorUserId &&
                x.Id != excludeInspectionRequestId &&
                x.ConfirmedDateTime != null &&
                x.Status != "Expired" &&
                x.Status != "Cancelled" &&
                x.ConfirmedDateTime < slotEnd &&
                x.ConfirmedDateTime.Value.AddMinutes(SlotMinutes) > slotStart,
                cancellationToken);

        return !booked;
    }
}
