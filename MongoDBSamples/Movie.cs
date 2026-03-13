using Microsoft.Extensions.VectorData;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDBSamples;

[BsonIgnoreExtraElements]
public record Movie(
    [property: BsonRepresentation(MongoDB.Bson.BsonType.ObjectId), VectorStoreKey()] string Id,
    [property: BsonElement("title")] string Title,
    [property: BsonElement("plot")] string Plot,
    [property: BsonElement("fullplot")] string FullPlot,
    [property: BsonElement("cast")] List<string> Cast,
    [property: BsonElement("genres")] List<string> Genres,
    [property: BsonElement("plot_embedding"), VectorStoreVector(1536)]
        ReadOnlyMemory<float> PlotEmbedding
);
