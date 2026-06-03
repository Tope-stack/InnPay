using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services
{
    public class CurrencyPairConfigService : ICurrencyPairConfigService
    {
        private readonly IUnitOfWork _uow;

        public CurrencyPairConfigService(IUnitOfWork uow) => _uow = uow;

        public async Task<ServiceResult<List<CurrencyPairConfigResponse>>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
        {
            var configs = await _uow.CurrencyPairConfigs.GetAllActiveAsync();
            return ServiceResult<List<CurrencyPairConfigResponse>>.Success(
                configs.Select(MapToDto).ToList());
        }

        public async Task<ServiceResult<CurrencyPairConfigResponse>> UpsertConfigAsync(UpsertCurrencyPairConfigRequest request, CancellationToken cancellationToken = default)
        {
            if (request.FromCurrency == request.ToCurrency)
                return ServiceResult<CurrencyPairConfigResponse>.Fail("Source and target currencies must differ.");

            if (request.MinSourceAmount > 0 && request.MaxSourceAmount > 0
                && request.MinSourceAmount > request.MaxSourceAmount)
                return ServiceResult<CurrencyPairConfigResponse>.Fail("Minimum amount cannot exceed maximum amount.");

            var existing = await _uow.CurrencyPairConfigs.GetAsync(request.FromCurrency, request.ToCurrency);

            if (existing is not null)
            {
                existing.SpreadBps = request.SpreadBps;
                existing.FlatFee = request.FlatFee;
                existing.PercentageFee = request.PercentageFee;
                existing.MinSourceAmount = request.MinSourceAmount;
                existing.MaxSourceAmount = request.MaxSourceAmount;
                existing.IsActive = request.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                await _uow.CurrencyPairConfigs.UpdateAsync(existing);
                await _uow.SaveChangesAsync(cancellationToken);
                return ServiceResult<CurrencyPairConfigResponse>.Success(MapToDto(existing));
            }

            var config = new CurrencyPairConfig
            {
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                SpreadBps = request.SpreadBps,
                FlatFee = request.FlatFee,
                PercentageFee = request.PercentageFee,
                MinSourceAmount = request.MinSourceAmount,
                MaxSourceAmount = request.MaxSourceAmount,
                IsActive = request.IsActive
            };

            await _uow.CurrencyPairConfigs.AddAsync(config);
            await _uow.SaveChangesAsync(cancellationToken);
            return ServiceResult<CurrencyPairConfigResponse>.Success(MapToDto(config), 201);
        }

        private static CurrencyPairConfigResponse MapToDto(CurrencyPairConfig c) => new()
        {
            Id = c.Id,
            FromCurrency = c.FromCurrency,
            ToCurrency = c.ToCurrency,
            PairLabel = c.PairLabel,
            SpreadBps = c.SpreadBps,
            FlatFee = c.FlatFee,
            PercentageFee = c.PercentageFee,
            MinSourceAmount = c.MinSourceAmount,
            MaxSourceAmount = c.MaxSourceAmount,
            IsActive = c.IsActive
        };
    }
}
