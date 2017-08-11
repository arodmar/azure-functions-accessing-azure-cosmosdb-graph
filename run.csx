using System;
using System.Linq;
using System.Threading.Tasks;

// ADD THIS PART TO YOUR CODE
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Graphs;
using Newtonsoft.Json;


public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("Starting function code execution");
 // parse query parameter
    string command = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "command", true) == 0)
        .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    command = command ?? data?.command;

    var endpoint="https://{endpoint}:443/";
    var authKey="authkey";
    CosmosDBConnectionParameters connectionParams = new CosmosDBConnectionParameters
            {
                AuthKey = authKey,
                Endpoint = endpoint,
                CollectionName = "cmdb",
                DbName = "architecturecmdb"
            };
            
    CosmosDBGraphDBManager dbManager = new CosmosDBGraphDBManager { CosmosDBConnectionParameters = connectionParams };
    string queryResults = await dbManager.ExecuteCommandAsync(command);
    return req.CreateResponse(HttpStatusCode.OK, queryResults);

}

public class CosmosDBConnectionParameters
{
        public string Endpoint { get; set; }
        public string AuthKey { get; set; }
        public String DbName { get; set; }
        public string CollectionName { get; set; }
}

 public class CosmosDBGraphDBManager
    {
        public CosmosDBConnectionParameters CosmosDBConnectionParameters { get; set; }

        public CosmosDBGraphDBManager()
        { 


        }

        public async Task<string> ExecuteCommandAsync(string dbCommand)
        {
            using (DocumentClient client = new DocumentClient(new Uri(CosmosDBConnectionParameters.Endpoint ),
                CosmosDBConnectionParameters.AuthKey,
                new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }))
            {
                Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = CosmosDBConnectionParameters.DbName  });

                DocumentCollection graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri(CosmosDBConnectionParameters.DbName),
                    new DocumentCollection { Id = CosmosDBConnectionParameters.CollectionName  },
                    new RequestOptions { OfferThroughput = 1000 });

                var queryResults = String.Empty;
                IDocumentQuery<dynamic> query = client.CreateGremlinQuery<dynamic>(graph, dbCommand);

                while (query.HasMoreResults)
                {
                    foreach (dynamic result in await query.ExecuteNextAsync())
                    {
                        queryResults += JsonConvert.SerializeObject(result);
                    }
                }

                return queryResults;
            }
        }

    }

