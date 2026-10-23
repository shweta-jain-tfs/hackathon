using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// Patient information sent by LIS with a test order.
/// Even if LIS does not identify the patient the massage still contain an
/// "anonymous patient".
/// </summary>
public class LisPatient
{
    /// <summary>
    /// The processing number assigned to the patient by the practice.
    /// It is a "primary key" for patient record in the hospital database.
    /// This number is sent by LIS and Pharos returns this field back unchanged.
    /// </summary>
    public string? PracticePatientId { get; set; }

    /// <summary>
    /// The unique patient number defined by a lab (e.g. a social security number).
    /// </summary>
    public string? PatientNumber { get; set; }

    /// <summary>
    /// The patient's medical record number.
    /// </summary>
    public string? MedicalRecordNumber { get; set; }

    /// <summary>
    /// The patient's gender.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// The patient name.
    /// </summary>
    [ProtectedData] public string? Name { get; set; }

    /// <summary>
    /// The patient's birthdate.
    /// </summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// The patient's age at the moment where the sample was taken.
    /// </summary>
    public int? PatientAge { get; set; }

    /// <summary>
    /// The zip code part of the patient's address.
    /// </summary>
    public string? ZipCode { get; set; }

    /// <summary>
    /// The patient diagnosis (in a free text form).
    /// </summary>
    public string? Diagnosis { get; set; }

    /// <summary>
    /// The patient's address (without the zip code).
    /// </summary>
    [ProtectedData] public string? Address { get; set; }

    /// <summary>
    /// The comment about the patient.
    /// </summary>
    [ProtectedData] public string? Comment { get; set; }

    /// <summary>
    /// The navigation property for the related symptoms.
    /// </summary>
    public List<LisSymptom> Symptoms { get; set; } = [];

    /// <summary>
    /// The list of the patient's samples with test orders.
    /// </summary>
    public List<LisSample> Samples { get; set; } = [];

    /// <summary>
    /// Indicates whether the patient age should be calculated based on birth date and sample collection date.
    /// This flag is set by the LIS module based on the CalculatePatientAge import setting.
    /// </summary>
    public bool CalculateAge { get; set; }
}