using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace HOST.Models
{
    [BsonIgnoreExtraElements]
    public class curriculum
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        public string menu_name { get; set; }
        public string date { get; set; }
        public string chef { get; set; }

        public List<MenuCategory> categories { get; set; } = new();
        public List<MenuDocument> documents { get; set; } = new();

        public string last_updated { get; set; }
    }



    public class MenuCategory
    {
        public string category_name { get; set; }
        public List<MenuItem> items { get; set; } = new();
    }

    public class MenuItem
    {
        public string item_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<string> ingredients { get; set; } = new();
        public int calories { get; set; }
        public double price { get; set; }

        // ⭐ REQUIRED for grouping AI output
        public string category { get; set; }
    }

    public class MenuDocument
    {
        public string title { get; set; }
        public string file_url { get; set; }
    }
}
