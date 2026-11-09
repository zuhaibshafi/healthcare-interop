using System.Text.Json;
using System.Text.Json.Nodes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Thin wrapper around the Firely <see cref="FhirJsonSerializer"/> / <see cref="FhirJsonParser"/>.
///
/// Why a wrapper: the gateway deliberately passes FHIR payloads through as *strings* at the
/// HTTP boundary (so Swagger never has to describe the enormous FHIR object graph) and only
/// materialises Firely POCOs where it needs typed access. Centralising parser settings here
/// keeps behaviour consistent (permissive parsing, pretty output) across all endpoints.
/// </summary>
public sealed class FhirSerialization
{
    public const string FhirJsonMediaType = "application/fhir+json";
    public const string FhirNdjsonMediaType = "application/fhir+ndjson";

    private readonly FhirJsonParser _parser = new(new ParserSettings
    {
        // Be forgiving with upstream data: unknown elements / enum values are kept rather than rejected.
        AcceptUnknownMembers = true,
        AllowUnrecognizedEnums = true,
        PermissiveParsing = true
    });

    private readonly FhirJsonSerializer _pretty = new(new SerializerSettings { Pretty = true });
    private readonly FhirJsonSerializer _compact = new(new SerializerSettings { Pretty = false });

    public static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Parse JSON into a typed Firely resource. Throws <see cref="FormatException"/> on invalid FHIR.</summary>
    public T Parse<T>(string json) where T : Resource => _parser.Parse<T>(json);

    /// <summary>Parse JSON into whatever resource type it declares.</summary>
    public Resource Parse(string json) => _parser.Parse<Resource>(json);

    public string ToJson(Base resource, bool pretty = true) =>
        pretty ? _pretty.SerializeToString(resource) : _compact.SerializeToString(resource);

    /// <summary>Serialise a Firely resource into a mutable System.Text.Json tree (used for embedding in API responses).</summary>
    public JsonNode ToJsonNode(Base resource) => JsonNode.Parse(_compact.SerializeToString(resource))!;

    public static string PrettyPrint(JsonNode node) => node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
}
