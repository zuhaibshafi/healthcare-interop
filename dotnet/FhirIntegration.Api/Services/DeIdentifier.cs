using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Implements the UDS+ "Trust Service Provider" role: turns identifiable US Core resources into
/// de-identified resources conforming to the HRSA UDS+ IG (fhir.hrsa.uds-plus 2.0.0).
///
/// Design notes:
///  * The transformation runs on the System.Text.Json tree rather than on typed Firely POCOs.
///    One generic rule set (strip narrative, strip identifiers, strip reference display text,
///    drop references to people/organisations, pseudonymise Patient references) then applies to
///    any resource type without per-type code, and unknown/extension content round-trips untouched.
///  * Patient-specific rules follow the de-identified-uds-plus-patient profile: name, telecom,
///    identifier, birthDate, photo, contact, GP, managing organisation and link are prohibited;
///    age is carried in the uds-plus-age-extension (Quantity, years, ">= 90" for ages over 89);
///    the address is reduced to a 3-digit ZIP prefix (postalCode "NNN00").
///  * Patient ids are replaced by a stable pseudonym (SHA-256 of the source id). The same hash
///    is applied to every Patient reference so the linkage between a patient's resources survives.
///  * Every date / dateTime / instant anywhere in a resource (including extensions and Periods) is
///    truncated to the 4-digit year, as the UDS+ invariants require on Patient, Encounter, Condition,
///    Procedure, Immunization, MedicationRequest and AllergyIntolerance (uds-plus-diag-1/8, enc-3/4,
///    proc-2/3, imm-3, medreq-1, all-1). Observation is the one exception: its UDS+ profiles carry no
///    truncation constraint and base FHIR vs-1 requires vital-sign effective[x] at day precision.
///    Ages are computed from the full-precision values *before* truncation.
///  * Free text is removed: narrative, notes, Coding.display and CodeableConcept.text. Codes carry the
///    meaning UDS+ needs and free text is both a re-identification vector and a source of
///    terminology "wrong display" findings.
///  * Race / ethnicity are normalised to the UDS+ extension profiles, which keep the US Core extension
///    URLs but require the `detailed` (1..*) and `text` (1..1) sub-extensions.
///  * Profiles are assigned from a small resource-type -> canonical table (<see cref="UdsPlusProfiles"/>).
/// </summary>
public sealed partial class DeIdentifier
{
    public const string AgeExtensionUrl = UdsPlusProfiles.Base + "uds-plus-age-extension";
    private const string UcumSystem = "http://unitsofmeasure.org";
    private const string UsCoreRaceUrl = "http://hl7.org/fhir/us/core/StructureDefinition/us-core-race";
    private const string UsCoreEthnicityUrl = "http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity";
    private const string CdcRaceEthnicitySystem = "urn:oid:2.16.840.1.113883.6.238";
    private const string NullFlavorSystem = "http://terminology.hl7.org/CodeSystem/v3-NullFlavor";

    /// <summary>FHIR date / dateTime literals with at least month precision (year-only values are already compliant).</summary>
    [GeneratedRegex(@"^\d{4}-\d{2}(-\d{2}(T\d{2}:\d{2}(:\d{2}(\.\d+)?)?(Z|[+-]\d{2}:\d{2})?)?)?$")]
    private static partial Regex DateLiteral();

    /// <summary>Resource types whose date elements are left at full precision (see class remarks).</summary>
    private static readonly HashSet<string> DateTruncationExempt = new(StringComparer.Ordinal) { "Observation" };

    /// <summary>JSON keys whose string values are never dates (avoids touching codes, ids and URLs that happen to look like one).</summary>
    private static readonly HashSet<string> NonDateKeys = new(StringComparer.Ordinal)
    {
        "id", "url", "system", "reference", "code", "version", "display", "text", "div", "language", "unit", "resourceType", "fullUrl"
    };

    /// <summary>Code systems removed from codings because receivers/validators cannot resolve them (NDC product codes; CVX stays).</summary>
    private static readonly HashSet<string> DroppedCodingSystems = new(StringComparer.Ordinal)
    {
        "http://hl7.org/fhir/sid/ndc"
    };

    /// <summary>
    /// OMB race category -> a representative CDC "detailed race" code, used only when the source has no
    /// detailed race (UDS+ requires detailed 1..*). The derivation is recorded in the run notes.
    /// </summary>
    private static readonly Dictionary<string, (string Code, string Display)> DetailedRaceByOmb = new(StringComparer.Ordinal)
    {
        ["2106-3"] = ("2108-9", "European"),
        ["2054-5"] = ("2060-2", "African"),
        ["2028-9"] = ("2029-7", "Asian Indian"),
        ["1002-5"] = ("1004-1", "American Indian"),
        ["2076-8"] = ("2079-2", "Native Hawaiian"),
    };

    /// <summary>
    /// OMB ethnicity category -> detailed ethnicity. The US Core detailed-ethnicity value set only contains
    /// Hispanic/Latino sub-groups plus null flavours, so "Not Hispanic or Latino" can only be expressed with
    /// the UNK null flavour at the detailed level (the OMB category still carries the real value).
    /// </summary>
    private static readonly Dictionary<string, (string System, string Code, string Display)> DetailedEthnicityByOmb = new(StringComparer.Ordinal)
    {
        ["2135-2"] = (CdcRaceEthnicitySystem, "2148-5", "Mexican"),
        ["2186-5"] = (NullFlavorSystem, "UNK", "Unknown"),
    };

    /// <summary>Patient extensions that UDS+ permits/expects (kept verbatim; everything else on Patient is dropped).</summary>
    private static readonly HashSet<string> AllowedPatientExtensions = new(StringComparer.Ordinal)
    {
        "http://hl7.org/fhir/us/core/StructureDefinition/us-core-race",
        "http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity",
        "http://hl7.org/fhir/us/core/StructureDefinition/us-core-birthsex",
        "http://hl7.org/fhir/us/core/StructureDefinition/us-core-sex",
        "http://hl7.org/fhir/us/core/StructureDefinition/us-core-genderIdentity",
        UdsPlusProfiles.Base + "uds-plus-birthsex-extension",
        UdsPlusProfiles.Base + "uds-plus-sex-extension",
        UdsPlusProfiles.Base + "udsplus-agriculture-worker-status",
        UdsPlusProfiles.Base + "uds-plus-housing-status-extension",
        UdsPlusProfiles.Base + "uds-plus-veteran-status-extension",
    };

    /// <summary>Reference targets that identify people or organisations; the referencing element is removed.</summary>
    private static readonly HashSet<string> RemovedReferenceTargets = new(StringComparer.Ordinal)
    {
        "Practitioner", "PractitionerRole", "Organization", "RelatedPerson", "Person", "CareTeam", "Device"
    };

    /// <summary>Elements removed from every resource type (narrative and free text are re-identification risks).</summary>
    private static readonly string[] GlobalRemovals = { "text", "contained", "note", "identifier" };

    /// <summary>Per-type elements that carry people (performers, recorders, etc.). Removed wholesale.</summary>
    private static readonly Dictionary<string, string[]> TypeRemovals = new(StringComparer.Ordinal)
    {
        ["Patient"] = new[] { "name", "telecom", "photo", "contact", "generalPractitioner", "managingOrganization", "link", "maritalStatus", "multipleBirthBoolean", "multipleBirthInteger", "birthDate" },
        ["Encounter"] = new[] { "participant", "serviceProvider", "appointment", "account", "basedOn" },
        ["Condition"] = new[] { "recorder", "asserter" },
        ["Procedure"] = new[] { "performer", "recorder", "asserter", "report" },
        ["Observation"] = new[] { "performer", "issued" }, // issued is prohibited (max 0) by uds-plus-lab-observation
        ["Immunization"] = new[] { "performer", "manufacturer" },
        ["MedicationRequest"] = new[] { "requester", "recorder", "performer", "dispenseRequest", "insurance" },
        ["AllergyIntolerance"] = new[] { "recorder", "asserter" },
    };

    public sealed class Context
    {
        /// <summary>Date used to compute ages (the reporting date).</summary>
        public DateTime ReportDate { get; init; } = DateTime.UtcNow.Date;

        /// <summary>Source Patient id -> birthDate (yyyy, yyyy-MM or yyyy-MM-dd). Filled while processing Patients.</summary>
        public Dictionary<string, string?> BirthDates { get; } = new(StringComparer.Ordinal);

        public List<string> Notes { get; } = new();
    }

    /// <summary>Stable pseudonym for a Patient id: lower-case hex SHA-256 (64 chars, valid FHIR id).</summary>
    public static string Pseudonym(string patientId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(patientId))).ToLowerInvariant();

    /// <summary>
    /// De-identify one resource in place and return it. Patients must be processed before the
    /// resources that reference them so that <see cref="Context.BirthDates"/> is populated.
    /// </summary>
    public JsonObject DeIdentify(JsonObject resource, Context context)
    {
        var resourceType = resource["resourceType"]?.GetValue<string>() ?? throw new ArgumentException("resourceType missing");

        if (resourceType == "Patient")
        {
            DeIdentifyPatient(resource, context);
        }
        else
        {
            // Age-at-encounter is mandatory on uds-plus-encounter; compute before the subject is pseudonymised.
            if (resourceType == "Encounter") AddEncounterAge(resource, context);
        }

        foreach (var element in GlobalRemovals) resource.Remove(element);
        if (TypeRemovals.TryGetValue(resourceType, out var removals))
            foreach (var element in removals) resource.Remove(element);

        ApplyTypeSpecificShaping(resource, resourceType);
        Scrub(resource, truncateDates: !DateTruncationExempt.Contains(resourceType));

        resource["meta"] = new JsonObject { ["profile"] = new JsonArray(UdsPlusProfiles.ProfileFor(resource)) };
        return resource;
    }

    // ------------------------------------------------------------------ Patient

    private static void DeIdentifyPatient(JsonObject patient, Context context)
    {
        var sourceId = patient["id"]?.GetValue<string>() ?? throw new ArgumentException("Patient.id missing");
        var birthDate = patient["birthDate"]?.GetValue<string>();
        context.BirthDates[sourceId] = birthDate;

        patient["id"] = Pseudonym(sourceId);

        // Extensions: keep only the demographic extensions UDS+ expects, then add age.
        var kept = new JsonArray();
        if (patient["extension"] is JsonArray extensions)
        {
            foreach (var ext in extensions.OfType<JsonObject>().ToList())
            {
                var url = ext["url"]?.GetValue<string>();
                if (url is not null && AllowedPatientExtensions.Contains(url))
                {
                    extensions.Remove(ext);
                    kept.Add(ext);
                }
            }
        }
        NormaliseRaceEthnicity(kept, sourceId, context);

        var age = ComputeAgeYears(birthDate, context.ReportDate);
        if (age is not null) kept.Add(AgeExtension(age.Value));
        else context.Notes.Add($"Patient {sourceId}: no birthDate, age extension omitted.");
        patient["extension"] = kept;

        // Address: the profile keeps address (1..1) only as a carrier for the 3-digit ZIP prefix;
        // line/text are prohibited, so the whole address collapses to {postalCode}.
        var postalCode = (patient["address"] as JsonArray)?.OfType<JsonObject>()
            .Select(a => a["postalCode"]?.GetValue<string>())
            .FirstOrDefault(z => !string.IsNullOrWhiteSpace(z));
        patient.Remove("address");
        patient["address"] = new JsonArray(new JsonObject { ["postalCode"] = MaskZip(postalCode) });

        // Deceased: year precision only (profile invariant uds-plus-pat-1).
        if (patient["deceasedDateTime"]?.GetValue<string>() is { Length: >= 4 } deceased)
            patient["deceasedDateTime"] = deceased[..4];
    }

    /// <summary>
    /// Ensures the race and ethnicity extensions satisfy uds-plus-race-extension / uds-plus-ethnicity-extension:
    /// extension present (1..1), ombCategory kept from the source, `detailed` 1..* and `text` 1..1 present.
    /// Missing detailed codes are derived from the OMB category (or a null flavour); missing extensions are
    /// added with null flavours so the profile's cardinalities hold without inventing demographics.
    /// </summary>
    private static void NormaliseRaceEthnicity(JsonArray extensions, string sourceId, Context context)
    {
        EnsureDemographicExtension(extensions, UsCoreRaceUrl, "race", sourceId, context,
            omb => DetailedRaceByOmb.TryGetValue(omb, out var d) ? (CdcRaceEthnicitySystem, d.Code, d.Display) : null);
        EnsureDemographicExtension(extensions, UsCoreEthnicityUrl, "ethnicity", sourceId, context,
            omb => DetailedEthnicityByOmb.TryGetValue(omb, out var d) ? d : null);
    }

    private static void EnsureDemographicExtension(JsonArray extensions, string url, string label, string sourceId, Context context,
        Func<string, (string System, string Code, string Display)?> detailedFromOmb)
    {
        var ext = extensions.OfType<JsonObject>().FirstOrDefault(e => e["url"]?.GetValue<string>() == url);
        if (ext is null)
        {
            ext = new JsonObject { ["url"] = url, ["extension"] = new JsonArray() };
            extensions.Add(ext);
            context.Notes.Add($"Patient {sourceId}: no {label} extension in source; added with null flavour UNK.");
        }

        var subs = ext["extension"] as JsonArray;
        if (subs is null) ext["extension"] = subs = new JsonArray();

        var ombCodes = subs.OfType<JsonObject>()
            .Where(s => s["url"]?.GetValue<string>() == "ombCategory")
            .Select(s => (s["valueCoding"] as JsonObject)?["code"]?.GetValue<string>())
            .Where(c => c is not null).Cast<string>().ToList();
        var ombTexts = subs.OfType<JsonObject>()
            .Where(s => s["url"]?.GetValue<string>() == "ombCategory")
            .Select(s => (s["valueCoding"] as JsonObject)?["display"]?.GetValue<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t)).Cast<string>().ToList();

        if (!subs.OfType<JsonObject>().Any(s => s["url"]?.GetValue<string>() == "detailed"))
        {
            var derived = ombCodes.Select(detailedFromOmb).Where(d => d is not null).Select(d => d!.Value).Distinct().ToList();
            if (derived.Count == 0) derived.Add((NullFlavorSystem, "UNK", "Unknown"));
            foreach (var (system, code, display) in derived)
            {
                subs.Add(new JsonObject
                {
                    ["url"] = "detailed",
                    ["valueCoding"] = new JsonObject { ["system"] = system, ["code"] = code, ["display"] = display }
                });
            }
            context.Notes.Add($"Patient {sourceId}: {label} 'detailed' derived from OMB category ({string.Join(", ", derived.Select(d => d.Code))}).");
        }

        if (!subs.OfType<JsonObject>().Any(s => s["url"]?.GetValue<string>() == "text"))
        {
            subs.Add(new JsonObject { ["url"] = "text", ["valueString"] = ombTexts.Count > 0 ? string.Join(", ", ombTexts) : "Unknown" });
        }
    }

    /// <summary>Truncates a FHIR date/dateTime/instant literal to its 4-digit year.</summary>
    public static string TruncateToYear(string value) => value.Length >= 4 ? value[..4] : value;

    /// <summary>Profile-driven cardinality / terminology adjustments that are not simple removals.</summary>
    private static void ApplyTypeSpecificShaping(JsonObject resource, string resourceType)
    {
        switch (resourceType)
        {
            case "Condition":
                // uds-plus-diagnosis: category is 1..1 (US Core allows many); keep the first.
                if (resource["category"] is JsonArray { Count: > 1 } categories)
                {
                    var first = categories[0]!.DeepClone();
                    resource["category"] = new JsonArray(first);
                }
                break;

            case "Immunization":
                // Keep CVX; drop NDC product codings that terminology services cannot validate.
                if (resource["vaccineCode"] is JsonObject vaccineCode && vaccineCode["coding"] is JsonArray codings)
                {
                    for (var i = codings.Count - 1; i >= 0; i--)
                    {
                        var system = (codings[i] as JsonObject)?["system"]?.GetValue<string>();
                        if (system is not null && DroppedCodingSystems.Contains(system) && codings.Count > 1) codings.RemoveAt(i);
                    }
                }
                break;
        }
    }

    /// <summary>UDS+ rule: postalCode must match ^[0-9]{3}00(-[0-9]{4})?$ or be masked as 00000.</summary>
    public static string MaskZip(string? postalCode)
    {
        var digits = new string((postalCode ?? "").Where(char.IsDigit).ToArray());
        return digits.Length >= 3 ? digits[..3] + "00" : "00000";
    }

    /// <summary>Age in whole years at <paramref name="asOf"/>; null if birthDate is absent/unparseable.</summary>
    public static int? ComputeAgeYears(string? birthDate, DateTime asOf)
    {
        if (string.IsNullOrWhiteSpace(birthDate)) return null;
        var parts = birthDate.Split('-');
        if (!int.TryParse(parts[0], out var y)) return null;
        var m = parts.Length > 1 && int.TryParse(parts[1], out var mm) ? mm : 1;
        var d = parts.Length > 2 && int.TryParse(parts[2][..Math.Min(2, parts[2].Length)], out var dd) ? dd : 1;
        var dob = new DateTime(y, Math.Clamp(m, 1, 12), Math.Clamp(d, 1, 28));
        var age = asOf.Year - dob.Year;
        if (asOf < dob.AddYears(age)) age--;
        return Math.Max(age, 0);
    }

    /// <summary>uds-plus-age-extension: Quantity in UCUM years; ages over 89 are reported as ">= 90" (HIPAA safe harbour).</summary>
    public static JsonObject AgeExtension(int ageYears)
    {
        var quantity = new JsonObject
        {
            ["value"] = ageYears > 89 ? 90 : ageYears,
            ["unit"] = "a",
            ["system"] = UcumSystem,
            ["code"] = "a"
        };
        if (ageYears > 89) quantity["comparator"] = ">=";
        return new JsonObject { ["url"] = AgeExtensionUrl, ["valueQuantity"] = quantity };
    }

    // ------------------------------------------------------------------ Encounter

    private static void AddEncounterAge(JsonObject encounter, Context context)
    {
        var subject = (encounter["subject"] as JsonObject)?["reference"]?.GetValue<string>();
        var patientId = ParseReference(subject).id;
        if (patientId is null || !context.BirthDates.TryGetValue(patientId, out var birthDate)) return;

        var start = (encounter["period"] as JsonObject)?["start"]?.GetValue<string>();
        var asOf = DateTimeOffset.TryParse(start, out var dto) ? dto.Date : context.ReportDate;
        var age = ComputeAgeYears(birthDate, asOf);
        if (age is null) return;

        var extensions = encounter["extension"] as JsonArray ?? new JsonArray();
        encounter.Remove("extension");
        extensions.Add(AgeExtension(age.Value));
        encounter["extension"] = extensions;
    }

    // ------------------------------------------------------------------ generic reference scrubbing

    /// <summary>
    /// Walks the whole tree and applies the generic rules:
    ///  * Reference: drop "display", rewrite Patient references to the pseudonym, signal removal for
    ///    references to people/organisations;
    ///  * Coding: drop "display"; CodeableConcept: drop "text" (free text);
    ///  * when <paramref name="truncateDates"/> is set, any date / dateTime / instant literal is truncated to the year;
    ///  * containers left empty are removed.
    /// </summary>
    /// <returns>true when the node should be removed by its parent.</returns>
    private static bool Scrub(JsonNode? node, bool truncateDates)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["reference"] is JsonValue refValue && refValue.TryGetValue<string>(out var reference))
                {
                    var (type, id) = ParseReference(reference);
                    obj.Remove("display");
                    if (type is not null && RemovedReferenceTargets.Contains(type)) return true;
                    if (type == "Patient" && id is not null) obj["reference"] = $"Patient/{Pseudonym(id)}";
                    return false;
                }

                // Coding {system, code, display} -> drop display; CodeableConcept {coding, text} -> drop text.
                if (obj.ContainsKey("code") && obj.ContainsKey("system") && obj["code"] is JsonValue) obj.Remove("display");
                if (obj["coding"] is JsonArray) obj.Remove("text");

                foreach (var key in obj.Select(kv => kv.Key).ToList())
                {
                    var child = obj[key];
                    if (truncateDates && child is JsonValue leaf && leaf.TryGetValue<string>(out var s) && !NonDateKeys.Contains(key) && DateLiteral().IsMatch(s))
                    {
                        obj[key] = TruncateToYear(s);
                        continue;
                    }
                    if (Scrub(child, truncateDates)) obj.Remove(key);
                    else if (child is JsonArray { Count: 0 } || child is JsonObject { Count: 0 }) obj.Remove(key);
                }
                return false;

            case JsonArray array:
                for (var i = array.Count - 1; i >= 0; i--)
                {
                    if (Scrub(array[i], truncateDates)) array.RemoveAt(i);
                }
                return false;

            default:
                return false;
        }
    }

    /// <summary>Parses "Type/id", absolute URLs ".../Type/id[/_history/v]" and "urn:uuid:..." references.</summary>
    public static (string? type, string? id) ParseReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.StartsWith("urn:") || reference.StartsWith('#')) return (null, null);
        var segments = reference.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var historyIndex = Array.IndexOf(segments, "_history");
        if (historyIndex > 1) segments = segments[..historyIndex];
        return segments.Length >= 2 ? (segments[^2], segments[^1]) : (null, null);
    }
}

/// <summary>Mapping table from FHIR resource type to the UDS+ profile canonical it must declare.</summary>
public static class UdsPlusProfiles
{
    public const string Base = "http://fhir.org/guides/hrsa/uds-plus/StructureDefinition/";
    public const string ImportManifest = Base + "uds-plus-import-manifest";

    public static readonly IReadOnlyDictionary<string, string> ByResourceType = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Patient"] = Base + "de-identified-uds-plus-patient",
        ["Encounter"] = Base + "uds-plus-encounter",
        ["Condition"] = Base + "uds-plus-diagnosis",
        ["Procedure"] = Base + "uds-plus-procedure",
        ["Observation"] = Base + "uds-plus-observation",
        ["Immunization"] = Base + "uds-plus-immunization",
        ["MedicationRequest"] = Base + "uds-plus-medicationrequest",
        ["AllergyIntolerance"] = Base + "uds-plus-allergyintolerance",
        ["Coverage"] = Base + "uds-plus-coverage",
        ["ServiceRequest"] = Base + "uds-plus-servicerequest",
        ["MedicationStatement"] = Base + "uds-plus-medicationstatement",
        ["Location"] = Base + "uds-plus-location",
    };

    /// <summary>
    /// Observation is split by category in UDS+: laboratory results use uds-plus-lab-observation,
    /// everything else (vital signs, assessments, social history) uses uds-plus-observation.
    /// </summary>
    public static string ProfileFor(JsonObject resource)
    {
        var type = resource["resourceType"]!.GetValue<string>();
        if (type == "Observation")
        {
            var categories = (resource["category"] as JsonArray)?
                .OfType<JsonObject>()
                .SelectMany(c => (c["coding"] as JsonArray)?.OfType<JsonObject>() ?? Enumerable.Empty<JsonObject>())
                .Select(c => c["code"]?.GetValue<string>())
                .ToList() ?? new List<string?>();
            if (categories.Contains("laboratory")) return Base + "uds-plus-lab-observation";
        }
        return ByResourceType.TryGetValue(type, out var profile) ? profile : Base + "uds-plus-" + type.ToLowerInvariant();
    }
}
