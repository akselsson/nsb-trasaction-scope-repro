using System;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Data.SqlClient;
using NServiceBus;

namespace App
{
    public static class Program
    {
        private static async Task Main()
        {
            var endpointConfiguration = new EndpointConfiguration("App");
            var rabbitMq = endpointConfiguration.UseTransport<RabbitMQTransport>();
            rabbitMq.UseConventionalRoutingTopology().ConnectionString("host=localhost");
            endpointConfiguration.EnableInstallers();
            var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
            persistence.ConnectionBuilder(() => new SqlConnection(
                "Data Source=localhost;Initial Catalog=tempdb;User ID=sa;Password=SQL_strong_password_123!"));
            persistence.SqlDialect<SqlDialect.MsSqlServer>();

            endpointConfiguration.EnableOutbox().UseTransactionScope();

            var startableEndpoint = await Endpoint.Create(endpointConfiguration);
            var endpointInstance = await startableEndpoint.Start();

            while (true)
            {
                await endpointInstance.Publish<IExampleEvent>(e => { });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public interface IExampleEvent : IEvent { }

    public class ExampleEventHandler : IHandleMessages<IExampleEvent>
    {
        public Task Handle(IExampleEvent message, IMessageHandlerContext context)
        {
            if (Transaction.Current == null)
            {
                Console.WriteLine("No current transaction found");
            }
            else
            {
                Console.WriteLine("Current transaction found");
            }

            return Task.CompletedTask;
        }
    }
}
