# COBOL Banking Application

A menu-driven C# console client talks to an ASP.NET Core REST API. The API sends
commands to one long-running GnuCOBOL process, which owns the account balance,
validates transactions, and stores successful transactions in memory.

The initial account balance is **₱5,000.00**. The COBOL backend keeps up to
1,000 successful transactions for the lifetime of the API process. Restarting
the API starts a new account and clears transaction history.

## Requirements

- .NET 9 SDK
- GnuCOBOL (`cobc`) available on `PATH`

## Build and run

From the repository root, compile the COBOL backend:

```powershell
cobc -x -free -o Banking.Backend\bank-backend.exe Banking.Backend\bank.cob
```

Start the API in one terminal:

```powershell
$env:COBOL_BACKEND_PATH = (Resolve-Path .\Banking.Backend\bank-backend.exe).Path
dotnet run --project .\Banking.Api\Banking.Api.csproj --urls http://localhost:5080
```

Start the interactive menu client in another terminal:

```powershell
dotnet run --project .\Banking.Client\Banking.Client.csproj
```

The client also accepts a custom API address through `BANKING_API_URL`.

## API

- `GET /balance` — current balance
- `POST /deposit` — JSON body: `{ "amount": 250.00 }`
- `POST /withdraw` — JSON body: `{ "amount": 100.00 }`
- `GET /history` — successful deposits and withdrawals, in order
- `GET /health` — API health check

Amounts must be greater than zero, up to ₱999,999,999.99, and have at most
two decimal places. Withdrawals above the available balance are rejected.
Invalid amounts return HTTP 400. Insufficient funds, a full history, or an
account-balance overflow return HTTP 409. Invalid menu selections and amounts
are reported by the client, which continues to accept input.
