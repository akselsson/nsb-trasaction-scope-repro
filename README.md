# Repro of NServiceBus outbox issue

From the [official documentation](https://docs.particular.net/persistence/sql/outbox#transaction-type-transaction-scope):

> In this mode the SQL persistence creates a TransactionScope that wraps the whole message processing attempt and within that scope it opens a connection [...]

However, when the outbox is configured with `UseTrasactionScope()`, the `System.Trasactions.Transaction.Current` is `null`.

## How to run

1. From the root of the repository, run `docker-compose up -d`
2. Wait for RabbitMQ and MSSQL server to start
3. Run `dotnet run --project .\App\App.csproj`