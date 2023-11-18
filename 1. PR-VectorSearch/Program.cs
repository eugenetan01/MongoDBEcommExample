using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using MongoDB.Bson;
using System.Net.Http.Json;
using MongoDB.Driver;
using System.Text.Json.Nodes;

class Program
{
    static string hfToken = "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";
    static string embeddingUrl = "https://api-inference.huggingface.co/pipeline/feature-extraction/sentence-transformers/all-MiniLM-L6-v2";

    static IMongoCollection<BsonDocument> collection;

    static void Main()
    {
        var client = new MongoClient("mongodb+srv://sa:admin@shengshiong.g3aer.mongodb.net/?retryWrites=true&w=majority");
        var database = client.GetDatabase("shengshiong");
        collection = database.GetCollection<BsonDocument>("product_catalogue");

        //LoadEmbeddingsToMongoDB();
        IssueQuery();
    }

    static List<float> GenerateEmbedding(string text)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);

            var response = httpClient.PostAsJsonAsync(embeddingUrl, new { inputs = text }).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Request failed with status code {response.StatusCode}: {response.Content.ReadAsStringAsync().Result}");
            }

            var jsonResult = response.Content.ReadAsStringAsync().Result;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<float>>(jsonResult);
        }
    }

    static void LoadEmbeddingsToMongoDB()
    {
        var documents = collection.Find(Builders<BsonDocument>.Filter.Exists("itemDescription")).ToList();

        foreach (var doc in documents)
        {
            var embeddingList = GenerateEmbedding(doc["itemDescription"].AsString);
            var embeddingArray = new BsonArray(embeddingList);

            doc["itemDesc_embeddings"] = embeddingArray;
            collection.ReplaceOne(Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]), doc);
        }
    }


    static void IssueQuery()
    {
        var query = "orange";
        var queryVector = GenerateEmbedding(query);

        double[] doubleArray = queryVector.ConvertAll(f => (double)f).ToArray();

        // Create System.ReadOnlyMemory<double> from the double array
        ReadOnlyMemory<Double> readOnlyMemory = new ReadOnlyMemory<Double>(doubleArray);


        //var filter = Builders<BsonDocument>.Filter.Exists("itemDesc_embeddings");
        //var projection = Builders<BsonDocument>.Projection.Include("itemDescription").Include("itemDesc_embeddings");


        BsonArray arr = new BsonArray();
        foreach (var value in doubleArray)
        {
            arr.Add(BsonValue.Create(value));
        }

        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(new[] 
        {
            new BsonDocument("$vectorSearch",
            new BsonDocument
                    {
                        { "queryVector", arr},
                        { "path", "itemDesc_embeddings" },
                        { "numCandidates", 100 },
                        { "limit", 4 },
                        { "index", "default" }
                    })
        } );

        var results = collection.Aggregate<BsonDocument>(pipeline).ToList();
        if (results != null)
        {
            foreach (var document in results)
            {
                var itemDescription = document["itemDescription"].AsString;
                //var itemDescEmbeddings = document["itemDesc_embeddings"].AsBsonArray;

                Console.WriteLine($"Product: {itemDescription}\n");
            }
        }
        
    }
}
