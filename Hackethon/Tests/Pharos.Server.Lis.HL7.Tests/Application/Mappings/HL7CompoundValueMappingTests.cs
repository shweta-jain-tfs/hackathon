using FluentAssertions;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.HL7.Application.Mappings;
using Pharos.Server.Lis.HL7.Application.Messages;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Application.Mappings;

public class HL7CompoundValueMappingTests
{
    [Fact]
    public void MapFromPatient_ShouldSplitNameByCommaAndAddressByLine()
    {
        var patient = new LisPatient
        {
            Name = "Doe, Jane, Ann",
            Address = "Street 1\r\nBuilding B\r\nUppsala\r\nSE-UP\r\nSweden",
            ZipCode = "12345"
        };
        var segment = new HL7PatientIdentificationSegment();

        segment.MapFromPatient(patient, new HL7ExportSettings());

        segment.LastName.Should().Be("Doe");
        segment.FirstName.Should().Be("Jane");
        segment.MiddleName.Should().Be("Ann");
        segment.StreetAddress.Should().Be("Street 1");
        segment.OtherAddress.Should().Be("Building B");
        segment.City.Should().Be("Uppsala");
        segment.State.Should().Be("SE-UP");
        segment.ZipCode.Should().Be("12345");
        segment.Country.Should().Be("Sweden");
    }

    [Fact]
    public void MapFromPatient_WithMissingNameAndAddressParts_ShouldUseEmptyStrings()
    {
        var patient = new LisPatient
        {
            Name = "Doe",
            Address = "Street 1",
            ZipCode = "12345"
        };
        var segment = new HL7PatientIdentificationSegment();

        segment.MapFromPatient(patient, new HL7ExportSettings());

        segment.LastName.Should().Be("Doe");
        segment.FirstName.Should().Be("");
        segment.MiddleName.Should().Be("");
        segment.StreetAddress.Should().Be("Street 1");
        segment.OtherAddress.Should().Be("");
        segment.City.Should().Be("");
        segment.State.Should().Be("");
        segment.ZipCode.Should().Be("12345");
        segment.Country.Should().Be("");
    }

    [Fact]
    public void MapFromSample_ShouldSplitRequestorNameByCommaAndAddressByLine()
    {
        var sample = new LisSample
        {
            Requestor = new LisRequestor
            {
                RequestorId = "REQ-1",
                Name = "House, Gregory, MD",
                Address = "Clinic Street\r\nFloor 2\r\nPrinceton\r\nNJ\r\nUSA",
                ZipCode = "08540",
                PhoneNumber = "555-0100"
            }
        };
        var segment = new HL7CommonOrderSegment();

        segment.MapFromSample(sample, new HL7ExportSettings());

        segment.RequestorId.Should().Be("REQ-1");
        segment.RequestorLastName.Should().Be("House");
        segment.RequestorFirstName.Should().Be("Gregory");
        segment.RequestorMiddleName.Should().Be("MD");
        segment.RequestorStreetAddress.Should().Be("Clinic Street");
        segment.RequestorOtherAddress.Should().Be("Floor 2");
        segment.RequestorCity.Should().Be("Princeton");
        segment.RequestorState.Should().Be("NJ");
        segment.RequestorZipCode.Should().Be("08540");
        segment.RequestorCountry.Should().Be("USA");
        segment.RequestorPhone.Should().Be("555-0100");
    }

    [Fact]
    public void MapFromSample_WithMissingRequestorNameAndAddressParts_ShouldUseEmptyStrings()
    {
        var sample = new LisSample
        {
            Requestor = new LisRequestor
            {
                Name = "House",
                Address = "Clinic Street",
                ZipCode = "08540"
            }
        };
        var segment = new HL7CommonOrderSegment();

        segment.MapFromSample(sample, new HL7ExportSettings());

        segment.RequestorLastName.Should().Be("House");
        segment.RequestorFirstName.Should().Be("");
        segment.RequestorMiddleName.Should().Be("");
        segment.RequestorStreetAddress.Should().Be("Clinic Street");
        segment.RequestorOtherAddress.Should().Be("");
        segment.RequestorCity.Should().Be("");
        segment.RequestorState.Should().Be("");
        segment.RequestorZipCode.Should().Be("08540");
        segment.RequestorCountry.Should().Be("");
    }
}
