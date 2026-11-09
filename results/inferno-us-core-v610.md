# Inferno results — US Core Server v6.1.0 (us_core_v610)

- Inferno: http://localhost:4567  (session [9Xh45L7zxSC](http://localhost:4567/test_sessions/9Xh45L7zxSC))
- Scope: `us_core_v610-us_core_v610_fhir_api`
- Inputs: `url`=`http://localhost:8080/fhir`, `patient_ids`=`demo-patient-1,demo2-patient`, `smart_auth_info`=`{"auth_type":"public"}`
- Run status: done, duration 55 s

| Result | Count |
| --- | --- |
| pass | 358 |
| fail | 2 |
| skip | 93 |

## Per-test results

| Group | Test | Result | Message |
| --- | --- | --- | --- |
| US Core FHIR API / AllergyIntolerance Tests | All must support elements are provided in the AllergyIntolerance resources returned | **pass** |  |
| US Core FHIR API / AllergyIntolerance Tests | Server returns valid results for AllergyIntolerance search by patient + clinical-status | **pass** |  |
| US Core FHIR API / AllergyIntolerance Tests | Server returns valid results for AllergyIntolerance search by patient | **pass** |  |
| US Core FHIR API / AllergyIntolerance Tests | Server returns Provenance resources from AllergyIntolerance search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / AllergyIntolerance Tests | Server returns correct AllergyIntolerance resource from AllergyIntolerance read interaction | **pass** |  |
| US Core FHIR API / AllergyIntolerance Tests | MustSupport references within AllergyIntolerance resources are valid | **pass** |  |
| US Core FHIR API / AllergyIntolerance Tests | AllergyIntolerance resources returned during previous tests conform to the US Core AllergyIntolerance Profile | **pass** |  |
| US Core FHIR API / Capability Statement | FHIR server secured by transport layer security | **fail** | Server did not support any allowed TLS versions. |
| US Core FHIR API / Capability Statement | FHIR Server supports the conformance interaction | **pass** |  |
| US Core FHIR API / Capability Statement | Server is using FHIR R4 | **pass** |  |
| US Core FHIR API / Capability Statement | Server instantiates US Core Server | **fail** | Server CapabilityStatement.instantiates does not include 'http://hl7.org/fhir/us/core/CapabilityStatement/us-core-server' |
| US Core FHIR API / Capability Statement | FHIR server capability states JSON support | **pass** |  |
| US Core FHIR API / Capability Statement | Capability Statement lists support for required US Core Profiles | **pass** |  |
| US Core FHIR API / CarePlan Tests | All must support elements are provided in the CarePlan resources returned | **pass** |  |
| US Core FHIR API / CarePlan Tests | Server returns valid results for CarePlan search by patient + category + date | **pass** |  |
| US Core FHIR API / CarePlan Tests | Server returns valid results for CarePlan search by patient + category | **pass** |  |
| US Core FHIR API / CarePlan Tests | Server returns valid results for CarePlan search by patient + category + status + date | **pass** |  |
| US Core FHIR API / CarePlan Tests | Server returns valid results for CarePlan search by patient + category + status | **pass** |  |
| US Core FHIR API / CarePlan Tests | Server returns Provenance resources from CarePlan search by patient + category + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / CarePlan Tests | Server returns correct CarePlan resource from CarePlan read interaction | **pass** |  |
| US Core FHIR API / CarePlan Tests | MustSupport references within CarePlan resources are valid | **pass** |  |
| US Core FHIR API / CarePlan Tests | CarePlan resources returned during previous tests conform to the US Core CarePlan Profile | **pass** |  |
| US Core FHIR API / CareTeam Tests | All must support elements are provided in the CareTeam resources returned | **pass** |  |
| US Core FHIR API / CareTeam Tests | Server returns valid results for CareTeam search by patient + role | **pass** |  |
| US Core FHIR API / CareTeam Tests | Server returns valid results for CareTeam search by patient + status | **pass** |  |
| US Core FHIR API / CareTeam Tests | Server returns Provenance resources from CareTeam search by patient + status + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / CareTeam Tests | Server returns correct CareTeam resource from CareTeam read interaction | **pass** |  |
| US Core FHIR API / CareTeam Tests | MustSupport references within CareTeam resources are valid | **pass** |  |
| US Core FHIR API / CareTeam Tests | Server returns valid results for CareTeam search by role | **pass** |  |
| US Core FHIR API / CareTeam Tests | CareTeam resources returned during previous tests conform to the US Core CareTeam Profile | **pass** |  |
| US Core FHIR API / Clinical Notes Guidance | DiagnosticReport and DocumentReference reference the same attachment | **pass** |  |
| US Core FHIR API / Clinical Notes Guidance | Server demonstrates support for the required DocumentReference types and DiagnosticReport categories. | **skip** | Could not find these DocumentReference types: 11488-4, 18842-5, 34117-2, 28570-0, 11506-3, 18748-4, 11502-2, 11526-1 and DiagnosticReport categories LP29708-2, LP7839-6. |
| US Core FHIR API / Condition Encounter Diagnosis Tests | All must support elements are provided in the Condition resources returned | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + abatement-date | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + asserted-date | **skip** | No Condition resources appear to be available. Please use patients with more information |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + category + clinical-status | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + category + encounter | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + category | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + clinical-status | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + code | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + onset-date | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient + recorded-date | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns valid results for Condition search by patient | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns Provenance resources from Condition search by patient + category + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Server returns correct Condition resource from Condition read interaction | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | MustSupport references within Condition resources are valid | **pass** |  |
| US Core FHIR API / Condition Encounter Diagnosis Tests | Condition resources returned during previous tests conform to the US Core Condition Encounter Diagnosis Profile | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | All must support elements are provided in the Condition resources returned | **skip** | Could not find abatementDateTime, category:screening-assessment in the 4 provided resource(s) |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + abatement-date | **skip** | Could not find values for all search params `patient`, `abatement-date` |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + asserted-date | **skip** | No Condition resources appear to be available. Please use patients with more information |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + category + clinical-status | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + category + encounter | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + category | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + clinical-status | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + code | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + onset-date | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient + recorded-date | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns valid results for Condition search by patient | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns Provenance resources from Condition search by patient + category + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Server returns correct Condition resource from Condition read interaction | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | MustSupport references within Condition resources are valid | **pass** |  |
| US Core FHIR API / Condition Problems and Health Concerns Tests | Condition resources returned during previous tests conform to the US Core Condition Problems and Health Concerns Profile | **pass** |  |
| US Core FHIR API / Coverage Tests | All must support elements are provided in the Coverage resources returned | **pass** |  |
| US Core FHIR API / Coverage Tests | Server returns valid results for Coverage search by patient | **pass** |  |
| US Core FHIR API / Coverage Tests | Server returns Provenance resources from Coverage search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Coverage Tests | Server returns correct Coverage resource from Coverage read interaction | **pass** |  |
| US Core FHIR API / Coverage Tests | MustSupport references within Coverage resources are valid | **skip** | Could not resolve and validate any Must Support references for payor element: Reference(http://hl7.org/fhir/us/core/StructureDefinition/us-core-organization) |
| US Core FHIR API / Coverage Tests | Coverage resources returned during previous tests conform to the US Core Coverage Profile | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | All must support elements are provided in the DiagnosticReport resources returned | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | Server returns valid results for DiagnosticReport search by patient + category + date | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | Server returns valid results for DiagnosticReport search by patient + category | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | Server returns valid results for DiagnosticReport search by patient + code + date | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | Server returns valid results for DiagnosticReport search by patient + code | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | Server returns valid results for DiagnosticReport search by patient | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | Server returns valid results for DiagnosticReport search by patient + status | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | Server returns Provenance resources from DiagnosticReport search by patient + category + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | Server returns correct DiagnosticReport resource from DiagnosticReport read interaction | **pass** |  |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | MustSupport references within DiagnosticReport resources are valid | **skip** | Could not resolve and validate any Must Support references for performer element: Reference(http://hl7.org/fhir/us/core/StructureDefinition/us-core-practitioner) |
| US Core FHIR API / DiagnosticReport for Laboratory Results Reporting Tests | DiagnosticReport resources returned during previous tests conform to the US Core DiagnosticReport Profile for Laboratory Results Reporting | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | All must support elements are provided in the DiagnosticReport resources returned | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | Server returns valid results for DiagnosticReport search by patient + category + date | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | Server returns valid results for DiagnosticReport search by patient + category | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | Server returns valid results for DiagnosticReport search by patient + code + date | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | Server returns valid results for DiagnosticReport search by patient + code | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | Server returns valid results for DiagnosticReport search by patient | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | Server returns valid results for DiagnosticReport search by patient + status | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | Server returns Provenance resources from DiagnosticReport search by patient + category + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | Server returns correct DiagnosticReport resource from DiagnosticReport read interaction | **pass** |  |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | MustSupport references within DiagnosticReport resources are valid | **skip** | Could not resolve and validate any Must Support references for performer element: Reference(http://hl7.org/fhir/us/core/StructureDefinition/us-core-practitioner\|http://hl7.org/fhi |
| US Core FHIR API / DiagnosticReport for Report and Note Exchange Tests | DiagnosticReport resources returned during previous tests conform to the US Core DiagnosticReport Profile for Report and Note Exchange | **pass** |  |
| US Core FHIR API / DocumentReference Tests | DocumentReference resources returned during previous tests have custodian | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns valid results for DocumentReference search by _id | **pass** |  |
| US Core FHIR API / DocumentReference Tests | All must support elements are provided in the DocumentReference resources returned | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns valid results for DocumentReference search by patient + category + date | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns valid results for DocumentReference search by patient + category | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns valid results for DocumentReference search by patient | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns valid results for DocumentReference search by patient + status | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns valid results for DocumentReference search by patient + type + period | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns valid results for DocumentReference search by patient + type | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns Provenance resources from DocumentReference search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / DocumentReference Tests | Server returns correct DocumentReference resource from DocumentReference read interaction | **pass** |  |
| US Core FHIR API / DocumentReference Tests | MustSupport references within DocumentReference resources are valid | **pass** |  |
| US Core FHIR API / DocumentReference Tests | DocumentReference resources returned during previous tests conform to the US Core DocumentReference Profile | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by _id | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by class + patient | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by date + patient | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by identifier | **pass** |  |
| US Core FHIR API / Encounter Tests | All must support elements are provided in the Encounter resources returned | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by patient + discharge-disposition | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by patient + location | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by patient | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by patient + status | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns valid results for Encounter search by patient + type | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns Provenance resources from Encounter search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Encounter Tests | Server returns correct Encounter resource from Encounter read interaction | **pass** |  |
| US Core FHIR API / Encounter Tests | MustSupport references within Encounter resources are valid | **pass** |  |
| US Core FHIR API / Encounter Tests | Encounter resources returned during previous tests conform to the US Core Encounter Profile | **pass** |  |
| US Core FHIR API / Goal Tests | All must support elements are provided in the Goal resources returned | **pass** |  |
| US Core FHIR API / Goal Tests | Server returns valid results for Goal search by patient + description | **pass** |  |
| US Core FHIR API / Goal Tests | Server returns valid results for Goal search by patient + lifecycle-status | **pass** |  |
| US Core FHIR API / Goal Tests | Server returns valid results for Goal search by patient | **pass** |  |
| US Core FHIR API / Goal Tests | Server returns valid results for Goal search by patient + target-date | **pass** |  |
| US Core FHIR API / Goal Tests | Server returns Provenance resources from Goal search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Goal Tests | Server returns correct Goal resource from Goal read interaction | **pass** |  |
| US Core FHIR API / Goal Tests | MustSupport references within Goal resources are valid | **pass** |  |
| US Core FHIR API / Goal Tests | Goal resources returned during previous tests conform to the US Core Goal Profile | **pass** |  |
| US Core FHIR API / Immunization Tests | All must support elements are provided in the Immunization resources returned | **pass** |  |
| US Core FHIR API / Immunization Tests | Server returns valid results for Immunization search by patient + date | **pass** |  |
| US Core FHIR API / Immunization Tests | Server returns valid results for Immunization search by patient | **pass** |  |
| US Core FHIR API / Immunization Tests | Server returns valid results for Immunization search by patient + status | **pass** |  |
| US Core FHIR API / Immunization Tests | Server returns Provenance resources from Immunization search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Immunization Tests | Server returns correct Immunization resource from Immunization read interaction | **pass** |  |
| US Core FHIR API / Immunization Tests | MustSupport references within Immunization resources are valid | **pass** |  |
| US Core FHIR API / Immunization Tests | Immunization resources returned during previous tests conform to the US Core Immunization Profile | **pass** |  |
| US Core FHIR API / Implantable Device Tests | All must support elements are provided in the Device resources returned | **pass** |  |
| US Core FHIR API / Implantable Device Tests | Server returns valid results for Device search by patient | **pass** |  |
| US Core FHIR API / Implantable Device Tests | Server returns valid results for Device search by patient + status | **pass** |  |
| US Core FHIR API / Implantable Device Tests | Server returns valid results for Device search by patient + type | **pass** |  |
| US Core FHIR API / Implantable Device Tests | Server returns Provenance resources from Device search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Implantable Device Tests | Server returns correct Device resource from Device read interaction | **pass** |  |
| US Core FHIR API / Implantable Device Tests | MustSupport references within Device resources are valid | **pass** |  |
| US Core FHIR API / Implantable Device Tests | Device resources returned during previous tests conform to the US Core Implantable Device Profile | **pass** |  |
| US Core FHIR API / MedicationDispense Tests | All must support elements are provided in the MedicationDispense resources returned | **skip** | Could not find dosageInstruction.doseAndRate, dosageInstruction.doseAndRate.doseQuantity, dosageInstruction.timing in the 1 provided resource(s) |
| US Core FHIR API / MedicationDispense Tests | Server returns valid results for MedicationDispense search by patient | **pass** |  |
| US Core FHIR API / MedicationDispense Tests | Server returns valid results for MedicationDispense search by patient + status | **pass** |  |
| US Core FHIR API / MedicationDispense Tests | Server returns valid results for MedicationDispense search by patient + status + type | **pass** |  |
| US Core FHIR API / MedicationDispense Tests | Server returns Provenance resources from MedicationDispense search by patient + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / MedicationDispense Tests | Server returns correct MedicationDispense resource from MedicationDispense read interaction | **pass** |  |
| US Core FHIR API / MedicationDispense Tests | MustSupport references within MedicationDispense resources are valid | **pass** |  |
| US Core FHIR API / MedicationDispense Tests | MedicationDispense resources returned during previous tests conform to the US Core MedicationDispense Profile | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | All must support elements are provided in the MedicationRequest resources returned | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | Server returns valid results for MedicationRequest search by patient + intent + authoredon | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | Server returns valid results for MedicationRequest search by patient + intent + encounter | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | Server returns valid results for MedicationRequest search by patient + intent | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | Server returns valid results for MedicationRequest search by patient + intent + status | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | Server returns Provenance resources from MedicationRequest search by patient + intent + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | Server returns correct MedicationRequest resource from MedicationRequest read interaction | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | MustSupport references within MedicationRequest resources are valid | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | MedicationRequest resources returned during previous tests conform to the US Core MedicationRequest Profile | **pass** |  |
| US Core FHIR API / MedicationRequest Tests | Medication resources returned during previous tests conform to the US Core Medication Profile | **pass** |  |
| US Core FHIR API / Missing Data Tests | Server represents missing data with the DataAbsentReason CodeSystem | **pass** |  |
| US Core FHIR API / Missing Data Tests | Server represents missing data with the DataAbsentReason Extension | **pass** |  |
| US Core FHIR API / Observation BMI Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation BMI Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation BMI Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation BMI Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation BMI Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation BMI Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation BMI Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation BMI Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation BMI Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation BMI Tests | Observation resources returned during previous tests conform to the US Core BMI Profile | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | All must support elements are provided in the Observation resources returned | **skip** | Could not find component.dataAbsentReason, component:diastolic.dataAbsentReason, component:systolic.dataAbsentReason in the 3 provided resource(s) |
| US Core FHIR API / Observation Blood Pressure Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Blood Pressure Tests | Observation resources returned during previous tests conform to the US Core Blood Pressure Profile | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Body Height Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Body Height Tests | Observation resources returned during previous tests conform to the US Core Body Height Profile | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Body Temperature Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Body Temperature Tests | Observation resources returned during previous tests conform to the US Core Body Temperature Profile | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Body Weight Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Body Weight Tests | Observation resources returned during previous tests conform to the US Core Body Weight Profile | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | Server returns Provenance resources from Observation search by patient + category + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Clinical Result Tests | Observation resources returned during previous tests conform to the US Core Observation Clinical Result Profile | **pass** |  |
| US Core FHIR API / Observation Head Circumference Tests | All must support elements are provided in the Observation resources returned | **skip** | No Observation resources were found |
| US Core FHIR API / Observation Head Circumference Tests | Server returns valid results for Observation search by patient + category + date | **skip** | Could not find values for all search params `patient`, `category`, `date` |
| US Core FHIR API / Observation Head Circumference Tests | Server returns valid results for Observation search by patient + category | **skip** | Could not find values for all search params `patient`, `category` |
| US Core FHIR API / Observation Head Circumference Tests | Server returns valid results for Observation search by patient + category + status | **skip** | Could not find values for all search params `patient`, `category`, `status` |
| US Core FHIR API / Observation Head Circumference Tests | Server returns valid results for Observation search by patient + code + date | **skip** | Could not find values for all search params `patient`, `code`, `date` |
| US Core FHIR API / Observation Head Circumference Tests | Server returns valid results for Observation search by patient + code | **skip** | No Observation resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Head Circumference Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Head Circumference Tests | Server returns correct Observation resource from Observation read interaction | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Head Circumference Tests | MustSupport references within Observation resources are valid | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Head Circumference Tests | Observation resources returned during previous tests conform to the US Core Head Circumference Profile | **skip** | No Observation resources conforming to the http://hl7.org/fhir/us/core/StructureDefinition/us-core-head-circumference profile were returned |
| US Core FHIR API / Observation Heart Rate Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Heart Rate Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Heart Rate Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Heart Rate Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Heart Rate Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Heart Rate Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Heart Rate Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Heart Rate Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Heart Rate Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Heart Rate Tests | Observation resources returned during previous tests conform to the US Core Heart Rate Profile | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | All must support elements are provided in the Observation resources returned | **skip** | Could not find valueString in the 9 provided resource(s) |
| US Core FHIR API / Observation Laboratory Result Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | Server returns Provenance resources from Observation search by patient + category + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Laboratory Result Tests | Observation resources returned during previous tests conform to the US Core Laboratory Result Observation Profile | **pass** |  |
| US Core FHIR API / Observation Occupation Tests | All must support elements are provided in the Observation resources returned | **skip** | Could not find effective[x]:effectivePeriod in the 1 provided resource(s) |
| US Core FHIR API / Observation Occupation Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Occupation Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Occupation Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Occupation Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Occupation Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Occupation Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Occupation Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Occupation Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Occupation Tests | Observation resources returned during previous tests conform to the US Core Observation Occupation Profile | **pass** |  |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | All must support elements are provided in the Observation resources returned | **skip** | No Observation resources were found |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | Server returns valid results for Observation search by patient + category + date | **skip** | Could not find values for all search params `patient`, `category`, `date` |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | Server returns valid results for Observation search by patient + category | **skip** | Could not find values for all search params `patient`, `category` |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | Server returns valid results for Observation search by patient + category + status | **skip** | Could not find values for all search params `patient`, `category`, `status` |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | Server returns valid results for Observation search by patient + code + date | **skip** | Could not find values for all search params `patient`, `code`, `date` |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | Server returns valid results for Observation search by patient + code | **skip** | No Observation resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | Server returns correct Observation resource from Observation read interaction | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | MustSupport references within Observation resources are valid | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pediatric BMI for Age Tests | Observation resources returned during previous tests conform to the US Core Pediatric BMI for Age Observation Profile | **skip** | No Observation resources conforming to the http://hl7.org/fhir/us/core/StructureDefinition/pediatric-bmi-for-age profile were returned |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | All must support elements are provided in the Observation resources returned | **skip** | No Observation resources were found |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | Server returns valid results for Observation search by patient + category + date | **skip** | Could not find values for all search params `patient`, `category`, `date` |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | Server returns valid results for Observation search by patient + category | **skip** | Could not find values for all search params `patient`, `category` |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | Server returns valid results for Observation search by patient + category + status | **skip** | Could not find values for all search params `patient`, `category`, `status` |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | Server returns valid results for Observation search by patient + code + date | **skip** | Could not find values for all search params `patient`, `code`, `date` |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | Server returns valid results for Observation search by patient + code | **skip** | No Observation resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | Server returns correct Observation resource from Observation read interaction | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | MustSupport references within Observation resources are valid | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pediatric Head Occipital Frontal Circumference Percentile Tests | Observation resources returned during previous tests conform to the US Core Pediatric Head Occipital Frontal Circumference Percentile Profile | **skip** | No Observation resources conforming to the http://hl7.org/fhir/us/core/StructureDefinition/head-occipital-frontal-circumference-percentile profile were returned |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | All must support elements are provided in the Observation resources returned | **skip** | No Observation resources were found |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | Server returns valid results for Observation search by patient + category + date | **skip** | Could not find values for all search params `patient`, `category`, `date` |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | Server returns valid results for Observation search by patient + category | **skip** | Could not find values for all search params `patient`, `category` |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | Server returns valid results for Observation search by patient + category + status | **skip** | Could not find values for all search params `patient`, `category`, `status` |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | Server returns valid results for Observation search by patient + code + date | **skip** | Could not find values for all search params `patient`, `code`, `date` |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | Server returns valid results for Observation search by patient + code | **skip** | No Observation resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | Server returns correct Observation resource from Observation read interaction | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | MustSupport references within Observation resources are valid | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pediatric Weight for Height Tests | Observation resources returned during previous tests conform to the US Core Pediatric Weight for Height Observation Profile | **skip** | No Observation resources conforming to the http://hl7.org/fhir/us/core/StructureDefinition/pediatric-weight-for-height profile were returned |
| US Core FHIR API / Observation Pregnancy Intent Tests | All must support elements are provided in the Observation resources returned | **skip** | No Observation resources were found |
| US Core FHIR API / Observation Pregnancy Intent Tests | Server returns valid results for Observation search by patient + category + date | **skip** | Could not find values for all search params `patient`, `category`, `date` |
| US Core FHIR API / Observation Pregnancy Intent Tests | Server returns valid results for Observation search by patient + category | **skip** | Could not find values for all search params `patient`, `category` |
| US Core FHIR API / Observation Pregnancy Intent Tests | Server returns valid results for Observation search by patient + category + status | **skip** | Could not find values for all search params `patient`, `category`, `status` |
| US Core FHIR API / Observation Pregnancy Intent Tests | Server returns valid results for Observation search by patient + code + date | **skip** | Could not find values for all search params `patient`, `code`, `date` |
| US Core FHIR API / Observation Pregnancy Intent Tests | Server returns valid results for Observation search by patient + code | **skip** | No Observation resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pregnancy Intent Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pregnancy Intent Tests | Server returns correct Observation resource from Observation read interaction | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pregnancy Intent Tests | MustSupport references within Observation resources are valid | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pregnancy Intent Tests | Observation resources returned during previous tests conform to the US Core Observation Pregnancy Intent Profile | **skip** | No Observation resources conforming to the http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-pregnancyintent profile were returned |
| US Core FHIR API / Observation Pregnancy Status Tests | All must support elements are provided in the Observation resources returned | **skip** | No Observation resources were found |
| US Core FHIR API / Observation Pregnancy Status Tests | Server returns valid results for Observation search by patient + category + date | **skip** | Could not find values for all search params `patient`, `category`, `date` |
| US Core FHIR API / Observation Pregnancy Status Tests | Server returns valid results for Observation search by patient + category | **skip** | Could not find values for all search params `patient`, `category` |
| US Core FHIR API / Observation Pregnancy Status Tests | Server returns valid results for Observation search by patient + category + status | **skip** | Could not find values for all search params `patient`, `category`, `status` |
| US Core FHIR API / Observation Pregnancy Status Tests | Server returns valid results for Observation search by patient + code + date | **skip** | Could not find values for all search params `patient`, `code`, `date` |
| US Core FHIR API / Observation Pregnancy Status Tests | Server returns valid results for Observation search by patient + code | **skip** | No Observation resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pregnancy Status Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pregnancy Status Tests | Server returns correct Observation resource from Observation read interaction | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pregnancy Status Tests | MustSupport references within Observation resources are valid | **skip** | No Observation resources appear to be available. Please use patients with more information. |
| US Core FHIR API / Observation Pregnancy Status Tests | Observation resources returned during previous tests conform to the US Core Observation Pregnancy Status Profile | **skip** | No Observation resources conforming to the http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-pregnancystatus profile were returned |
| US Core FHIR API / Observation Pulse Oximetry Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Pulse Oximetry Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Pulse Oximetry Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Pulse Oximetry Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Pulse Oximetry Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Pulse Oximetry Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Pulse Oximetry Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Pulse Oximetry Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Pulse Oximetry Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Pulse Oximetry Tests | Observation resources returned during previous tests conform to the US Core Pulse Oximetry Profile | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Respiratory Rate Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Respiratory Rate Tests | Observation resources returned during previous tests conform to the US Core Respiratory Rate Profile | **pass** |  |
| US Core FHIR API / Observation Screening Assessment Tests | All must support elements are provided in the Observation resources returned | **skip** | Could not find hasMember, valueString in the 2 provided resource(s) |
| US Core FHIR API / Observation Screening Assessment Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Screening Assessment Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Screening Assessment Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Screening Assessment Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Screening Assessment Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Screening Assessment Tests | Server returns Provenance resources from Observation search by patient + category + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Screening Assessment Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Screening Assessment Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Screening Assessment Tests | Observation resources returned during previous tests conform to the US Core Observation Screening Assessment Profile | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / Observation Sexual Orientation Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Sexual Orientation Tests | Observation resources returned during previous tests conform to the US Core Observation Sexual Orientation Profile | **pass** |  |
| US Core FHIR API / Observation Simple Tests | All must support elements are provided in the Observation resources returned | **skip** | Could not find valueBoolean in the 29 provided resource(s) |
| US Core FHIR API / Observation Simple Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Simple Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Simple Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Simple Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Simple Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Simple Tests | Server returns Provenance resources from Observation search by patient + category + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Observation Simple Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Simple Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Simple Tests | Observation resources returned during previous tests conform to the US Core Simple Observation Profile | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | All must support elements are provided in the Observation resources returned | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | Server returns valid results for Observation search by patient + category + date | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | Server returns valid results for Observation search by patient + category | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | Server returns valid results for Observation search by patient + category + status | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | Server returns valid results for Observation search by patient + code + date | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | Server returns valid results for Observation search by patient + code | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | Server returns Provenance resources from Observation search by patient + code + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | Server returns correct Observation resource from Observation read interaction | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | MustSupport references within Observation resources are valid | **pass** |  |
| US Core FHIR API / Observation Smoking Status Tests | Observation resources returned during previous tests conform to the US Core Smoking Status Observation Profile | **pass** |  |
| US Core FHIR API / Organization Tests | Server returns valid results for Organization search by address | **pass** |  |
| US Core FHIR API / Organization Tests | All must support elements are provided in the Organization resources returned | **pass** |  |
| US Core FHIR API / Organization Tests | Server returns valid results for Organization search by name | **pass** |  |
| US Core FHIR API / Organization Tests | Server returns correct Organization resource from Organization read interaction | **pass** |  |
| US Core FHIR API / Organization Tests | Organization resources returned during previous tests conform to the US Core Organization Profile | **pass** |  |
| US Core FHIR API / Patient Tests | Server returns valid results for Patient search by _id | **pass** |  |
| US Core FHIR API / Patient Tests | Server returns valid results for Patient search by birthdate + family | **pass** |  |
| US Core FHIR API / Patient Tests | Server returns valid results for Patient search by birthdate + name | **pass** |  |
| US Core FHIR API / Patient Tests | Server returns valid results for Patient search by death-date + family | **skip** | Could not find values for all search params `death-date`, `family` |
| US Core FHIR API / Patient Tests | Server returns valid results for Patient search by family + gender | **pass** |  |
| US Core FHIR API / Patient Tests | Server returns valid results for Patient search by gender + name | **pass** |  |
| US Core FHIR API / Patient Tests | Server returns valid results for Patient search by identifier | **pass** |  |
| US Core FHIR API / Patient Tests | All must support elements are provided in the Patient resources returned | **skip** | Could not find address.period.end, address.use = old, deceasedDateTime, extension:tribalAffiliation, name.suffix in the 2 provided resource(s) |
| US Core FHIR API / Patient Tests | Server returns valid results for Patient search by name | **pass** |  |
| US Core FHIR API / Patient Tests | Server returns Provenance resources from Patient search by _id + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Patient Tests | Server returns correct Patient resource from Patient read interaction | **pass** |  |
| US Core FHIR API / Patient Tests | Patient resources returned during previous tests conform to the US Core Patient Profile | **pass** |  |
| US Core FHIR API / Practitioner Tests | Server returns valid results for Practitioner search by _id | **pass** |  |
| US Core FHIR API / Practitioner Tests | Server support either Practitioner.address or PractitionerRole | **pass** |  |
| US Core FHIR API / Practitioner Tests | Server returns valid results for Practitioner search by identifier | **pass** |  |
| US Core FHIR API / Practitioner Tests | All must support elements are provided in the Practitioner resources returned | **pass** |  |
| US Core FHIR API / Practitioner Tests | Server returns valid results for Practitioner search by name | **pass** |  |
| US Core FHIR API / Practitioner Tests | Server returns correct Practitioner resource from Practitioner read interaction | **pass** |  |
| US Core FHIR API / Practitioner Tests | Practitioner resources returned during previous tests conform to the US Core Practitioner Profile | **pass** |  |
| US Core FHIR API / PractitionerRole Tests | All must support elements are provided in the PractitionerRole resources returned | **skip** | Could not find endpoint in the 1 provided resource(s) |
| US Core FHIR API / PractitionerRole Tests | Server returns valid results for PractitionerRole search by practitioner | **pass** |  |
| US Core FHIR API / PractitionerRole Tests | Server returns correct PractitionerRole resource from PractitionerRole read interaction | **pass** |  |
| US Core FHIR API / PractitionerRole Tests | MustSupport references within PractitionerRole resources are valid | **pass** |  |
| US Core FHIR API / PractitionerRole Tests | Server returns valid results for PractitionerRole search by specialty | **pass** |  |
| US Core FHIR API / PractitionerRole Tests | PractitionerRole resources returned during previous tests conform to the US Core PractitionerRole Profile | **pass** |  |
| US Core FHIR API / Procedure Tests | All must support elements are provided in the Procedure resources returned | **skip** | Could not find basedOn in the 3 provided resource(s) |
| US Core FHIR API / Procedure Tests | Server returns valid results for Procedure search by patient + code + date | **pass** |  |
| US Core FHIR API / Procedure Tests | Server returns valid results for Procedure search by patient + date | **pass** |  |
| US Core FHIR API / Procedure Tests | Server returns valid results for Procedure search by patient | **pass** |  |
| US Core FHIR API / Procedure Tests | Server returns valid results for Procedure search by patient + status | **pass** |  |
| US Core FHIR API / Procedure Tests | Server returns Provenance resources from Procedure search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / Procedure Tests | Server returns correct Procedure resource from Procedure read interaction | **pass** |  |
| US Core FHIR API / Procedure Tests | MustSupport references within Procedure resources are valid | **pass** |  |
| US Core FHIR API / Procedure Tests | Procedure resources returned during previous tests conform to the US Core Procedure Profile | **pass** |  |
| US Core FHIR API / Provenance Tests | All must support elements are provided in the Provenance resources returned | **pass** |  |
| US Core FHIR API / Provenance Tests | Server returns correct Provenance resource from Provenance read interaction | **pass** |  |
| US Core FHIR API / Provenance Tests | MustSupport references within Provenance resources are valid | **pass** |  |
| US Core FHIR API / Provenance Tests | Provenance resources returned during previous tests conform to the US Core Provenance Profile | **pass** |  |
| US Core FHIR API / QuestionnaireResponse Tests | Server returns valid results for QuestionnaireResponse search by _id | **pass** |  |
| US Core FHIR API / QuestionnaireResponse Tests | All must support elements are provided in the QuestionnaireResponse resources returned | **skip** | Could not find identifier, item.answer.item, item.answer.valueDecimal, item.answer.valueString, item.item, questionnaire.extension:questionnaireDisplay, questionnaire.extension:url |
| US Core FHIR API / QuestionnaireResponse Tests | Server returns valid results for QuestionnaireResponse search by patient + authored | **pass** |  |
| US Core FHIR API / QuestionnaireResponse Tests | Server returns valid results for QuestionnaireResponse search by patient + questionnaire | **pass** |  |
| US Core FHIR API / QuestionnaireResponse Tests | Server returns valid results for QuestionnaireResponse search by patient | **pass** |  |
| US Core FHIR API / QuestionnaireResponse Tests | Server returns valid results for QuestionnaireResponse search by patient + status | **pass** |  |
| US Core FHIR API / QuestionnaireResponse Tests | Server returns Provenance resources from QuestionnaireResponse search by patient + revInclude:Provenance:target | **skip** | No Provenance resources appear to be available. Please use patients with more information |
| US Core FHIR API / QuestionnaireResponse Tests | Server returns correct QuestionnaireResponse resource from QuestionnaireResponse read interaction | **pass** |  |
| US Core FHIR API / QuestionnaireResponse Tests | MustSupport references within QuestionnaireResponse resources are valid | **pass** |  |
| US Core FHIR API / QuestionnaireResponse Tests | QuestionnaireResponse resources returned during previous tests conform to the US Core QuestionnaireResponse Profile | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | Server returns valid results for RelatedPerson search by _id | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | All must support elements are provided in the RelatedPerson resources returned | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | Server returns valid results for RelatedPerson search by name | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | Server returns valid results for RelatedPerson search by patient + name | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | Server returns valid results for RelatedPerson search by patient | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | Server returns Provenance resources from RelatedPerson search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | Server returns correct RelatedPerson resource from RelatedPerson read interaction | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | MustSupport references within RelatedPerson resources are valid | **pass** |  |
| US Core FHIR API / RelatedPerson Tests | RelatedPerson resources returned during previous tests conform to the US Core RelatedPerson Profile | **pass** |  |
| US Core FHIR API / Screening Assessments Guidance | Server demonstrates support for the required Condition and Observation categories. | **skip** | Could not find these Observation categories: http://hl7.org/fhir/us/core/CodeSystem/us-core-category\|functional-status, http://hl7.org/fhir/us/core/CodeSystem/us-core-category\|di |
| US Core FHIR API / ServiceRequest Tests | Server returns valid results for ServiceRequest search by _id | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | All must support elements are provided in the ServiceRequest resources returned | **skip** | Could not find occurrencePeriod in the 2 provided resource(s) |
| US Core FHIR API / ServiceRequest Tests | Server returns valid results for ServiceRequest search by patient + category + authored | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | Server returns valid results for ServiceRequest search by patient + category | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | Server returns valid results for ServiceRequest search by patient + code + authored | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | Server returns valid results for ServiceRequest search by patient + code | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | Server returns valid results for ServiceRequest search by patient | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | Server returns valid results for ServiceRequest search by patient + status | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | Server returns Provenance resources from ServiceRequest search by patient + revInclude:Provenance:target | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | Server returns correct ServiceRequest resource from ServiceRequest read interaction | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | MustSupport references within ServiceRequest resources are valid | **pass** |  |
| US Core FHIR API / ServiceRequest Tests | ServiceRequest resources returned during previous tests conform to the US Core ServiceRequest Profile | **pass** |  |
| US Core FHIR API / Specimen Tests | Server returns valid results for Specimen search by _id | **pass** |  |
| US Core FHIR API / Specimen Tests | All must support elements are provided in the Specimen resources returned | **pass** |  |
| US Core FHIR API / Specimen Tests | Server returns valid results for Specimen search by patient | **pass** |  |
| US Core FHIR API / Specimen Tests | Server returns correct Specimen resource from Specimen read interaction | **pass** |  |
| US Core FHIR API / Specimen Tests | MustSupport references within Specimen resources are valid | **pass** |  |
| US Core FHIR API / Specimen Tests | Specimen resources returned during previous tests conform to the US Core Specimen Profile | **pass** |  |
