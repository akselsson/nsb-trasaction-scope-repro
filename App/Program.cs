using System;
using System.Data;
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
            persistence.ConnectionBuilder(() => new SqlConnection(SqlHelper.ConnectionString));
            persistence.SqlDialect<SqlDialect.MsSqlServer>();

            endpointConfiguration.UnitOfWork().WrapHandlersInATransactionScope();
            endpointConfiguration.EnableOutbox().UseTransactionScope();

            var startableEndpoint = await Endpoint.Create(endpointConfiguration);
            var endpointInstance = await startableEndpoint.Start();

            await SqlHelper.ExecuteNonQuery(@"IF OBJECT_ID (N'test_table', N'U') IS NOT NULL 
            drop table test_table;");
            await SqlHelper.ExecuteNonQuery("create table test_table (id int identity primary key, message_id int)");

            int counter = 0;
            while (true)
            {
                await endpointInstance.Publish<IExampleEvent>(e => { e.MessageId = counter++; });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public interface IExampleEvent : IEvent
    {
        int MessageId { get; set; }
    }

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


    public class SqlHandler : IHandleMessages<IExampleEvent>
    {
        public async Task Handle(IExampleEvent message, IMessageHandlerContext context)
        {
            await SqlHelper.ExecuteNonQuery($"insert into test_table (message_id) values({message.MessageId})");

            //Uncomment this line to commit the sql insert
            throw new Exception("This should roll back the previous query");
        }
    }

    public static class SqlHelper
    {
        public const string ConnectionString = "Data Source=localhost;Initial Catalog=test;User ID=sa;Password=SQL_strong_password_123!";

        public static async Task ExecuteNonQuery(string query)
        {
            await using var connection = new SqlConnection(
                ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }
}
