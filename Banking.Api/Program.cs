using Banking.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(serviceProvider =>
{
    var backendPath = Environment.GetEnvironmentVariable("COBOL_BACKEND_PATH");
    if (string.IsNullOrWhiteSpace(backendPath))
    {
        throw new InvalidOperationException(
            "Set COBOL_BACKEND_PATH to the compiled COBOL backend executable.");
    }

    return CobolBankClient.Start(
        backendPath,
        serviceProvider.GetRequiredService<ILogger<CobolBankClient>>());
});

var app = builder.Build();

app.MapGet("/balance", (CobolBankClient bank) =>
    Results.Ok(new BalanceResponse(bank.GetBalance())));

app.MapPost("/deposit", (AmountRequest? request, CobolBankClient bank) =>
{
    if (request is null || !IsValidAmount(request.Amount))
    {
        return Results.BadRequest(new ErrorResponse(
            $"Amount must be greater than zero, no more than {CobolBankClient.MaximumAmount:0.00}, and have at most two decimal places."));
    }

    var result = bank.Deposit(request.Amount);
    return result.Success
        ? Results.Ok(new TransactionResponse("DEPOSIT", result.Amount, result.Balance))
        : Results.Conflict(new ErrorResponse(result.Message));
});

app.MapPost("/withdraw", (AmountRequest? request, CobolBankClient bank) =>
{
    if (request is null || !IsValidAmount(request.Amount))
    {
        return Results.BadRequest(new ErrorResponse(
            $"Amount must be greater than zero, no more than {CobolBankClient.MaximumAmount:0.00}, and have at most two decimal places."));
    }

    var result = bank.Withdraw(request.Amount);
    return result.Success
        ? Results.Ok(new TransactionResponse("WITHDRAW", result.Amount, result.Balance))
        : Results.Conflict(new ErrorResponse(result.Message));
});

app.MapGet("/history", (CobolBankClient bank) =>
    Results.Ok(new HistoryResponse(bank.GetHistory())));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static bool IsValidAmount(decimal amount) =>
    amount > 0 &&
    amount <= CobolBankClient.MaximumAmount &&
    decimal.Round(amount, 2) == amount;

public sealed record AmountRequest(decimal Amount);
public sealed record BalanceResponse(decimal Balance);
public sealed record ErrorResponse(string Error);
public sealed record TransactionResponse(
    string Type,
    decimal Amount,
    decimal BalanceAfter);
public sealed record HistoryResponse(
    IReadOnlyList<TransactionResponse> Transactions);
public sealed record TransactionResult(
    bool Success,
    string Message,
    decimal Amount,
    decimal Balance);
