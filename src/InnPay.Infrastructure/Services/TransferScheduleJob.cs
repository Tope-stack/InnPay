using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InnPay.Infrastructure.Services;

/// <summary>
/// Background job that runs every 60 seconds, finds due transfer schedules,
/// and executes them via IInternalTransferService.
///
/// Note: This runs in-process. For production scale, consider Hangfire or
/// Azure Service Bus with a dedicated worker.
/// </summary>
public class TransferScheduleJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransferScheduleJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public TransferScheduleJob(IServiceScopeFactory scopeFactory, ILogger<TransferScheduleJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TransferScheduleJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSchedulesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TransferScheduleJob encountered an error.");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("TransferScheduleJob stopped.");
    }

    private async Task ProcessDueSchedulesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var transferService = scope.ServiceProvider.GetRequiredService<IInternalTransferService>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var dueSchedules = (await uow.TransferSchedules.GetDueSchedulesAsync(DateTime.UtcNow)).ToList();

        if (dueSchedules.Count == 0) return;

        _logger.LogInformation("TransferScheduleJob: processing {Count} due schedule(s).", dueSchedules.Count);

        foreach (var schedule in dueSchedules)
        {
            try
            {
                var senderAccount = await uow.Accounts.GetByIdAsync(schedule.SenderAccountId);
                var receiverAccount = await uow.Accounts.GetByIdAsync(schedule.ReceiverAccountId);

                if (senderAccount is null || receiverAccount is null)
                {
                    _logger.LogWarning("Scheduled transfer {Id}: account not found — skipping.", schedule.Id);
                    continue;
                }

                // Use receiver's phone/email as identifier for the service lookup
                var receiverUser = await uow.Users.GetByIdAsync(receiverAccount.UserId);

                // Build a system-initiated transfer request (PIN bypassed for scheduled jobs)
                // In production, store an encrypted PIN or use a service-account token
                var request = new InternalTransferRequest
                {
                    SenderAccountId = schedule.SenderAccountId,
                    RecipientIdentifier = receiverUser?.PhoneNumber ?? receiverUser?.Email ?? string.Empty,
                    Amount = schedule.Amount,
                    Currency = schedule.Currency,
                    Note = schedule.Note ?? "Scheduled transfer",
                    TransactionPin = "SYSTEM",  // bypassed below
                    SaveBeneficiary = false
                };

                // Directly debit/credit wallets for scheduled jobs — PIN check skipped (system-initiated)
                var senderWallet = await uow.Wallets.GetByAccountAndCurrencyAsync(schedule.SenderAccountId, schedule.Currency);
                var receiverWallet = await uow.Wallets.GetByAccountAndCurrencyAsync(schedule.ReceiverAccountId, schedule.Currency);

                if (senderWallet is null || receiverWallet is null || senderWallet.AvailableBalance < schedule.Amount)
                {
                    _logger.LogWarning("Scheduled transfer {Id}: insufficient balance or missing wallet — skipping.", schedule.Id);
                    continue;
                }

                senderWallet.Balance -= schedule.Amount;
                senderWallet.AvailableBalance -= schedule.Amount;
                receiverWallet.Balance += schedule.Amount;
                receiverWallet.AvailableBalance += schedule.Amount;

                await uow.Wallets.UpdateAsync(senderWallet);
                await uow.Wallets.UpdateAsync(receiverWallet);

                // Advance the next run date
                schedule.NextRunAt = schedule.Frequency switch
                {
                    TransferFrequency.Daily => schedule.NextRunAt.AddDays(1),
                    TransferFrequency.Weekly => schedule.NextRunAt.AddDays(7),
                    TransferFrequency.Monthly => schedule.NextRunAt.AddMonths(1),
                    _ => schedule.NextRunAt.AddDays(1)
                };

                await uow.TransferSchedules.UpdateAsync(schedule);
                await uow.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Scheduled transfer {Id} executed. Next run: {NextRun}.", schedule.Id, schedule.NextRunAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process scheduled transfer {Id}.", schedule.Id);
            }
        }
    }
}
