# Inferno results — UDS+ Test Kit (uds_plus)

- Inferno: http://localhost:4568  (session [9AiRiXmQCxS](http://localhost:4568/test_sessions/9AiRiXmQCxS))
- Scope: `uds_plus-uds_plus_test_group`
- Inputs: `import_manifest`=`http://localhost:5080/udsplus/files/20261108-193627-4855fa54942743f2b8ede889/man`
- Run status: done, duration 74 s

| Result | Count |
| --- | --- |
| pass | 8 |
| fail | 5 |
| omit | 10 |

## Per-test results

| Group | Test | Result | Message |
| --- | --- | --- | --- |
| Import Manifest Tests | Check For Invalid Types in Import Manifest | **pass** |  |
| Import Manifest Tests | Read the contents of the import manifest | **pass** |  |
| Import Manifest Tests | Receive UDS+ Import Manifest | **pass** |  |
| Import Manifest Tests | Validate UDS+ Adverse Event Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Allergy Intolerance Data | **fail** | Resource does not conform to the profile: http://fhir.org/guides/hrsa/uds-plus/StructureDefinition/uds-plus-allergyintolerance\|2.0.0 |
| Import Manifest Tests | Validate UDS+ Clinical Result Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Coverage Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Data Urls Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Diagnosis Data | **fail** | Resource does not conform to the profile: http://fhir.org/guides/hrsa/uds-plus/StructureDefinition/uds-plus-diagnosis\|2.0.0 |
| Import Manifest Tests | Validate UDS+ Encounter Data | **pass** |  |
| Import Manifest Tests | Validate UDS+ Immunization Data | **fail** | Resource does not conform to the profile: http://fhir.org/guides/hrsa/uds-plus/StructureDefinition/uds-plus-immunization\|2.0.0 |
| Import Manifest Tests | Validate UDS+ Income Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Lab Observation Data | **pass** |  |
| Import Manifest Tests | Validate UDS+ Location Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Import Manifest | **pass** |  |
| Import Manifest Tests | Validate UDS+ Medication Request Data | **pass** |  |
| Import Manifest Tests | Validate UDS+ Medication Statement Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Observation Data | **pass** |  |
| Import Manifest Tests | Validate UDS+ Patient Reporting Parameters Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Patient Data | **fail** | Resource does not conform to the profile: http://fhir.org/guides/hrsa/uds-plus/StructureDefinition/de-identified-uds-plus-patient\|2.0.0 |
| Import Manifest Tests | Validate UDS+ Procedure Data | **fail** | Resource does not conform to the profile: http://fhir.org/guides/hrsa/uds-plus/StructureDefinition/uds-plus-procedure\|2.0.0 |
| Import Manifest Tests | Validate UDS+ Related Person Data | **omit** | No data of this type was identified. |
| Import Manifest Tests | Validate UDS+ Service Request Data | **omit** | No data of this type was identified. |
