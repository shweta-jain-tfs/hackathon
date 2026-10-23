using System.Text;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Application.Mappings;

internal static class AstmPatientRecordExtensions
{
    public static LisPatient MapToPatient(this AstmPatientRecord record, AstmSettings astmSettings, LisImportSettings lisImportSettings)
    {
        var patient = new LisPatient
        {
            Name = CombinePatientName(record, astmSettings.Other.PatientNameInTwoFields),
            Gender = record.Gender,
            BirthDate = record.BirthDate,
            PatientNumber = record.PatientNumber,
            MedicalRecordNumber = record.MedicalRecordNumber,
            PracticePatientId = AssignPatientId(record, astmSettings),
            Address = CombineAddressFields(record),
            ZipCode = record.AddressLine5,
            Diagnosis = record.Diagnosis,
            Comment = CombineCommentFields(record, astmSettings.Other.AllowExtraLongPatientComments),
            PatientAge = record.Age,
            CalculateAge = lisImportSettings.CalculatePatientAge
        };

        if (record.Symptoms is { Count: > 0 })
        {
            foreach (var symptom in record.Symptoms)
            {
                patient.Symptoms.Add(new LisSymptom
                {
                    SymptomType = symptom
                });
            }
        }

        return patient;
    }

    private static string AssignPatientId(AstmPatientRecord record, AstmSettings astmSettings)
    {
        if (astmSettings.ImportSettings.UseLaboratoryPidAsPatientId)
        {
            return record.LaboratoryPatientId;
        }
        else
        {
            return record.PracticePatientId;
        }
    }

    public static void MapFromPatient(this AstmPatientRecord record, LisPatient patient, string requestId, AstmSettings astmSettings)
    {
        record.LaboratoryPatientId = requestId;
        record.PracticePatientId = patient.PracticePatientId ?? "";

        if (!astmSettings.ExportSettings.SkipExportOfPatientInformation)
        {
            record.Gender = patient.Gender;
            record.BirthDate = patient.BirthDate;
            record.PatientNumber = patient.PatientNumber ?? "";
            record.MedicalRecordNumber = patient.MedicalRecordNumber ?? "";
            record.AddressLine5 = patient.ZipCode ?? "";
            record.Diagnosis = patient.Diagnosis ?? "";
            record.Age = patient.PatientAge;

            AssignPatientName(record, patient, astmSettings.Other.PatientNameInTwoFields);
            AssignSymptoms(record, patient);
            AssignAddressFields(record, patient.Address);
            AssignCommentFields(record, patient.Comment, astmSettings.Other.AllowExtraLongPatientComments);
        }
    }

    private static string CombineAddressFields(AstmPatientRecord patientRecord)
    {
        var builder = new StringBuilder(160);
        builder.Append(patientRecord.AddressLine1);
        builder.Append('\n');
        builder.Append(patientRecord.AddressLine2);
        builder.Append('\n');
        builder.Append(patientRecord.AddressLine3);
        builder.Append('\n');
        builder.Append(patientRecord.AddressLine4);

        var result = builder.ToString();
        return string.IsNullOrWhiteSpace(result) ? string.Empty : result;
    }

    private static string CombineCommentFields(AstmPatientRecord patientRecord, bool AllowExtraLongPatientComments)
    {
        var maxLength = AllowExtraLongPatientComments ? 1000 : 80;
        var builder = new StringBuilder(maxLength);

        builder.Append(patientRecord.CommentLine1);
        builder.Append('\n');
        builder.Append(patientRecord.CommentLine2);
        builder.Append('\n');
        builder.Append(patientRecord.CommentLine3);
        builder.Append('\n');
        builder.Append(patientRecord.CommentLine4);
        builder.Append('\n');
        builder.Append(patientRecord.CommentLine5);

        var result = builder.ToString();
        if (result.Length > maxLength)
        {
            result = result[..maxLength];
        }

        return string.IsNullOrWhiteSpace(result) ? string.Empty : result;
    }

    private static void AssignPatientName(AstmPatientRecord patientRecord, LisPatient patient, bool patientNameIn2Fields)
    {
        if (string.IsNullOrEmpty(patient.Name))
        {
            return;
        }

        if (!patientNameIn2Fields)
        {
            patientRecord.PatientName1 = patient.Name;
            return;
        }

        var nameParts = patient.Name.Split(',', 2);
        patientRecord.PatientName1 = nameParts[0].Trim();
        if (nameParts.Length == 2)
        {
            patientRecord.PatientName2 = nameParts[1].Trim();
        }
    }

    private static void AssignAddressFields(AstmPatientRecord patientRecord, string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        address = address.Replace("\r", "");
        if (address.Length > 150) // Max address field length in ASTM
        {
            address = address[..150];
        }

        var addressParts = address.Split('\n');
        patientRecord.AddressLine1 = addressParts[0];
        if (addressParts.Length > 1)
        {
            patientRecord.AddressLine2 = addressParts[1];
        }
        if (addressParts.Length > 2)
        {
            patientRecord.AddressLine3 = addressParts[2];
        }
        if (addressParts.Length > 3)
        {
            patientRecord.AddressLine4 = addressParts[3];
        }
    }

    private static void AssignCommentFields(AstmPatientRecord patientRecord, string? comment, bool AllowExtraLongPatientComments)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return;
        }

        comment = comment.Replace("\r", "");
        var maxLength = AllowExtraLongPatientComments ? 1000 : 80;
        if (comment.Length > maxLength)
        {
            comment = comment[..maxLength];
        }

        var commentParts = comment.Split('\n');
        patientRecord.CommentLine1 = commentParts[0];
        if (commentParts.Length > 1)
        {
            patientRecord.CommentLine2 = commentParts[1];
        }
        if (commentParts.Length > 2)
        {
            patientRecord.CommentLine3 = commentParts[2];
        }
        if (commentParts.Length > 3)
        {
            patientRecord.CommentLine4 = commentParts[3];
        }
        if (commentParts.Length > 4)
        {
            patientRecord.CommentLine5 = commentParts[4];
        }
    }

    private static void AssignSymptoms(AstmPatientRecord patientRecord, LisPatient patient)
    {
        if (patient.Symptoms.Count > 0)
        {
            patientRecord.Symptoms ??= new List<SymptomType>();

            foreach (var symptom in patient.Symptoms)
            {
                patientRecord.Symptoms.Add(symptom.SymptomType);
            }
        }
    }

    private static string CombinePatientName(this AstmPatientRecord record, bool patientNameIn2Fields)
    {
        var patientName = record.PatientName1;
        if (patientNameIn2Fields && !string.IsNullOrEmpty(record.PatientName2))
        {
            patientName = record.PatientName1 + ", " + record.PatientName2;
        }
        return patientName.Trim();
    }
}