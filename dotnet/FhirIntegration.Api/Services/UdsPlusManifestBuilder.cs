using FhirIntegration.Api.Models;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Builds the UDS+ Import Manifest: a FHIR Parameters resource conforming to
/// http://fhir.org/guides/hrsa/uds-plus/StructureDefinition/uds-plus-import-manifest.
///
/// Parameter names mirror the IG's Parameters-example (STU2):
///   inputFormat (code, fixed "fhir+ndjson"), submissionId, submissionTime, hcInformation{uei,
///   grantNumber, hcName, hcAddress, hcContactPerson, hcContactPhone, hcPrenatalCareByReferralOnly},
///   importProtocol (code, fixed "https"), and one udsData{type, url, expirationTime, bearer} per file.
/// </summary>
public sealed class UdsPlusManifestBuilder
{
    private readonly HealthCenterInfo _hc;

    public UdsPlusManifestBuilder(IOptions<UdsPlusOptions> options) => _hc = options.Value.HealthCenter;

    public sealed record ManifestInput(string ResourceType, string Url);

    public Parameters Build(string submissionId, DateTimeOffset submissionTime, IEnumerable<ManifestInput> inputs, string? bearerToken)
    {
        var manifest = new Parameters
        {
            Id = submissionId,
            Meta = new Meta { Profile = new[] { UdsPlusProfiles.ImportManifest } }
        };

        // Whole-second precision keeps the dateTime readable; FHIR allows but does not need sub-second digits.
        submissionTime = new DateTimeOffset(submissionTime.Year, submissionTime.Month, submissionTime.Day,
            submissionTime.Hour, submissionTime.Minute, submissionTime.Second, submissionTime.Offset);

        manifest.Add("inputFormat", new Code("fhir+ndjson"));
        manifest.Add("submissionId", new FhirString(submissionId));
        manifest.Add("submissionTime", new FhirDateTime(submissionTime));

        manifest.Parameter.Add(new Parameters.ParameterComponent
        {
            Name = "hcInformation",
            Part =
            {
                Part("uei", new FhirString(_hc.Uei)),
                Part("grantNumber", new FhirString(_hc.GrantNumber)),
                Part("hcName", new FhirString(_hc.Name)),
                Part("hcAddress", new Address
                {
                    Line = new[] { _hc.AddressLine },
                    City = _hc.City,
                    State = _hc.State,
                    PostalCode = _hc.PostalCode,
                    Country = "US"
                }),
                Part("hcContactPerson", new HumanName { Family = _hc.ContactFamily, Given = new[] { _hc.ContactGiven } }),
                Part("hcContactPhone", new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = _hc.ContactPhone }),
                Part("hcPrenatalCareByReferralOnly", new FhirBoolean(_hc.PrenatalCareByReferralOnly))
            }
        });

        manifest.Add("importProtocol", new Code("https"));

        var expiration = submissionTime.AddDays(7);
        foreach (var input in inputs)
        {
            var udsData = new Parameters.ParameterComponent
            {
                Name = "udsData",
                Part =
                {
                    Part("type", new Code(input.ResourceType)),
                    Part("url", new FhirUrl(input.Url)),
                    Part("expirationTime", new FhirDateTime(expiration))
                }
            };
            if (!string.IsNullOrEmpty(bearerToken)) udsData.Part.Add(Part("bearer", new FhirString(bearerToken)));
            manifest.Parameter.Add(udsData);
        }

        return manifest;
    }

    private static Parameters.ParameterComponent Part(string name, DataType value) => new() { Name = name, Value = value };
}
