using System.Diagnostics;
using System.Globalization;

namespace Banking.Api;

public sealed class CobolBankClient : IDisposable
{
    public const decimal MaximumAmount = 999_999_999.99m;

    private readonly Process _process;
    private readonly StreamWriter _input;
    private readonly StreamReader _output;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private readonly ILogger<CobolBankClient> _logger;
    private decimal _balance;

    private CobolBankClient(Process process, ILogger<CobolBankClient> logger)
    {
        _process = process;
        _input = process.StandardInput;
        _output = process.StandardOutput;
        _logger = logger;

        var startup = _output.ReadLine();
        if (startup is null || !startup.StartsWith("START|", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"COBOL backend did not start correctly. Response: {startup ?? "<no response>"}");
        }

        _balance = ParseMoney(startup["START|".Length..]);
    }

    public static CobolBankClient Start(
        string executablePath,
        ILogger<CobolBankClient> logger)
    {
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                "The COBOL backend executable was not found.", executablePath);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                logger.LogError("COBOL backend: {BackendError}", eventArgs.Data);
            }
        };

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Failed to start the COBOL backend.");
        }

        process.BeginErrorReadLine();
        try
        {
            return new CobolBankClient(process, logger);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            process.Dispose();
            throw;
        }
    }

    public decimal GetBalance()
    {
        _commandLock.Wait();
        try
        {
            var response = SendCommand("BALANCE");
            if (!response.StartsWith("BALANCE|", StringComparison.Ordinal))
            {
                throw UnexpectedResponse(response);
            }

            _balance = ParseMoney(response["BALANCE|".Length..]);
            return _balance;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public TransactionResult Deposit(decimal amount) =>
        SendTransaction("DEPOSIT", amount);

    public TransactionResult Withdraw(decimal amount) =>
        SendTransaction("WITHDRAW", amount);

    public IReadOnlyList<TransactionResponse> GetHistory()
    {
        _commandLock.Wait();
        try
        {
            var header = SendCommand("HISTORY");
            var headerFields = header.Split('|');
            if (headerFields.Length != 2 ||
                headerFields[0] != "HISTORY" ||
                !int.TryParse(headerFields[1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out var count))
            {
                throw UnexpectedResponse(header);
            }

            var transactions = new List<TransactionResponse>(count);
            for (var index = 0; index < count; index++)
            {
                var line = ReadResponse();
                var fields = line.Split('|');
                if (fields.Length != 4 || fields[0] != "ITEM")
                {
                    throw UnexpectedResponse(line);
                }

                transactions.Add(new TransactionResponse(
                    fields[1],
                    ParseMoney(fields[2]),
                    ParseMoney(fields[3])));
            }

            var end = ReadResponse();
            if (end != "END")
            {
                throw UnexpectedResponse(end);
            }

            return transactions;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    private TransactionResult SendTransaction(string action, decimal amount)
    {
        _commandLock.Wait();
        try
        {
            var response = SendCommand(
                $"{action}|{amount.ToString("0.00", CultureInfo.InvariantCulture)}");
            var fields = response.Split('|');

            if (fields.Length >= 2 && fields[0] == "RESULT" &&
                fields[1] == "INSUFFICIENT_FUNDS")
            {
                if (fields.Length != 3)
                {
                    throw UnexpectedResponse(response);
                }

                _balance = ParseMoney(fields[2]);
                return new TransactionResult(
                    false, "Insufficient funds.", amount, _balance);
            }

            if (fields.Length >= 2 && fields[0] == "RESULT" &&
                fields[1] == "BALANCE_LIMIT")
            {
                if (fields.Length != 3)
                {
                    throw UnexpectedResponse(response);
                }

                _balance = ParseMoney(fields[2]);
                return new TransactionResult(
                    false, "The account balance limit would be exceeded.", amount, _balance);
            }

            if (fields.Length == 2 && fields[0] == "RESULT" &&
                fields[1] == "HISTORY_FULL")
            {
                return new TransactionResult(
                    false, "Transaction history is full.", amount, _balance);
            }

            if (fields.Length != 5 ||
                fields[0] != "RESULT" ||
                fields[1] != "OK" ||
                fields[2] != action)
            {
                throw UnexpectedResponse(response);
            }

            var acceptedAmount = ParseMoney(fields[3]);
            _balance = ParseMoney(fields[4]);
            return new TransactionResult(
                true, $"{action[..1]}{action[1..].ToLowerInvariant()} completed.",
                acceptedAmount, _balance);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    private string SendCommand(string command)
    {
        if (_process.HasExited)
        {
            throw new InvalidOperationException(
                $"COBOL backend exited with code {_process.ExitCode}.");
        }

        _input.WriteLine(command);
        _input.Flush();
        return ReadResponse();
    }

    private string ReadResponse() =>
        _output.ReadLine() ??
        throw new InvalidOperationException("COBOL backend closed its output unexpectedly.");

    private static decimal ParseMoney(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number,
                CultureInfo.InvariantCulture, out var amount))
        {
            throw new InvalidOperationException(
                $"COBOL backend returned invalid money value: {value}");
        }

        return amount;
    }

    private static InvalidOperationException UnexpectedResponse(string response) =>
        new($"Unexpected response from COBOL backend: {response}");

    public void Dispose()
    {
        _commandLock.Wait();
        try
        {
            if (!_process.HasExited)
            {
                _input.WriteLine("EXIT");
                _input.Flush();
                _process.WaitForExit(2000);
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }

            _process.Dispose();
        }
        finally
        {
            _commandLock.Release();
            _commandLock.Dispose();
        }
    }
}
