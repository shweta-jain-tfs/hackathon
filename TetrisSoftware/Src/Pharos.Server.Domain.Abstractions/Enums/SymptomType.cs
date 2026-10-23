namespace Pharos.Server.Domain.Enums;

/// <summary>
/// List of possible patient's symptoms.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum SymptomType
{
    NoSymptoms = 0,
    RhinoConjunctivitis = 1,
    AtopicEczemaDermatitisSyndrome = 2,
    Urticaria = 3,
    OralAllergySyndrome = 5,
    ChildhoodWheezing = 6,
    Asthma = 7,
    GastrointestinalSymptoms = 9,
    Anaphylaxis = 10,
    OtherSymptoms = 12
}