using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Move;

namespace Pharos.Server.Reagents.Application.Commands;

[Collection(nameof(ReagentsTestsCollection))]
public class ProcessUnloadReagentResultCommandHandlerTests(ReagentTestsWaf factory) : DatabaseTestBase<ReagentTestsWaf>(factory)
{
    private ProcessUnloadReagentResultCommandHandler _sut = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        var dbContext = ServiceProvider.GetRequiredService<IReagentsDbContext>();
        var barcodeService = ServiceProvider.GetRequiredService<IReagentBarcodeService>();
        var methodsService = new Mock<IMethodsService>().Object;
        using var factory = new LoggerFactory();
        var logger = factory.CreateLogger<ProcessUnloadReagentResultCommandHandler>();
        _sut = new ProcessUnloadReagentResultCommandHandler(dbContext, barcodeService, methodsService, logger);
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingReagentQuantity()
    {
        // Arrange
        var barcode = "B1";
        var lotNumber = "L1";
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IReagentsDbContext>();
            var lot = await dbContext.Lots.FirstOrDefaultAsync(l => l.LotNumber == lotNumber, TestContext.Current.CancellationToken);
            if (lot == null)
            {
                lot = new Domain.Lot { LotNumber = lotNumber };
                dbContext.Lots.Add(lot);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
            var reagent = new Domain.Reagent
            {
                Barcode = barcode,
                Udi = "U1",
                ReagentContents = new List<Domain.ReagentContent>
                {
                    new() { Quantity = 0, CellPosition=1 },
                    new() { Quantity = 0, CellPosition=2 }
                },
                Lot = lot,
            };
            dbContext.Reagents.Add(reagent);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var unloaded = new UnloadedReagent
        {
            ReagentBarcode = barcode,
            ReagentUdiBarcode = "U1",
            ReagentType = EnumReagentType.ConjugateCartridge,
        };
        unloaded.ContentCells.Add(new UnloadedReagentCell { Position = 1, Content = new ReagentContent { RemainingQuantity = 10, ExpiryDate = Timestamp.FromDateTimeOffset(DateTime.UtcNow) } });
        unloaded.ContentCells.Add(new UnloadedReagentCell { Position = 2, Content = new ReagentContent { RemainingQuantity = 5, OpenTimestamp = Timestamp.FromDateTimeOffset(DateTime.UtcNow), ExpiryDate = Timestamp.FromDateTimeOffset(DateTime.UtcNow) } });

        var cmd = new ProcessUnloadReagentResultCommand("INSTR1", new UnloadReagentResult { Reagent = unloaded });

        // Act
        await _sut.Handle(cmd, TestContext.Current.CancellationToken);

        // Assert
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IReagentsDbContext>();
            var updated = await dbContext.Reagents.Include(r => r.ReagentContents).FirstAsync(r => r.Barcode == barcode, TestContext.Current.CancellationToken);
            updated.ReagentContents?.First().Quantity.Should().Be(10);
            updated.ReagentContents?.Last().Quantity.Should().Be(0);
        }
    }

    [Fact]
    public async Task Handle_ShouldNotCallCreateReagentCommand_WhenUnloadedIsNull()
    {
        // Arrange
        var cmd = new ProcessUnloadReagentResultCommand("INSTR3", new UnloadReagentResult { Reagent = null });

        // Act & Assert
        await _sut.Handle(cmd, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(EnumReagentType.ConjugateCartridge, 5, 0)]
    [InlineData(EnumReagentType.DevelopmentSolutionCartridge, 2, 0)]
    [InlineData(EnumReagentType.DiluentCartridge, 3, 0)]
    [InlineData(EnumReagentType.QualityControlCartridge, 4, 0)]
    public async Task Handle_ShouldSetQuantityToZeroForCartridgeTypesIfPierced(EnumReagentType type, int expectedFirst, int expectedSecond)
    {
        // Arrange
        var barcode = "B1";
        var lotNumber = "L1";
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IReagentsDbContext>();
            var lot = await dbContext.Lots.FirstOrDefaultAsync(l => l.LotNumber == lotNumber, TestContext.Current.CancellationToken);
            if (lot == null)
            {
                lot = new Domain.Lot { LotNumber = lotNumber };
                dbContext.Lots.Add(lot);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
            var reagent = new Domain.Reagent
            {
                Barcode = barcode,
                Udi = "U1",
                ReagentContents = new List<Domain.ReagentContent>
                {
                    new() { Quantity = 0, CellPosition=1 },
                    new() { Quantity = 0, CellPosition=2 }
                },
                Lot = lot,
            };
            dbContext.Reagents.Add(reagent);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var unloaded = new UnloadedReagent
        {
            ReagentBarcode = barcode,
            ReagentUdiBarcode = "U1",
            ReagentType = type,
        };
        unloaded.ContentCells.Add(new UnloadedReagentCell { Position = 1, Content = new ReagentContent { RemainingQuantity = expectedFirst, ExpiryDate = Timestamp.FromDateTimeOffset(DateTime.UtcNow) } });
        unloaded.ContentCells.Add(new UnloadedReagentCell { Position = 2, Content = new ReagentContent { RemainingQuantity = expectedSecond, OpenTimestamp = Timestamp.FromDateTimeOffset(DateTime.UtcNow), ExpiryDate = Timestamp.FromDateTimeOffset(DateTime.UtcNow) } });

        var cmd = new ProcessUnloadReagentResultCommand("INSTR1", new UnloadReagentResult { Reagent = unloaded });

        // Act
        await _sut.Handle(cmd, TestContext.Current.CancellationToken);

        // Assert
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IReagentsDbContext>();
            var updated = await dbContext.Reagents.Include(r => r.ReagentContents).FirstAsync(r => r.Barcode == barcode, TestContext.Current.CancellationToken);
            updated.ReagentContents?[0].Quantity.Should().Be(expectedFirst);
            updated.ReagentContents?[1].Quantity.Should().Be(expectedSecond);
        }
    }

    [Theory]
    [InlineData(6, 0, 1)]  // 6 unpierced cells = 1 set
    [InlineData(12, 0, 2)] // 12 unpierced cells = 2 sets
    [InlineData(5, 0, 0)]  // 5 unpierced cells = 0 sets (need at least 6)
    [InlineData(7, 0, 1)]  // 7 unpierced cells = 1 set (floor division)
    public async Task Handle_ShouldCalculateSetsQuantityForCalibratorCartridge(int unpiercedCells, int piercedCells, int expectedSets)
    {
        // Arrange
        var barcode = "B1";
        var lotNumber = "L1";
        var totalCells = unpiercedCells + piercedCells;
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IReagentsDbContext>();
            var lot = await dbContext.Lots.FirstOrDefaultAsync(l => l.LotNumber == lotNumber, TestContext.Current.CancellationToken);
            if (lot == null)
            {
                lot = new Domain.Lot { LotNumber = lotNumber };
                dbContext.Lots.Add(lot);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
            var reagentContents = new List<Domain.ReagentContent>();
            for (var i = 1; i <= totalCells; i++)
            {
                reagentContents.Add(new Domain.ReagentContent { Quantity = 0, CellPosition = i });
            }
            var reagent = new Domain.Reagent
            {
                Barcode = barcode,
                Udi = "U1",
                ReagentContents = reagentContents,
                Lot = lot,
            };
            dbContext.Reagents.Add(reagent);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var unloaded = new UnloadedReagent
        {
            ReagentBarcode = barcode,
            ReagentUdiBarcode = "U1",
            ReagentType = EnumReagentType.CalibratorCartridge,
        };

        // Add unpierced cells (no OpenTimestamp)
        for (var i = 1; i <= unpiercedCells; i++)
        {
            unloaded.ContentCells.Add(new UnloadedReagentCell
            {
                Position = i,
                Content = new ReagentContent { RemainingQuantity = 10, ExpiryDate = Timestamp.FromDateTimeOffset(DateTime.UtcNow) }
            });
        }

        // Add pierced cells (with OpenTimestamp)
        for (var i = unpiercedCells + 1; i <= totalCells; i++)
        {
            unloaded.ContentCells.Add(new UnloadedReagentCell
            {
                Position = i,
                Content = new ReagentContent { RemainingQuantity = 0, OpenTimestamp = Timestamp.FromDateTimeOffset(DateTime.UtcNow), ExpiryDate = Timestamp.FromDateTimeOffset(DateTime.UtcNow) }
            });
        }

        var cmd = new ProcessUnloadReagentResultCommand("INSTR1", new UnloadReagentResult { Reagent = unloaded });

        // Act
        await _sut.Handle(cmd, TestContext.Current.CancellationToken);

        // Assert - quantity is stored as sets in first cell only
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IReagentsDbContext>();
            var updated = await dbContext.Reagents.Include(r => r.ReagentContents).FirstAsync(r => r.Barcode == barcode, TestContext.Current.CancellationToken);
            updated.ReagentContents?.First(c => c.CellPosition == 1).Quantity.Should().Be(expectedSets);
        }
    }
}
