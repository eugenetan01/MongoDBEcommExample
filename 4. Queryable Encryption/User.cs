namespace QueryableEncryption;

// start-user
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

[BsonIgnoreExtraElements]
public class User
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public UserRecord Record { get; set; }
}
// end-user