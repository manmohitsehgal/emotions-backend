using Emotions.Application.DTOs.Rooms;

namespace Emotions.Application.Interfaces;

public interface IBookingsService
{
    Task<BookingDto> BookAsync(Guid sessionId, Guid userId, bool isPremium, CancellationToken ct);
    Task CancelAsync(Guid sessionId, Guid userId, CancellationToken ct);
    Task<BookingDto?> GetMyBookingAsync(Guid sessionId, Guid userId, CancellationToken ct);
    Task<int> AutoReleaseNoShowsAsync(TimeSpan grace, CancellationToken ct);
    Task CheckInAsync(Guid sessionId, Guid userId, CancellationToken ct);
}