using Bogus;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataModellingDemo
{
    class Program
    {
        private static readonly Uri _endpointUri = new Uri("");
        private static readonly string _primaryKey = "key";
        public static async Task Main(string[] args)
        {
            var program = new Program();
            //await program.insertOrdersData_SeparateCollection();
            //await program.insertOrdersData_SameCollection();
            //await program.query_SeparateCollection();
            await program.query_SameCollection();
        }

        private async Task insertOrdersData_SeparateCollection()
        {
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();
                Uri collectionLinkCustomers = UriFactory.CreateDocumentCollectionUri("Database1", "Customers");

                var orderIds = 0;
                var items = new[] { "hamburger", "cheeseburger", "salad", "orange"};
            
                var customers = new Bogus.Faker<Person>()
                    .RuleFor(i => i.customerId, (fake, i) => fake.UniqueIndex)
                    .RuleFor(i => i.name, (fake) => fake.Name.FirstName())
                    .RuleFor(i => i.email, (fake, i) => fake.Internet.Email(i.name))
                    .RuleFor(i => i.number, (fake) => fake.Person.Phone)
                    .RuleFor(i => i.address, (fake) => fake.Address.FullAddress())
                    .Generate(100);

                var itemsOrdered = new Bogus.Faker<ItemsOrdered>()
                    .RuleFor(i => i.itemName, (fake, i) => fake.PickRandom(items))
                    .RuleFor(i => i.price, (fake, i) => fake.Commerce.Price())
                    .RuleFor(i => i.qty, (fake, i) => fake.Random.Number(1, 10));
                    //.Generate(100);

                var orders = new Bogus.Faker<Order>()
                    .RuleFor(i => i.customerId, (fake, i) => fake.PickRandom(customers).customerId)
                    .RuleFor(i => i.orderId, (fake) => orderIds++)
                    .RuleFor(i => i.orderDate, (fake, i) => fake.Date.Recent())
                    .RuleFor(u => u.itemsOrdered, fake => itemsOrdered.Generate(3).ToList())
                    .Generate(100);
                foreach (var customer in customers)
                {
                    ResourceResponse<Document> result = await client.CreateDocumentAsync(collectionLinkCustomers, customer);

                    await Console.Out.WriteLineAsync($"Customer #{customers.IndexOf(customer):000} Created\t{result.Resource.Id}");

                }

                Uri collectionLinkOrders = UriFactory.CreateDocumentCollectionUri("Database1", "Orders");
                foreach (var order in orders)
                {
                    ResourceResponse<Document> result = await client.CreateDocumentAsync(collectionLinkOrders, order);

                    await Console.Out.WriteLineAsync($"Order #{orders.IndexOf(order):000} Created\t{result.Resource.Id}");

                }
            }
        }

        private async Task insertOrdersData_SameCollection()
        {
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();
                Uri collectionLink = UriFactory.CreateDocumentCollectionUri("Database2", "CustomersAndOrders");

                var orderIds = 0;
                var items = new[] { "hamburger", "cheeseburger", "salad", "orange" };

                var customers = new Bogus.Faker<Person>()
                    .RuleFor(c => c.customerId, (fake, c) => fake.UniqueIndex)
                    .RuleFor(c => c.name, (fake) => fake.Name.FirstName())
                    .RuleFor(c => c.email, (fake, c) => fake.Internet.Email(c.name))
                    .RuleFor(c => c.number, (fake) => fake.Person.Phone)
                    .RuleFor(c => c.address, (fake) => fake.Address.FullAddress())
                    .RuleFor(c => c.type, fake => "customer")
                    .Generate(100);

                var itemsOrdered = new Bogus.Faker<ItemsOrdered>()
                    .RuleFor(i => i.itemName, (fake, i) => fake.PickRandom(items))
                    .RuleFor(i => i.price, (fake, i) => fake.Commerce.Price())
                    .RuleFor(i => i.qty, (fake, i) => fake.Random.Number(1, 10));

                var orders = new Bogus.Faker<Order>()
                    .RuleFor(o => o.customerId, (fake, o) => fake.PickRandom(customers).customerId)
                    .RuleFor(o => o.orderId, (fake) => orderIds++)
                    .RuleFor(o => o.orderDate, (fake, o) => fake.Date.Recent())
                    .RuleFor(o => o.itemsOrdered, fake => itemsOrdered.Generate(3).ToList())
                    .RuleFor(o => o.type, fake => "order")
                    .Generate(100);
                foreach (var customer in customers)
                {
                    ResourceResponse<Document> result = await client.CreateDocumentAsync(collectionLink, customer);

                    await Console.Out.WriteLineAsync($"Document #{customers.IndexOf(customer):000} Created\t{result.Resource.Id}");

                }

                Uri collectionLinkOrders = UriFactory.CreateDocumentCollectionUri("Database1", "Orders");
                foreach (var order in orders)
                {
                    ResourceResponse<Document> result = await client.CreateDocumentAsync(collectionLink, order);

                    await Console.Out.WriteLineAsync($"Document #{orders.IndexOf(order):000} Created\t{result.Resource.Id}");

                }
            }
        }

        private async Task query_SeparateCollection()
        {
            Console.WriteLine("Starting...");
            var connectionPolicy = new ConnectionPolicy()
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };
            connectionPolicy.PreferredLocations.Add(LocationNames.WestUS2); // first preference

            // query for the customer info
            var queryCustomer = "SELECT c.name, c.number, c.customerId FROM c WHERE c.customerId = 5";

            // then query for all the orders
            var queryCustomerOrders = "SELECT c.orderId, c.itemsOrdered FROM c WHERE c.customerId = 5";
            FeedOptions feedOptions = new FeedOptions
            {
            };

            Uri collectionLinkCustomers = UriFactory.CreateDocumentCollectionUri("Database1", "Customers");
            Uri collectionLinkOrders = UriFactory.CreateDocumentCollectionUri("Database1", "Orders");

            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey, connectionPolicy: connectionPolicy))
            {
                await client.OpenAsync();
                while (true)
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    var customer = client.CreateDocumentQuery<dynamic>(collectionLinkCustomers, queryCustomer, feedOptions).ToList().FirstOrDefault();

                    var orders = client.CreateDocumentQuery<dynamic>(collectionLinkOrders, queryCustomerOrders, feedOptions).ToList();//.FirstOrDefault();

                    sw.Stop();
                    Console.WriteLine($"Found {orders.Count()} orders for customer {customer.name}");

                    Console.WriteLine($"Read document in {sw.ElapsedMilliseconds} ms from {client.ReadEndpoint}");

                    Thread.Sleep(1000);
                }

            }
        }

        private async Task query_SameCollection()
        {
            Console.WriteLine("Starting...");
            var connectionPolicy = new ConnectionPolicy()
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };
            connectionPolicy.PreferredLocations.Add(LocationNames.WestUS2); // first preference

            // query for orders by filtering on type
            var queryOrders = "SELECT c.customerId, c.itemsOrdered FROM c WHERE c.customerId = 5 AND c.type = 'order'";

            FeedOptions feedOptions = new FeedOptions
            {
            };

            Uri collectionLink = UriFactory.CreateDocumentCollectionUri("Database2", "CustomersAndOrders");

            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey, connectionPolicy: connectionPolicy))
            {
                await client.OpenAsync();
                while (true)
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    var orders = client.CreateDocumentQuery<dynamic>(collectionLink, queryOrders, feedOptions).ToList();

                    sw.Stop();
                    Console.WriteLine($"Found {orders.Count()} orders for customer {orders.FirstOrDefault().customerId})");

                    Console.WriteLine($"Read document in {sw.ElapsedMilliseconds} ms from {client.ReadEndpoint}");

                    Thread.Sleep(1000);
                }

            }
        }

    }
}


