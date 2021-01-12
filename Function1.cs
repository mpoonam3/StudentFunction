using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Host;
using StudentFunction.Models;
using Microsoft.Azure.Documents.Client;
using System.Linq;
using Microsoft.Azure.Documents;

namespace StudentFunction
{
    public static class Function1
    {
        [FunctionName("CosmosDb_GetAllDetails")]
        [Obsolete]
        public static IActionResult GetAllDetails(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetAllDetails")] HttpRequest req,
        [CosmosDB(databaseName: "Students", collectionName: "StudentDetails", ConnectionStringSetting = "CosmosDBConnection",
            SqlQuery = "SELECT * FROM c order by c._ts desc")]
        IEnumerable<StudentDetails> all,
        TraceWriter log)
        {
            log.Info("Getting all items");
            return new OkObjectResult(all);
        }

        [FunctionName("CosmosDb_GetDetailById")]
        [Obsolete]
        public static IActionResult GetDetailById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetDetailById/{id}")] HttpRequest req,
        //[CosmosDB(databaseName: "Students", collectionName: "StudentDetails", ConnectionStringSetting = "CosmosDBConnection", Id = "{id}")] StudentDetails item,
        [CosmosDB(ConnectionStringSetting = "CosmosDBConnection",PartitionKey ="department")] DocumentClient client,
        TraceWriter log, string id)
        {
            log.Info("Getting item by id");

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("Students", "StudentDetails");
            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            var document = client.CreateDocumentQuery(collectionUri, option).Where(t => t.Id == id)
                           .AsEnumerable().FirstOrDefault();
            
            //if (item == null)
            //{
            //    log.Info($"Item {id} not found");
            //    return new NotFoundResult();
            //}
            return new OkObjectResult(document);
        }

        [FunctionName("CosmosDb_CreateNew")]
        [Obsolete]
        public static async Task<IActionResult> CreateNew(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "CreateNew")] HttpRequest req,
        [CosmosDB( databaseName: "Students", collectionName: "StudentDetails", ConnectionStringSetting = "CosmosDBConnection")]
        IAsyncCollector<object> items, TraceWriter log)
        {
            log.Info("Creating a new item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<StudentDetails>(requestBody);

            //var item = new StudentDetails() { id = 4, firstName = "Poonam", lastName = "M", Gender = "F", DOB = "1992-12-12", department = "CS" };
            //await items.AddAsync(item);

            await items.AddAsync(new StudentDetails
            {
                id = input.id,
                firstName = input.firstName,
                lastName = input.lastName,
                department = input.department,
                DOB = input.DOB,
                Gender = input.Gender
            });
            return new OkObjectResult(input);
        }

        [FunctionName("CosmosDb_UpdateDetails")]
        [Obsolete]
        public static async Task<IActionResult> UpdateDetails(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "UpdateDetails/{id}")] HttpRequest req,
        [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")]
        DocumentClient client,
        TraceWriter log, string id)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<StudentDetails>(requestBody);
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("Students", "StudentDetails");
            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            var document = client.CreateDocumentQuery(collectionUri, option).Where(t => t.Id == id)
                            .AsEnumerable().FirstOrDefault();
            if (document == null)
            {
                return new NotFoundResult();
            }
            if (!string.IsNullOrEmpty(updated.department))
            {
                document.SetPropertyValue("department", updated.department);
            }

            await client.ReplaceDocumentAsync(document, new RequestOptions() { PartitionKey = new PartitionKey("department") } );
            StudentDetails details = (dynamic)document;

            return new OkObjectResult(details);
        }

        [FunctionName("CosmosDb_DeleteDetails")]
        [Obsolete]
        public static async Task<IActionResult> DeleteDetails(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "DeleteDetails/{id}")] HttpRequest req,
        [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
        TraceWriter log, string id)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("Students", "StudentDetails");
            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            var document = client.CreateDocumentQuery(collectionUri, option).Where(t => t.Id == id).AsEnumerable().FirstOrDefault();
            if (document == null)
            {
                return new NotFoundResult();
            }
            //
            await client.DeleteDocumentAsync(document.SelfLink, new RequestOptions() { PartitionKey = new PartitionKey("department") });
                      
            return new OkResult();
        }

    }
}
