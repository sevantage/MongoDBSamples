namespace MongoDBSamples;

public record Configuration(
    MongoDBConfiguration MongoDB,
    string Endpoint,
    string ChatDeployment,
    string EmbeddingDeployment = "text-embedding-ada-002"
);

public record MongoDBConfiguration(string ConnectionString, string Database = "sample_mflix");
