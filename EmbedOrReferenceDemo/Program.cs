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
using DataModellingDemo;


namespace EmbedOrReferenceDemo
{
    class Program
    {
        private static readonly Uri _endpointUri = new Uri("endpoint");
        private static readonly string _primaryKey = "key";
        public static async Task Main(string[] args)
        {
            var program = new Program();
            //await program.insertCustomersData_EmbeddedCollection();
            await program.query_EmbeddedCollection();
        }


        private async Task insertCustomersData_EmbeddedCollection()
        {
            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey))
            {
                await client.OpenAsync();
                Uri collectionLink = UriFactory.CreateDocumentCollectionUri("DatabaseEmbedded", "CustomersEmbedded");

                var companyNames = new[] { "Fabrikam", "Contoso", "Microsoft", "Acme" };

                var companies = new Bogus.Faker<Company>()
                    .RuleFor(i => i.companyName, (fake, i) => fake.PickRandom(companyNames))
                    .RuleFor(i => i.companyId, (fake, i) => fake.UniqueIndex)
                    .RuleFor(i => i.companyAddress, (fake, i) => fake.Address.FullAddress());

                var customers = new Bogus.Faker<DataModellingDemo.Person>()
                    .RuleFor(c => c.customerId, (fake, c) => fake.UniqueIndex)
                    .RuleFor(c => c.name, (fake) => fake.Name.FirstName())
                    .RuleFor(c => c.email, (fake, c) => fake.Internet.Email(c.name))
                    .RuleFor(c => c.number, (fake) => fake.Person.Phone)
                    .RuleFor(c => c.address, (fake) => fake.Address.FullAddress())
                    .RuleFor(c => c.company, (fake) => companies.Generate())
                    .Generate(100);

                foreach (var customer in customers)
                {
                    ResourceResponse<Document> result = await client.CreateDocumentAsync(collectionLink, customer);

                    await Console.Out.WriteLineAsync($"Document #{customers.IndexOf(customer):000} Created\t{result.Resource.Id}");

                }
            }
        }

        private async Task query_EmbeddedCollection()
        {
            Console.WriteLine("Starting...");
            var connectionPolicy = new ConnectionPolicy()
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };
            connectionPolicy.PreferredLocations.Add(LocationNames.WestUS2); // first preference

            // query for the customer info
            var queryCustomer = "SELECT c.name, c.number, c.customerId, c.company FROM c WHERE c.customerId = 12";

            FeedOptions feedOptions = new FeedOptions { };

            Uri collectionLinkCustomers = UriFactory.CreateDocumentCollectionUri("DatabaseEmbedded", "CustomersEmbedded");

            using (DocumentClient client = new DocumentClient(_endpointUri, _primaryKey, connectionPolicy: connectionPolicy))
            {
                await client.OpenAsync();
                while (true)
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    var customer = client.CreateDocumentQuery<dynamic>(collectionLinkCustomers, queryCustomer, feedOptions).ToList().FirstOrDefault();

                    sw.Stop();
                    Console.WriteLine($"Found id: {customer.customerId}, phone: {customer.number} for customer {customer.name} at company {customer.company.companyName}");

                    Console.WriteLine($"Read document in {sw.ElapsedMilliseconds} ms from {client.ReadEndpoint}");

                    Thread.Sleep(1000);
                }

            }
        }
    }
}


