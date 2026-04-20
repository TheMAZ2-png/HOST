using HOST.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HOST.Models
{
    [BsonIgnoreExtraElements]
    public class PdfDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string ExtractedText { get; set; } = string.Empty;

        public string AiDigestedJson { get; set; } = string.Empty;

        public curriculum? ParsedCurriculum { get; set; }
    }
}