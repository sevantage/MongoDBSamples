using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBSamples.Services
{
    internal class MovieRepository
    {
        private readonly IMongoCollection<Movie> _movies;

        public MovieRepository(IMongoCollection<Movie> movies)
        {
            _movies = movies;
        }

        public async Task<Movie?> GetMovieByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return null;
            return await _movies.Find(m => m.Id == id).FirstOrDefaultAsync();
        }
    }
}
