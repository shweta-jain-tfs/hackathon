using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Integration.Tests.WafDependentLocalTests.Pharos.Server.Application.Inventory;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class GetCartridgeContentsQueryTests : IntegrationTestBase
{
    private readonly DateTimeOffset _lotExpiryDate1 = new(2022, 3, 5, 0, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _lotExpiryDate2 = new(2022, 3, 6, 0, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _openTimestamp1 = new(2022, 3, 5, 1, 3, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _openTimestamp2 = new(2022, 3, 4, 2, 4, 0, TimeSpan.Zero);

    public GetCartridgeContentsQueryTests(IntegrationTestsWaf factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetNewCartridgeContents()
    {
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new GetCartridgeContentsQuery(new[]
        {
            "ABCE25B9355R",
            "DEFE26B9255S",
            "wrong_barcode"
        }), TestContext.Current.CancellationToken);

        result.Count.Should().Be(2);

        var reagentDto = result["ABCE25B9355R"];
        reagentDto.Should().NotBeNull();
        reagentDto.ArticleNumber.Should().Be("ABC");
        reagentDto.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        reagentDto.LotNumber.Should().Be("ABCB9");
        reagentDto.LotExpiryDate.Should().Be(_lotExpiryDate1);
        reagentDto.ContentCells.Should().HaveCount(6);
        reagentDto.ContentCells[0].Position.Should().Be(1);
        reagentDto.ContentCells[0].RemainingQuantity.Should().Be(96);
        reagentDto.ContentCells[0].OpenTimestamp.Should().BeNull();

        reagentDto = result["DEFE26B9255S"];
        reagentDto.ArticleNumber.Should().Be("DEF");
        reagentDto.ArticleType.Should().Be(ArticleType.DevelopmentSolutionCartridge);
        reagentDto.LotNumber.Should().Be("DEFB9");
        reagentDto.LotExpiryDate.Should().Be(_lotExpiryDate2);
        reagentDto.ContentCells.Should().HaveCount(3);
        reagentDto.ContentCells[0].RemainingQuantity.Should().Be(56);
    }

    [Fact]
    public async Task GetExistingCartridgeContents()
    {
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new GetCartridgeContentsQuery(new[]
        {
            "ABCE25B9255R",
            "ABCE25B9255S"
        }), TestContext.Current.CancellationToken);

        var reagentDto = result["ABCE25B9255R"];
        reagentDto.Should().NotBeNull();
        reagentDto.ArticleNumber.Should().Be("ABC");
        reagentDto.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        reagentDto.LotNumber.Should().Be("ABCB9");
        reagentDto.LotExpiryDate.Should().Be(_lotExpiryDate1);
        reagentDto.ContentCells.Should().HaveCount(1);
        reagentDto.ContentCells[0].RemainingQuantity.Should().Be(9);
        reagentDto.ContentCells[0].OpenTimestamp.Should().Be(_openTimestamp1);

        reagentDto = result["ABCE25B9255S"];
        reagentDto.ArticleNumber.Should().Be("ABC");
        reagentDto.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        reagentDto.LotNumber.Should().Be("ABCB9");
        reagentDto.LotExpiryDate.Should().Be(_lotExpiryDate1);
        reagentDto.ContentCells[0].RemainingQuantity.Should().Be(20);
        reagentDto.ContentCells[0].OpenTimestamp.Should().Be(_openTimestamp2);
    }

    [Fact]
    public async Task GetNewAndExistingCartridgeContents()
    {
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new GetCartridgeContentsQuery(new[]
        {
            "ABCE25B9255R",
            "DEFE26B9255S"
        }), TestContext.Current.CancellationToken);

        var reagentDto = result["ABCE25B9255R"];
        reagentDto.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        reagentDto.LotExpiryDate.Should().Be(_lotExpiryDate1);
        reagentDto.ContentCells.Should().HaveCount(1);
        reagentDto.ContentCells[0].RemainingQuantity.Should().Be(9);
        reagentDto.ContentCells[0].OpenTimestamp.Should().Be(_openTimestamp1);

        reagentDto = result["DEFE26B9255S"];
        reagentDto.ArticleType.Should().Be(ArticleType.DevelopmentSolutionCartridge);
        reagentDto.LotExpiryDate.Should().Be(_lotExpiryDate2);
        reagentDto.ContentCells.Should().HaveCount(3);
        reagentDto.ContentCells[0].RemainingQuantity.Should().Be(56);
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        var appDbContext = ServiceProvider.GetRequiredService<IReagentsDbContext>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();

        var article1 = new Article // fictional
        {
            ArticleNumber = "ABC",
            ArticleType = ArticleType.ConjugateCartridge,
            FullName = "ImmunoCap Conjugate 50",
            Quantity = 96,
            Identity = "sIgE",
            NumberOfCells = 6,
        };
        var article2 = new Article // fictional
        {
            ArticleNumber = "DEF",
            ArticleType = ArticleType.DevelopmentSolutionCartridge,
            FullName = "Development Solution",
            Quantity = 56,
            NumberOfCells = 3,
        };

        paramDbContext.Articles.AddRange(article1, article2);

        appDbContext.Lots.Add(
            new Lot
            {
                ArticleType = ArticleType.ConjugateCartridge,
                LotNumber = "ABCB9",
                ExpiryDate = _lotExpiryDate1,
                Reagents = new List<Reagent>()
                {
                    new()
                    {
                        Barcode = "ABCE25B9255R",
                        ReagentContents = new List<ReagentContent>
                        {
                            new()
                            {
                                CellPosition = 1,
                                Quantity = 9,
                                OpenTimestamp = _openTimestamp1
                            }
                        }
                    },
                    new()
                    {
                        Barcode = "ABCE25B9255S",
                        ReagentContents = new List<ReagentContent>
                        {
                            new()
                            {
                                CellPosition = 1,
                                Quantity = 20,
                                OpenTimestamp = _openTimestamp2
                            }
                        }
                    }
                }
            });

        await paramDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await appDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}