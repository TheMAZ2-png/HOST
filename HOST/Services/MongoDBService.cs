using HOST.Models;
using MongoDB.Driver;

namespace HOST.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<curriculum> _collection;
        private readonly IMongoCollection<PdfDocument> _pdfCollection;

        public MongoDBService(IConfiguration configuration)
        {
            var client = new MongoClient(
                configuration["MongoDBSettings:ConnectionString"]);

            var database = client.GetDatabase(
                configuration["MongoDBSettings:DatabaseName"]);

            _collection = database.GetCollection<curriculum>(
                configuration["MongoDBSettings:CollectionName"]);

            _pdfCollection = database.GetCollection<PdfDocument>(
                configuration["MongoDBSettings:PDFCollectionName"] ?? "pdf_documents");
        }

        public async Task<List<curriculum>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

        public async Task<curriculum> GetAsync(string id) =>
            await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(curriculum curriculum) =>
            await _collection.InsertOneAsync(curriculum);

        public async Task UpdateAsync(string id, curriculum curriculum) =>
            await _collection.ReplaceOneAsync(x => x.Id == id, curriculum);

        public async Task DeleteAsync(string id) =>
            await _collection.DeleteOneAsync(x => x.Id == id);

        // --- PDF Document methods ---
        public async Task CreatePdfDocumentAsync(PdfDocument doc) =>
            await _pdfCollection.InsertOneAsync(doc);

        public async Task ReplaceMenuAsync(curriculum menu)
        {
            await _collection.ReplaceOneAsync(
                x => x.Id == "SPECIALS_MENU",
                menu,
                new ReplaceOptions { IsUpsert = true }
            );
        }

    }
}

