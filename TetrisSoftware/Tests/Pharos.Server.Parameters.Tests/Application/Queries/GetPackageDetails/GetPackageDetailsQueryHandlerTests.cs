using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Queries.GetPackages;

namespace Pharos.Server.Parameters.Application.Queries.GetPackageDetails
{
    public class GetPackageDetailsQueryHandlerTests
    {
        private readonly Mock<IParametersDbContext> _mockContext;
        private readonly GetPackageDetailsQueryHandler _handler;

        public GetPackageDetailsQueryHandlerTests()
        {
            // Create a mock of the database context
            _mockContext = new Mock<IParametersDbContext>();

            // Inject the mocked context into the handler
            _handler = new GetPackageDetailsQueryHandler(_mockContext.Object);
        }

        [Fact]
        public async Task Handle_ReturnsCorrectPackageDetails()
        {
            // Arrange

            // This test verifies that the handler maps values from a mocked Package entity
            // correctly into the resulting PackageDetailsDto.
            // The value "Europe" for IntendedMarket.Name is arbitrary and does not reflect real database contents,
            // but is used here to assert correct mapping logic in isolation from the database.

            var packageId = 1;

            // Create a mock package entity with an intended market
            var package = new Package
            {
                Id = packageId,
                PackageBarcode = "350",
                OrderingNumber = "14-4100-01",
                PackageFullName = "Timothy",
                IntendedMarket = new IntendedMarket { Name = "Europe" } // IntendedMarket.Name is mocked
            };

            // Create a mock article entity
            var article = new Article
            {
                Id = 1,
                Identity = "g1",
                FullName = "g1",
                ArticleNumber = "030",
                ArticleType = ArticleType.ImmunoCapCarrier,
                NumberOfCells = 12,
                MethodName = "ELISA",
                MaxDaysInInstrument = 5,
                MaxHoursInInstrument = 48
            };

            // Mock package contents linking the article to the package
            var packageContents = new List<PackageItem>
            {
                new() { PackageId = packageId, ArticleAmount = 16, Article = article }
            };

            // Setup mock context to return the above package and contents
            _mockContext.Setup(c => c.Packages).ReturnsDbSet(new List<Package> { package });
            _mockContext.Setup(c => c.PackageItems).ReturnsDbSet(packageContents);

            var query = new GetPackageDetailsQuery(packageId);

            // Act
            var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("350", result.PackageBarcode); // Check that the barcode matches the mock value
            Assert.Equal("Europe", result.IntendedMarket); // Check that the intended market name was mapped
            Assert.Single(result.PackageItems); // Ensure only one article is returned
            Assert.Equal("g1", result.PackageItems[0].FullName); // Validate the article full name
        }

        [Fact]
        public async Task Handle_ThrowsException_WhenPackageNotFound()
        {
            // Arrange

            var packageId = 999; // Non-existing package ID for test

            // Simulate no matching package in the database
            _mockContext.Setup(c => c.Packages).ReturnsDbSet(new List<Package>()); // Empty list means not found

            var query = new GetPackageDetailsQuery(packageId);

            // Act & Assert

            // Expect the handler to throw an exception due to missing package
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _handler.Handle(query, TestContext.Current.CancellationToken));
        }
    }
}
