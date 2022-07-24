namespace Ellie.Services.Currency;

public record class TxData(
    string Type,
    string Extra,
    string? Note = "",
    ulong? OtherId = null);