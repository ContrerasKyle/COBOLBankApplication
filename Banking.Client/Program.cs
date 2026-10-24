using System.Globalization;
using System.Net.Http.Json;

var apiUrl = Environment.GetEnvironmentVariable("BANKING_API_URL")
    ?? "http://localhost:5080";
using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

Console.WriteLine("Welcome to the COBOL Banking System");
await ShowBalance();

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1. Check Balance");
    Console.WriteLine("2. Deposit Money");
    Console.WriteLine("3. Withdraw Money");
    Console.WriteLine("4. View Transaction History");
    Console.WriteLine("5. Exit");
    Console.Write("Select an option: ");

    var selection = Console.ReadLine();
    switch (selection)
    {
        case null:
            Console.WriteLine("Input closed. Goodbye.");
            return;
        case "1":
            await ShowBalance();
            break;
        case "2":
            await MakeTransaction("deposit", "Deposit");
            break;
        case "3":
            await MakeTransaction("withdraw", "Withdrawal");
            break;
        case "4":
            await ShowHistory();
            break;
        case "5":
            Console.WriteLine("Goodbye.");
            return;
        default:
            Console.WriteLine("Invalid menu selection. Choose 1 through 5.");
            break;
    }
}

async Task ShowBalance()
{
    using var response = await http.GetAsync("/balance");
    if (!response.IsSuccessStatusCode)
    {
        await ShowApiError(response);
        return;
    }

    var result = await response.Content.ReadFromJsonAsync<BalanceResponse>();
    Console.WriteLine($"Current balance: ₱{result?.Balance:N2}");
}

async Task MakeTransaction(string endpoint, string label)
{
    decimal amount;
    while (true)
    {
        Console.Write($"Enter amount to {endpoint}: ₱");
        var input = Console.ReadLine();
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out amount) &&
            amount > 0 &&
            amount <= 999_999_999.99m &&
            decimal.Round(amount, 2) == amount)
        {
            break;
        }

        Console.WriteLine(
            "Enter an amount greater than zero, no more than ₱999,999,999.99, with at most two decimal places.");
    }

    using var response = await http.PostAsJsonAsync($"/{endpoint}", new AmountRequest(amount));
    if (!response.IsSuccessStatusCode)
    {
        await ShowApiError(response);
        return;
    }

    var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
    if (result is not null)
    {
        Console.WriteLine($"{label} successful: ₱{result.Amount:N2}");
        Console.WriteLine($"Updated balance: ₱{result.BalanceAfter:N2}");
    }
}

async Task ShowHistory()
{
    using var response = await http.GetAsync("/history");
    if (!response.IsSuccessStatusCode)
    {
        await ShowApiError(response);
        return;
    }

    var result = await response.Content.ReadFromJsonAsync<HistoryResponse>();
    if (result is null || result.Transactions.Count == 0)
    {
        Console.WriteLine("No successful transactions yet.");
        return;
    }

    Console.WriteLine("Transaction History");
    foreach (var transaction in result.Transactions)
    {
        Console.WriteLine(
            $"{transaction.Type,-8} ₱{transaction.Amount,12:N2}  Balance: ₱{transaction.BalanceAfter:N2}");
    }
}

async Task ShowApiError(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Console.WriteLine(error?.Error ?? $"API request failed: {(int)response.StatusCode}.");
}

internal sealed record AmountRequest(decimal Amount);
internal sealed record BalanceResponse(decimal Balance);
internal sealed record TransactionResponse(
    string Type,
    decimal Amount,
    decimal BalanceAfter);
internal sealed record HistoryResponse(
    IReadOnlyList<TransactionResponse> Transactions);
internal sealed record ErrorResponse(string Error);
