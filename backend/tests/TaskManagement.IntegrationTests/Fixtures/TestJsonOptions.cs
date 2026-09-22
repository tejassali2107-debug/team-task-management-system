using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManagement.IntegrationTests.Fixtures;

public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
