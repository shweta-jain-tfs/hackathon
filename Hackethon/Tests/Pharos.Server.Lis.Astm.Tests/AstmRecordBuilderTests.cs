using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmRecordBuilderTests
{
    [Fact]
    public void BuildHeaderRecord()
    {
        var record = new AstmHeaderRecord(AstmFieldDelimiters.Default, "UniCAP Data Manager (UDM)", "5.00", "4.0", "LIS", "127.0.0.1", AstmProcessingId.Production, new DateTime(2021, 1, 20, 15, 0, 28));

        record.ToString().Should().Be("H|\\^&|||UniCAP Data Manager (UDM)^5.00^4.0|||||LIS^127.0.0.1||P|1|20210120150028");
    }

    [Theory]
    [InlineData(1, "")]
    [InlineData(2, "SID001")]
    public void BuildRequestRecord(int sequenceNumber, string specimenId)
    {
        var record = new AstmRequestRecord()
        {
            SequenceNumber = sequenceNumber,
            SpecimenId = specimenId
        };

        var specimen = string.IsNullOrWhiteSpace(specimenId) ? "ALL" : specimenId;
        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be($"Q|{sequenceNumber}|^{specimen}||||||||||O");
    }

    [Fact]
    public void BuildPatientRecord()
    {
        var record = new AstmPatientRecord()
        {
            SequenceNumber = 1,
            PracticePatientId = "PracticePID",
            LaboratoryPatientId = "LabPID",
            PatientNumber = "PatientNumber",
            PatientName1 = "PatientName",
            BirthDate = new DateOnly(2021, 1, 20),
            Gender = Gender.Undefined,
            AddressLine1 = "PatientAddress1",
            AddressLine2 = "PatientAddress2",
            AddressLine3 = "PatientAddress3",
            AddressLine4 = "PatientAddress4",
            AddressLine5 = "PatientAddress5",
            CommentLine1 = "Comment1",
            CommentLine2 = "Comment2",
            CommentLine3 = "Comment3",
            CommentLine4 = "Comment4",
            CommentLine5 = "Comment5",
            Age = 5,
            MedicalRecordNumber = "MedRec",
            Diagnosis = "Diagnosis",
            Symptoms = new() { SymptomType.RhinoConjunctivitis, SymptomType.AtopicEczemaDermatitisSyndrome, SymptomType.Urticaria, SymptomType.GastrointestinalSymptoms }
        };

        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("P|1|PracticePID|LabPID|PatientNumber|PatientName||20210120|U||PatientAddress1^PatientAddress2^PatientAddress3^PatientAddress4^PatientAddress5||||Comment1^Comment2^Comment3^Comment4^Comment5|5|||Diagnosis^1\\2\\3\\9|||MedRec");
    }

    [Fact]
    public void BuildTestOrderRecord()
    {
        var record = new AstmTestOrderRecord()
        {
            SequenceNumber = 1,
            SpecimenId = "SID001",
            TubeType = "N",
            RackId = "01",
            RackPosition = 5,
            UniversalTestIds = new()
            {
                new AstmTestId("f1", "sIgE", 1, 0, "", 0),
                new AstmTestId("f2", "sIgE", 1, 0, "", 0)
            },
            SpecimenCollectionDateAndTime = new DateTime(2001, 2, 26, 9, 0, 0),
            TestAction = AstmTestAction.NewTest,
            Dilution = 1,
        };

        record.ToRecordString(AstmFieldDelimiters.Default)
            .Should()
            .Be("O|1|SID001^N^01^5||^^^f1^sIgE^1\\^^^f2^sIgE^1|||20010226090000||||N||1|||||^^^^^^0|||||||O");
    }

    [Fact]
    public void ParseTestOrderRecord()
    {
        var recordString = "O|1|SID001^N^01^5||^^^f1^sIgE^1\\^^^f2^sIgE^1|||20010226090000||||N||1||||||||||||O";
        var record = AstmTestOrderRecord.Parse(recordString, AstmFieldDelimiters.Default);

        record.Should().NotBeNull();
        record.SequenceNumber.Should().Be(1);
        record.SpecimenId.Should().Be("SID001");
        record.TubeType.Should().Be("N");
        record.RackId.Should().Be("01");
        record.RackPosition.Should().Be(5);
        record.UniversalTestIds.Should().HaveCount(2);
        record.UniversalTestIds[0].Should().BeEquivalentTo(new AstmTestId("f1", "sIgE", 1, 0, "", 0));
        record.UniversalTestIds[1].Should().BeEquivalentTo(new AstmTestId("f2", "sIgE", 1, 0, "", 0));
        record.SpecimenCollectionDateAndTime.Should().Be(new DateTime(2001, 2, 26, 9, 0, 0));
        record.TestAction.Should().Be(AstmTestAction.NewTest);
        record.Dilution.Should().Be(1);
    }

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildTestFinalResultRecord()
    {
        var record = new AstmTestResultRecord
        {
            SequenceNumber = 2,
            Class = "Class",
            ClinRelation = "ClinicalRelation",
            Concentration = "0.2",
            Cutoff = "Cutoff",
            Cutoff2 = "Cutoff2",
            DateTimeCompleted = new DateTime(2023, 8, 1, 13, 34, 26),
            InstrumentId = "InstrumentId",
            ResultStatus = AstmTestResultStatus.Final,
            PendingResultStatus = AstmPendingTestResultStatus.TestNotCompletedNotStarted,
            ProcessingErrorStatus = AstmErrorTestResultStatus.ProcessingErrorSample,
            Quotient = "Quotient",
            ResultInterpretationCommentIndex = "CommentID",
            ResultOperatorId = "ResultOperatorId",
            Sensitization = "Sensitization",
            UniversalTestId = new AstmTestId("TestId", "MethodId", 2, 1, "", 3),
            Units = "ml/g"
        };

        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("R|2|^^^TestId^MethodId^2|0.2^Class^Cutoff^Cutoff2^Quotient^Sensitization^ClinicalRelation^CommentID|ml/g||||F||ResultOperatorId||20230801133426|InstrumentId");
    }

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildTestPendingResultRecord()
    {
        var record = new AstmTestResultRecord
        {
            ResultStatus = AstmTestResultStatus.Pending,
            PendingResultStatus = AstmPendingTestResultStatus.None,
        };
        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("R|1|||||||I");

        record.PendingResultStatus = AstmPendingTestResultStatus.TestNotCompletedMissingCAP;
        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("R|1|||||||I^M");

        record.PendingResultStatus = AstmPendingTestResultStatus.TestNotCompletedQNS;
        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("R|1|||||||I^Q");

        record.PendingResultStatus = AstmPendingTestResultStatus.TestNotCompletedMissingCAPEsoteric;
        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("R|1|||||||I^E");

        record.PendingResultStatus = AstmPendingTestResultStatus.TestNotCompletedNotStarted;
        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("R|1|||||||I^N");

        record.PendingResultStatus = AstmPendingTestResultStatus.TestNotCompletedProcessing;
        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("R|1|||||||I^P");
    }

    [Fact]
    public void BuildTerminatorRecord()
    {
        var record = AstmTerminatorRecord.Default;

        record.ToRecordString(AstmFieldDelimiters.Default).Should().Be("L|1|N");
    }

    [Fact]
    public void BuildRecordWithEscaping()
    {
        var record = new AstmHeaderRecord(AstmFieldDelimiters.Default, "Sender & \\ ^ | Name", "", "", "", "", AstmProcessingId.Undefined, null);

        record.ToString().Should().Be("H|\\^&|||Sender &E& &R& &S& &F& Name|||||||P|1");
    }
}