using InnPay.Domain.Entities;
using InnPay.Domain.Interfaces;
using InnPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InnPay.Infrastructure.Repositories;

// ── 6.1 Payment ───────────────────────────────────────────────────────────────

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;
    public PaymentRepository(AppDbContext db) => _db = db;

    public Task<Payment?> GetByIdAsync(Guid id)
        => _db.Payments.FirstOrDefaultAsync(p => p.Id == id);

    public Task<Payment?> GetByIdempotencyKeyAsync(string key)
        => _db.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == key);

    public Task<Payment?> GetByReferenceAsync(string reference)
        => _db.Payments.FirstOrDefaultAsync(p => p.Reference == reference);

    public async Task<IEnumerable<Payment>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.Payments
            .Where(p => p.AccountId == accountId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(Payment payment) => await _db.Payments.AddAsync(payment);
    public Task UpdateAsync(Payment payment) { _db.Payments.Update(payment); return Task.CompletedTask; }
}

// ── 6.3 Internal Transfer ─────────────────────────────────────────────────────

public class InternalTransferRepository : IInternalTransferRepository
{
    private readonly AppDbContext _db;
    public InternalTransferRepository(AppDbContext db) => _db = db;

    public Task<InternalTransfer?> GetByIdAsync(Guid id)
        => _db.InternalTransfers
            .Include(t => t.ReceiverAccount).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<InternalTransfer?> GetByReferenceAsync(string reference)
        => _db.InternalTransfers.FirstOrDefaultAsync(t => t.Reference == reference);

    public async Task<IEnumerable<InternalTransfer>> GetBySenderAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.InternalTransfers
            .Include(t => t.ReceiverAccount).ThenInclude(a => a.User)
            .Where(t => t.SenderAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<IEnumerable<InternalTransfer>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.InternalTransfers
            .Include(t => t.ReceiverAccount).ThenInclude(a => a.User)
            .Where(t => t.SenderAccountId == accountId || t.ReceiverAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(InternalTransfer transfer) => await _db.InternalTransfers.AddAsync(transfer);
    public Task UpdateAsync(InternalTransfer transfer) { _db.InternalTransfers.Update(transfer); return Task.CompletedTask; }
}

public class TransferScheduleRepository : ITransferScheduleRepository
{
    private readonly AppDbContext _db;
    public TransferScheduleRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<TransferSchedule>> GetDueSchedulesAsync(DateTime asOf)
        => await _db.TransferSchedules
            .Where(s => s.IsActive && s.NextRunAt <= asOf)
            .ToListAsync();

    public async Task<IEnumerable<TransferSchedule>> GetByAccountIdAsync(Guid accountId)
        => await _db.TransferSchedules
            .Where(s => s.SenderAccountId == accountId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public Task<TransferSchedule?> GetByIdAsync(Guid id)
        => _db.TransferSchedules.FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(TransferSchedule schedule) => await _db.TransferSchedules.AddAsync(schedule);
    public Task UpdateAsync(TransferSchedule schedule) { _db.TransferSchedules.Update(schedule); return Task.CompletedTask; }
}

public class SavedBeneficiaryRepository : ISavedBeneficiaryRepository
{
    private readonly AppDbContext _db;
    public SavedBeneficiaryRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<SavedBeneficiary>> GetByOwnerAccountIdAsync(Guid ownerAccountId)
        => await _db.SavedBeneficiaries
            .Include(b => b.BeneficiaryAccount).ThenInclude(a => a.User)
            .Where(b => b.OwnerAccountId == ownerAccountId)
            .ToListAsync();

    public Task<SavedBeneficiary?> GetByIdAsync(Guid id)
        => _db.SavedBeneficiaries.FirstOrDefaultAsync(b => b.Id == id);

    public Task<bool> ExistsAsync(Guid ownerAccountId, Guid beneficiaryAccountId)
        => _db.SavedBeneficiaries.AnyAsync(b =>
            b.OwnerAccountId == ownerAccountId && b.BeneficiaryAccountId == beneficiaryAccountId);

    public async Task AddAsync(SavedBeneficiary beneficiary) => await _db.SavedBeneficiaries.AddAsync(beneficiary);

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.SavedBeneficiaries.FindAsync(id);
        if (entity is not null) _db.SavedBeneficiaries.Remove(entity);
    }
}

// ── 6.4 External Transfer ─────────────────────────────────────────────────────

public class ExternalTransferRepository : IExternalTransferRepository
{
    private readonly AppDbContext _db;
    public ExternalTransferRepository(AppDbContext db) => _db = db;

    public Task<ExternalTransfer?> GetByIdAsync(Guid id)
        => _db.ExternalTransfers.FirstOrDefaultAsync(t => t.Id == id);

    public Task<ExternalTransfer?> GetByReferenceAsync(string reference)
        => _db.ExternalTransfers.FirstOrDefaultAsync(t => t.Reference == reference);

    public async Task<IEnumerable<ExternalTransfer>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.ExternalTransfers
            .Where(t => t.SenderAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<decimal> GetSevenDayAverageAsync(Guid walletId)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var transfers = await _db.ExternalTransfers
            .Where(t => t.SenderWalletId == walletId && t.CreatedAt >= since)
            .ToListAsync();
        return transfers.Any() ? transfers.Average(t => t.Amount) : 0m;
    }

    public async Task AddAsync(ExternalTransfer transfer) => await _db.ExternalTransfers.AddAsync(transfer);
    public Task UpdateAsync(ExternalTransfer transfer) { _db.ExternalTransfers.Update(transfer); return Task.CompletedTask; }
}

public class SavedBankAccountRepository : ISavedBankAccountRepository
{
    private readonly AppDbContext _db;
    public SavedBankAccountRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<SavedBankAccount>> GetByAccountIdAsync(Guid accountId)
        => await _db.SavedBankAccounts.Where(b => b.AccountId == accountId).ToListAsync();

    public Task<SavedBankAccount?> GetByIdAsync(Guid id)
        => _db.SavedBankAccounts.FirstOrDefaultAsync(b => b.Id == id);

    public async Task AddAsync(SavedBankAccount bankAccount) => await _db.SavedBankAccounts.AddAsync(bankAccount);

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.SavedBankAccounts.FindAsync(id);
        if (entity is not null) _db.SavedBankAccounts.Remove(entity);
    }
}

// ── 6.2 Bills ─────────────────────────────────────────────────────────────────

public class BillPaymentRepository : IBillPaymentRepository
{
    private readonly AppDbContext _db;
    public BillPaymentRepository(AppDbContext db) => _db = db;

    public Task<BillPayment?> GetByIdAsync(Guid id) => _db.BillPayments.FirstOrDefaultAsync(b => b.Id == id);
    public Task<BillPayment?> GetByReferenceAsync(string ref_) => _db.BillPayments.FirstOrDefaultAsync(b => b.Reference == ref_);

    public async Task<IEnumerable<BillPayment>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.BillPayments
            .Where(b => b.AccountId == accountId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(BillPayment bill) => await _db.BillPayments.AddAsync(bill);
    public Task UpdateAsync(BillPayment bill) { _db.BillPayments.Update(bill); return Task.CompletedTask; }
}

// ── 6.5 Virtual Accounts ──────────────────────────────────────────────────────

public class VirtualAccountRepository : IVirtualAccountRepository
{
    private readonly AppDbContext _db;
    public VirtualAccountRepository(AppDbContext db) => _db = db;

    public Task<VirtualAccount?> GetByIdAsync(Guid id) => _db.VirtualAccounts.FirstOrDefaultAsync(v => v.Id == id);
    public Task<VirtualAccount?> GetByAccountNumberAsync(string number) => _db.VirtualAccounts.FirstOrDefaultAsync(v => v.AccountNumber == number);

    public Task<VirtualAccount?> GetPrimaryByAccountIdAsync(Guid accountId)
        => _db.VirtualAccounts.FirstOrDefaultAsync(v => v.AccountId == accountId && v.Label == null && v.IsActive);

    public async Task<IEnumerable<VirtualAccount>> GetByAccountIdAsync(Guid accountId)
        => await _db.VirtualAccounts.Where(v => v.AccountId == accountId).ToListAsync();

    public async Task AddAsync(VirtualAccount va) => await _db.VirtualAccounts.AddAsync(va);
    public Task UpdateAsync(VirtualAccount va) { _db.VirtualAccounts.Update(va); return Task.CompletedTask; }
}

// ── 6.6 Withdrawals ───────────────────────────────────────────────────────────

public class WithdrawalRepository : IWithdrawalRepository
{
    private readonly AppDbContext _db;
    public WithdrawalRepository(AppDbContext db) => _db = db;

    public Task<Withdrawal?> GetByIdAsync(Guid id) => _db.Withdrawals.FirstOrDefaultAsync(w => w.Id == id);
    public Task<Withdrawal?> GetByReferenceAsync(string ref_) => _db.Withdrawals.FirstOrDefaultAsync(w => w.Reference == ref_);

    public async Task<IEnumerable<Withdrawal>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.Withdrawals
            .Where(w => w.AccountId == accountId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<decimal> GetSevenDayAverageAsync(Guid walletId)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var withdrawals = await _db.Withdrawals
            .Where(w => w.WalletId == walletId && w.CreatedAt >= since)
            .ToListAsync();
        return withdrawals.Any() ? withdrawals.Average(w => w.Amount) : 0m;
    }

    public async Task AddAsync(Withdrawal w) => await _db.Withdrawals.AddAsync(w);
    public Task UpdateAsync(Withdrawal w) { _db.Withdrawals.Update(w); return Task.CompletedTask; }
}

// ── 6.7 Virtual Cards ─────────────────────────────────────────────────────────

public class VirtualCardRepository : IVirtualCardRepository
{
    private readonly AppDbContext _db;
    public VirtualCardRepository(AppDbContext db) => _db = db;

    public Task<VirtualCard?> GetByIdAsync(Guid id) => _db.VirtualCards.FirstOrDefaultAsync(c => c.Id == id);
    public Task<VirtualCard?> GetByProviderCardIdAsync(string id) => _db.VirtualCards.FirstOrDefaultAsync(c => c.ProviderCardId == id);

    public async Task<IEnumerable<VirtualCard>> GetByAccountIdAsync(Guid accountId)
        => await _db.VirtualCards.Where(c => c.AccountId == accountId).ToListAsync();

    public async Task AddAsync(VirtualCard card) => await _db.VirtualCards.AddAsync(card);
    public Task UpdateAsync(VirtualCard card) { _db.VirtualCards.Update(card); return Task.CompletedTask; }
}

public class CardTransactionRepository : ICardTransactionRepository
{
    private readonly AppDbContext _db;
    public CardTransactionRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<CardTransaction>> GetByCardIdAsync(Guid cardId, int page = 1, int pageSize = 20)
        => await _db.CardTransactions
            .Where(t => t.VirtualCardId == cardId)
            .OrderByDescending(t => t.TransactedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(CardTransaction txn) => await _db.CardTransactions.AddAsync(txn);
}

// ── 6.8 Gift Cards ────────────────────────────────────────────────────────────

public class GiftCardPurchaseRepository : IGiftCardPurchaseRepository
{
    private readonly AppDbContext _db;
    public GiftCardPurchaseRepository(AppDbContext db) => _db = db;

    public Task<GiftCardPurchase?> GetByIdAsync(Guid id) => _db.GiftCardPurchases.FirstOrDefaultAsync(g => g.Id == id);
    public Task<GiftCardPurchase?> GetByReferenceAsync(string ref_) => _db.GiftCardPurchases.FirstOrDefaultAsync(g => g.Reference == ref_);

    public async Task<IEnumerable<GiftCardPurchase>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.GiftCardPurchases
            .Where(g => g.AccountId == accountId)
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(GiftCardPurchase g) => await _db.GiftCardPurchases.AddAsync(g);
    public Task UpdateAsync(GiftCardPurchase g) { _db.GiftCardPurchases.Update(g); return Task.CompletedTask; }
}

// ── 6.9 Flights ───────────────────────────────────────────────────────────────

public class FlightBookingRepository : IFlightBookingRepository
{
    private readonly AppDbContext _db;
    public FlightBookingRepository(AppDbContext db) => _db = db;

    public Task<FlightBooking?> GetByIdAsync(Guid id)
        => _db.FlightBookings.Include(b => b.Passengers).FirstOrDefaultAsync(b => b.Id == id);

    public Task<FlightBooking?> GetByReferenceAsync(string ref_)
        => _db.FlightBookings.FirstOrDefaultAsync(b => b.BookingReference == ref_);

    public async Task<IEnumerable<FlightBooking>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.FlightBookings
            .Where(b => b.AccountId == accountId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(FlightBooking booking) => await _db.FlightBookings.AddAsync(booking);
    public Task UpdateAsync(FlightBooking booking) { _db.FlightBookings.Update(booking); return Task.CompletedTask; }
}

// ── 6.10 Bet Funding ──────────────────────────────────────────────────────────

public class BetFundingRepository : IBetFundingRepository
{
    private readonly AppDbContext _db;
    public BetFundingRepository(AppDbContext db) => _db = db;

    public Task<BetFunding?> GetByIdAsync(Guid id) => _db.BetFundings.FirstOrDefaultAsync(f => f.Id == id);
    public Task<BetFunding?> GetByReferenceAsync(string ref_) => _db.BetFundings.FirstOrDefaultAsync(f => f.Reference == ref_);

    public async Task<IEnumerable<BetFunding>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
        => await _db.BetFundings
            .Where(f => f.AccountId == accountId)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(BetFunding f) => await _db.BetFundings.AddAsync(f);
    public Task UpdateAsync(BetFunding f) { _db.BetFundings.Update(f); return Task.CompletedTask; }
}
