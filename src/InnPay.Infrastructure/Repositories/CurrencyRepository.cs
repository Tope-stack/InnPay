using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;
using InnPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnPay.Infrastructure.Repositories
{

    // ─── FX RATE ─────────────────────────────────────────────────────────────────

    public class FxRateRepository : IFxRateRepository
    {
        private readonly AppDbContext _db;
        public FxRateRepository(AppDbContext db) => _db = db;

        public Task<FxRate?> GetLatestAsync(WalletCurrency from, WalletCurrency to)
            => _db.FxRates
                  .Where(r => r.FromCurrency == from && r.ToCurrency == to)
                  .OrderByDescending(r => r.FetchedAt)
                  .FirstOrDefaultAsync();

        public async Task<IEnumerable<FxRate>> GetAllLatestAsync()
        {
            // Get the most recent rate per pair using a subquery
            var latest = await _db.FxRates
                .GroupBy(r => new { r.FromCurrency, r.ToCurrency })
                .Select(g => g.OrderByDescending(r => r.FetchedAt).First())
                .ToListAsync();

            return latest;
        }

        public async Task AddAsync(FxRate rate)
            => await _db.FxRates.AddAsync(rate);

        public async Task BulkAddAsync(IEnumerable<FxRate> rates)
            => await _db.FxRates.AddRangeAsync(rates);
    }

    // ─── FX RATE LOCK ────────────────────────────────────────────────────────────

    public class FxRateLockRepository : IFxRateLockRepository
    {
        private readonly AppDbContext _db;
        public FxRateLockRepository(AppDbContext db) => _db = db;

        public Task<FxRateLock?> GetByIdAsync(Guid lockId)
            => _db.FxRateLocks
                  .Include(l => l.FxRate)
                  .FirstOrDefaultAsync(l => l.Id == lockId);

        public Task<FxRateLock?> GetActiveByAccountAsync(Guid accountId, WalletCurrency from, WalletCurrency to)
            => _db.FxRateLocks.FirstOrDefaultAsync(l =>
                l.AccountId == accountId &&
                l.FromCurrency == from &&
                l.ToCurrency == to &&
                !l.IsUsed &&
                l.ExpiresAt > DateTime.UtcNow);

        public async Task AddAsync(FxRateLock rateLock)
            => await _db.FxRateLocks.AddAsync(rateLock);

        public Task UpdateAsync(FxRateLock rateLock)
        {
            _db.FxRateLocks.Update(rateLock);
            return Task.CompletedTask;
        }
    }

    // ─── FX TRANSACTION ──────────────────────────────────────────────────────────

    public class FxTransactionRepository : IFxTransactionRepository
    {
        private readonly AppDbContext _db;
        public FxTransactionRepository(AppDbContext db) => _db = db;

        public Task<FxTransaction?> GetByIdAsync(Guid id)
            => _db.FxTransactions
                  .Include(t => t.FromWallet)
                  .Include(t => t.ToWallet)
                  .FirstOrDefaultAsync(t => t.Id == id);

        public Task<FxTransaction?> GetByReferenceAsync(string reference)
            => _db.FxTransactions.FirstOrDefaultAsync(t => t.Reference == reference);

        public async Task<IEnumerable<FxTransaction>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20)
            => await _db.FxTransactions
                        .Where(t => t.AccountId == accountId)
                        .OrderByDescending(t => t.ConvertedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

        public async Task AddAsync(FxTransaction transaction)
            => await _db.FxTransactions.AddAsync(transaction);

        public Task UpdateAsync(FxTransaction transaction)
        {
            _db.FxTransactions.Update(transaction);
            return Task.CompletedTask;
        }
    }

    // ─── CURRENCY PAIR CONFIG ────────────────────────────────────────────────────

    public class CurrencyPairConfigRepository : ICurrencyPairConfigRepository
    {
        private readonly AppDbContext _db;
        public CurrencyPairConfigRepository(AppDbContext db) => _db = db;

        public Task<CurrencyPairConfig?> GetAsync(WalletCurrency from, WalletCurrency to)
            => _db.CurrencyPairConfigs.FirstOrDefaultAsync(c =>
                c.FromCurrency == from && c.ToCurrency == to && c.IsActive);

        public async Task<IEnumerable<CurrencyPairConfig>> GetAllActiveAsync()
            => await _db.CurrencyPairConfigs
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.FromCurrency).ThenBy(c => c.ToCurrency)
                        .ToListAsync();

        public async Task AddAsync(CurrencyPairConfig config)
            => await _db.CurrencyPairConfigs.AddAsync(config);

        public Task UpdateAsync(CurrencyPairConfig config)
        {
            _db.CurrencyPairConfigs.Update(config);
            return Task.CompletedTask;
        }
    }
}
