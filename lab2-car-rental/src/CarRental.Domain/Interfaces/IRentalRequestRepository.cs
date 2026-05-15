using CarRental.Domain.Entities;

namespace CarRental.Domain.Interfaces;

public interface IRentalRequestRepository
{
    Task<RentalRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(RentalRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(RentalRequest request, CancellationToken cancellationToken = default);

    // True if the car already has an Approved request whose date interval overlaps
    // [start, end). Used to prevent double-booking. Pending/Rejected/Completed are
    // ignored — only confirmed contracts block the calendar.
    Task<bool> HasOverlapAsync(int carId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
}
