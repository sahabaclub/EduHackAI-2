using MongoDB.Bson;

namespace WebAppDemo.Models
{
    public class TeamUser
    {
        public ObjectId Id { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("username")]
        public string? Username { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("firstname")]
        public string? FirstName { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("lastname")]
        public string? LastName { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("password")]
        public string? Password { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("email")]
        public string? Email { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("teamname")]
        public string? TeamName { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("status")]
        public string? Status { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("role")]
        public string? Role { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("created")]
        public DateTime? Created { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("modified")]
        public DateTime? Modified { get; set; }
    }
}
