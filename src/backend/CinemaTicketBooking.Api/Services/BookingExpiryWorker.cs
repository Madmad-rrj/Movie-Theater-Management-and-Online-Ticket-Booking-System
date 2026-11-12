using Microsoft.Extensions.Options;

namespace CinemaTicketBooking.Api.Services;

public sealed class BookingExpiryWorker(IServiceScopeFactory scopes, IOptions<BookingExpiryOptions> settings,
    ILogger<BookingExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Value.Enabled) return;
        long cursor = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var batch = await scope.ServiceProvider.GetRequiredService<BookingExpiryService>().RunBatchAsync(cursor, stoppingToken);
                cursor = batch.Cursor;
                if (batch.Scanned > 0)
                    logger.LogInformation("Booking expiry sweep: scanned {Scanned}, expired {Expired}, deferred {Deferred}, busy {Busy}, skipped {Skipped}, failed {Failed}.",
                        batch.Scanned, batch.Expired, batch.Deferred, batch.Busy, batch.Skipped, batch.Failed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning("Booking expiry sweep failed ({ErrorType}); will retry on the next interval.", ex.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(settings.Value.PollIntervalSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
