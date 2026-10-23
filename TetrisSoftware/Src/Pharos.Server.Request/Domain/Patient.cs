using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain;

namespace Pharos.Server.Request.Domain;

/// <summary>
/// Represents a person whose blood gets tested.
/// </summary>
internal class Patient : BaseAuditableEntity
{
    /// <summary>
    /// The patient name.
    /// </summary>
    [MaxLength(Constants.PatientNameLength)]
    [ProtectedData]
    public string? Name { get; set; }

    /// <summary>
    /// The patient's gender.
    /// </summary>
    public Gender Gender { get; set; } = Gender.Undefined;

    /// <summary>
    /// The patient's birthdate.
    /// </summary>
    [ProtectedData]
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// The unique patient number defined by a lab (e.g. a social security number).
    /// </summary>
    [MaxLength(Constants.PatientNumberLength)]
    [ProtectedData]
    public string? PatientNumber { get; set; }

    /// <summary>
    /// The patient's medical record number.
    /// </summary>
    [MaxLength(Constants.MedicalRecordNumberLength)]
    [ProtectedData]
    public string? MedicalRecordNumber { get; set; }

    /// <summary>
    /// The processing number assigned to the patient by the practice.
    /// It is a "primary key" for patient record in the hospital database.
    /// This number is sent by LIS and Pharos returns this field back unchanged.
    /// </summary>
    [MaxLength(Constants.PatientIdLength)]
    [ProtectedData]
    public string? PatientId { get; set; }

    /// <summary>
    /// The patient's address (without the zip code).
    /// </summary>
    [MaxLength(Constants.PatientAddressLength)]
    [ProtectedData]
    public string? Address { get; set; }

    /// <summary>
    /// The zip code part of the patient's address.
    /// </summary>
    [MaxLength(Constants.PatientZipCodeLength)]
    [ProtectedData]
    public string? ZipCode { get; set; }

    /// <summary>
    /// The patient diagnosis (in a free text form).
    /// </summary>
    [MaxLength(Constants.PatientDiagnosisLength)]
    [ProtectedData]
    public string? Diagnosis { get; set; }

    /// <summary>
    /// The comment about the patient.
    /// </summary>
    [MaxLength(Constants.PatientCommentLength)]
    [ProtectedData]
    public string? Comment { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for patient requests.
    /// </summary>
    public virtual IList<Request>? Requests { get; set; }

    #endregion
}