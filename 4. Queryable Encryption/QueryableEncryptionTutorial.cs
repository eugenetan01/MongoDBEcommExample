using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace QueryableEncryption;

public static class QueryableEncryptionTutorial
{
    public static async void RunExample()
    {
        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);

        // start-setup-application-variables
        // KMS provider name should be one of the following: "aws", "gcp", "azure", "kmip" or "local"
        const string kmsProviderName = "local";
        const string keyVaultDatabaseName = "encryption";
        const string keyVaultCollectionName = "__keyVault";
        var keyVaultNamespace =
            CollectionNamespace.FromFullName($"{keyVaultDatabaseName}.{keyVaultCollectionName}");
        const string encryptedDatabaseName = "UserRecords";
        const string encryptedCollectionName = "users";

        var appSettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var uri = appSettings["MongoDbUri"];
        // end-setup-application-variables

        var qeHelpers = new QueryableEncryptionHelpers(appSettings);
        var kmsProviderCredentials = qeHelpers.GetKmsProviderCredentials(kmsProviderName,
            generateNewLocalKey: true);

        // start-create-client
        var clientSettings = MongoClientSettings.FromConnectionString(uri);
        clientSettings.AutoEncryptionOptions = qeHelpers.GetAutoEncryptionOptions(
            keyVaultNamespace,
            kmsProviderCredentials);
        var encryptedClient = new MongoClient(clientSettings);
        // end-create-client

        var keyDatabase = encryptedClient.GetDatabase(keyVaultDatabaseName);

        // Drop the collection in case you created it in a previous run of this application.
        keyDatabase.DropCollection(keyVaultCollectionName);

        // start-encrypted-fields-map
        var encryptedFields = new BsonDocument
        {
            {
                "fields", new BsonArray
                {
                    new BsonDocument
                    {
                        { "keyId", BsonNull.Value },
                        { "path", "record.ssn" },
                        { "bsonType", "string" },
                        { "queries", new BsonDocument("queryType", "equality") }
                    },
                    new BsonDocument
                    {
                        { "keyId", BsonNull.Value },
                        { "path", "record.billing.cardNumber" },
                        { "bsonType", "long" },
                        { "queries", new BsonDocument("queryType", "equality") }
                    }
                }
            }
        };

        // end-encrypted-fields-map

        var userDatabase = encryptedClient.GetDatabase(encryptedDatabaseName);
        userDatabase.DropCollection(encryptedCollectionName);

        var clientEncryption = qeHelpers.GetClientEncryption(encryptedClient,
            keyVaultNamespace,
            kmsProviderCredentials);

        var customerMasterKeyCredentials = qeHelpers.GetCustomerMasterKeyCredentials(kmsProviderName);

        try
        {
            // start-create-encrypted-collection
            var createCollectionOptions = new CreateCollectionOptions<User>
            {
                EncryptedFields = encryptedFields
            };

            clientEncryption.CreateEncryptedCollection(userDatabase,
                encryptedCollectionName,
                createCollectionOptions,
                kmsProviderName,
                customerMasterKeyCredentials);
            // end-create-encrypted-collection
        }
        catch (Exception e)
        {
            throw new Exception("Unable to create encrypted collection due to the following error: " + e.Message);
        }

        // start-insert-document
        var user = new User
        {
            Name = "Jon Doe",
            Id = new ObjectId(),
            Record = new UserRecord
            {
                Ssn = "987-65-4320",
                Billing = new UserBilling
                {
                    CardType = "Visa",
                    CardNumber = 4111111111111111
                }
            }
        };

        var encryptedCollection = encryptedClient.GetDatabase(encryptedDatabaseName).
            GetCollection<User>(encryptedCollectionName);

        encryptedCollection.InsertOne(user);

        Console.WriteLine(user.Record.Billing.CardNumber);
        // end-insert-document

        // start-find-document
        var ccFilter = Builders<User>.Filter.Eq("record.billing.cardNumber", user.Record.Billing.CardNumber);
        try
        {
            var findResult = encryptedCollection.Find(ccFilter).ToList();

            Console.WriteLine(findResult.FirstOrDefault().ToJson());

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        // end-find-document
    }
}
