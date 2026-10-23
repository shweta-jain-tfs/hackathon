using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.LoadList.Application.Services;
using Pharos.Server.LoadList.Domain;
using Pharos.Server.LoadList.Infrastructure.Database;

namespace Pharos.Server.LoadList.Tests.Application.Services;

public class LoadListConfigurationInitializerServiceTests
{
    private const string _instrumentSerial = "SEED_001";

    private readonly TestLoadListDbContext _dbContext;
    private readonly Mock<IMethodsService> _methodsServiceMock = new();
    private readonly Mock<IArticlesService> _articlesServiceMock = new();
    private readonly LoadListConfigurationInitializerService _sut;

    public LoadListConfigurationInitializerServiceTests()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        _dbContext = new TestLoadListDbContext(config);
        _dbContext.Database.EnsureCreated();

        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, Article>>>()))
            .ReturnsAsync(new List<Article>());

        _sut = new LoadListConfigurationInitializerService(
            _dbContext,
            _methodsServiceMock.Object,
            _articlesServiceMock.Object);
    }

    [Fact]
    public async Task InitializeForInstrument_AddsActiveCarriers()
    {
        var method = BuildMethod(
            "sIgE",
            new[]
            {
                BuildTest("c1", true, "D1", "DEV1"),
                BuildTest("c2", true, "D2", null),
                BuildTest("c3", false, "D3", "DEV2")
            },
            Array.Empty<MethodReagent>());

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method> { method });
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, Article>>>()))
            .ReturnsAsync(new List<Article>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync(new List<string?>());

        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity(It.IsAny<string?>(), It.IsAny<string?>(), null))
            .ReturnsAsync((string? identity, string? _, ArticleType? _) => new Article
            {
                ArticleNumber = "AAA",
                ArticleType = ArticleType.None,
                FriendlyName = "FN-" + identity,
                Quantity = 1
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        configs.Should().Contain(c => c.Identity == "c1");
        configs.Should().Contain(c => c.Identity == "c2");
        configs.Should().NotContain(c => c.Identity == "c3");

        var c1 = configs.First(c => c.Identity == "c1");
        c1.ArticleNumber.Should().Be("AAA");
        c1.FriendlyName.Should().Be("FN-c1");
    }

    [Fact]
    public async Task InitializeForInstrument_RemovesObsolete_AddsNew()
    {
        _dbContext.LoadListConfigurations.AddRange(
            new LoadListConfiguration { InstrumentSerial = _instrumentSerial, Identity = "obsolete", ArticleType = ArticleType.ImmunoCapCarrier, ArticleNumber = "ABC"},
            new LoadListConfiguration { InstrumentSerial = _instrumentSerial, Identity = "c1", ArticleType = ArticleType.ImmunoCapCarrier, ArticleNumber = "123"});
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var method = BuildMethod(
            "m1",
            new[]
            {
                BuildTest("c1", true, "D1", null),
                BuildTest("c2", true, "D2", null)
            },
            Array.Empty<MethodReagent>());

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method> { method });
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, Article>>>()))
            .ReturnsAsync(new List<Article>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync(new List<string?>());
        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity(It.IsAny<string?>(), It.IsAny<string?>(), null))
            .ReturnsAsync((string? identity, string? _, ArticleType? _) => new Article
            {
                ArticleNumber = identity + "-NUM",
                ArticleType = ArticleType.None,
                Quantity = 1
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking()
            .Where(x => x.InstrumentSerial == _instrumentSerial).ToListAsync(TestContext.Current.CancellationToken);

        configs.Should().NotContain(c => c.Identity == "obsolete");
        configs.Should().Contain(c => c.Identity == "c1");
        configs.Should().Contain(c => c.Identity == "c2");
    }

    [Fact]
    public async Task InitializeForInstrument_AddsReagentQcDiluentDevelopment()
    {
        var method = BuildMethod(
            "mCalc",
            new[] { BuildTest("t1", true, "D1", "DEV1") },
            new[]
            {
                BuildReagent(ReagentType.Calibrator, "CAL-ID"),
                BuildReagent(ReagentType.CurveControl, "CC-ID"),
                BuildReagent(ReagentType.Conjugate, "CONJ-DEFAULT")
            });

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method> { method });
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string> { "QC1" });
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, Article>>>()))
            .ReturnsAsync(new List<Article>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync(new List<string?>());
        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ArticleType?>()))
            .ReturnsAsync((string? _, string? _, ArticleType? type) => new Article
            {
                ArticleNumber = "123",
                ArticleType = type ?? ArticleType.None,
                Quantity = 1
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        configs.Should().Contain(c => c.ArticleType == ArticleType.CalibratorCartridge && c.MethodName == "mCalc");
        configs.Should().Contain(c => c.ArticleType == ArticleType.CurveControlCartridge && c.MethodName == "mCalc");
        configs.Should().Contain(c => c.ArticleType == ArticleType.ConjugateCartridge && c.Identity == "CONJ-DEFAULT");
        configs.Should().Contain(c => c.ArticleType == ArticleType.QualityControlCartridge && c.Identity == "QC1");
        configs.Should().Contain(c => c.ArticleType == ArticleType.DiluentCartridge && c.Identity == "D1");
        configs.Should().Contain(c => c.ArticleType == ArticleType.DevelopmentSolutionCartridge && c.Identity == "DEV1");
    }

    [Fact]
    public async Task InitializeForInstrument_SkipsCarrier_WhenArticleLookupReturnsNull()
    {
        var method = BuildMethod(
            "mSkip",
            new[] { BuildTest("t1", true, "D1", null) },
            Array.Empty<MethodReagent>());

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method> { method });
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, Article>>>()))
            .ReturnsAsync(new List<Article>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync(new List<string?>());

        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity("t1", It.IsAny<string?>(), null))
            .ReturnsAsync((Article?)null);

        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity("D1", It.IsAny<string?>(), ArticleType.DiluentCartridge))
            .ReturnsAsync(new Article
            {
                ArticleNumber = "D1-NUM",
                ArticleType = ArticleType.DiluentCartridge,
                Quantity = 1
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        configs.Should().NotContain(c => c.Identity == "t1");
        configs.Should().Contain(c => c.Identity == "D1" && c.ArticleType == ArticleType.DiluentCartridge);
    }

    [Fact]
    public async Task InitializeForInstrument_AddsStopSolutionBottles()
    {
        var stopBottles = new List<Article>
        {
            new()
            {
                ArticleNumber = "STOP-001",
                ArticleType = ArticleType.StopSolutionBottle,
                Identity = "StopSolution1",
                FriendlyName = "Stop Solution 1",
                Quantity = 1,
                Shape = ArticleIndicatorShape.Rectangular
            },
            new()
            {
                ArticleNumber = "STOP-002",
                ArticleType = ArticleType.StopSolutionBottle,
                Identity = "StopSolution2",
                FriendlyName = "Stop Solution 2",
                Quantity = 1
            }
        };

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method>());
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string>());

        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync((Expression<Func<Article, bool>> predicate, Expression<Func<Article, string?>> selector) =>
            {
                var compiledPredicate = predicate.Compile();
                var compiledSelector = selector.Compile();
                return stopBottles.Where(compiledPredicate).Select(compiledSelector).ToList();
            });

        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity(It.IsAny<string?>(), It.IsAny<string?>(), ArticleType.StopSolutionBottle))
            .ReturnsAsync((string? identity, string? _, ArticleType? _) =>
            {
                return stopBottles.FirstOrDefault(a => a.Identity == identity);
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        configs.Should().Contain(c => c.ArticleNumber == "STOP-001" && c.ArticleType == ArticleType.StopSolutionBottle);
        configs.Should().Contain(c => c.ArticleNumber == "STOP-002" && c.ArticleType == ArticleType.StopSolutionBottle);
        
        var stop1 = configs.First(c => c.ArticleNumber == "STOP-001");
        stop1.FriendlyName.Should().Be("Stop Solution 1");
        stop1.Shape.Should().Be(ArticleIndicatorShape.Rectangular);
    }

    [Fact]
    public async Task InitializeForInstrument_RemovesObsoleteStopBottles()
    {
        _dbContext.LoadListConfigurations.Add(
            new LoadListConfiguration
            {
                InstrumentSerial = _instrumentSerial,
                Identity = "OldStop",
                ArticleNumber = "STOP-OLD",
                ArticleType = ArticleType.StopSolutionBottle
            });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stopBottles = new List<Article>
        {
            new()
            {
                ArticleNumber = "STOP-NEW",
                ArticleType = ArticleType.StopSolutionBottle,
                Identity = "NewStop",
                Quantity = 1
            }
        };

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method>());
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string>());

        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync((Expression<Func<Article, bool>> predicate, Expression<Func<Article, string?>> selector) =>
            {
                var compiledPredicate = predicate.Compile();
                var compiledSelector = selector.Compile();
                return stopBottles.Where(compiledPredicate).Select(compiledSelector).ToList();
            });

        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity(It.IsAny<string?>(), It.IsAny<string?>(), ArticleType.StopSolutionBottle))
            .ReturnsAsync((string? identity, string? _, ArticleType? _) =>
            {
                return stopBottles.FirstOrDefault(a => a.Identity == identity);
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        configs.Should().NotContain(c => c.ArticleNumber == "STOP-OLD");
        configs.Should().Contain(c => c.ArticleNumber == "STOP-NEW");
    }

    [Fact]
    public async Task InitializeForInstrument_AddsMethodSpecificBottles()
    {
        var method = BuildMethod(
            "m1",
            Array.Empty<MethodTest>(),
            new[]
            {
                BuildReagent(ReagentType.WashSolutionConcentrate,  "WASH-CONC"),
                BuildReagent(ReagentType.WashAdditive,"WASH-ADD")
            });

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method> { method });
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, Article>>>()))
            .ReturnsAsync(new List<Article>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync(new List<string?>());
        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ArticleType?>()))
            .ReturnsAsync((string? identity, string? _, ArticleType? type) => new Article
            {
                ArticleNumber = identity + "-NUM",
                ArticleType = type ?? ArticleType.None,
                Quantity = 1
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        configs.Should().Contain(c => c.Identity == "WASH-CONC" && c.ArticleType == ArticleType.WashConcentrateBottle);
        configs.Should().Contain(c => c.Identity == "WASH-ADD" && c.ArticleType == ArticleType.WashAdditiveBottle);
    }

    [Fact]
    public async Task InitializeForInstrument_HandlesMultipleMethodsWithSameReagents()
    {
        var method1 = BuildMethod(
            "m1",
            new[] { BuildTest("t1", true, "D1", null) },
            new[] { BuildReagent(ReagentType.Calibrator, "CAL1") });

        var method2 = BuildMethod(
            "m2",
            new[] { BuildTest("t2", true, "D1", null) },
            new[] { BuildReagent(ReagentType.Calibrator, "CAL2") });

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method> { method1, method2 });
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, Article>>>()))
            .ReturnsAsync(new List<Article>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync(new List<string?>());
        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ArticleType?>()))
            .ReturnsAsync((string? identity, string? methodName, ArticleType? type) => new Article
            {
                ArticleNumber = (identity ?? methodName ?? "X") + "-NUM",
                ArticleType = type ?? ArticleType.None,
                Quantity = 1
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        
        // Should have calibrators for both methods
        configs.Should().Contain(c => c.ArticleType == ArticleType.CalibratorCartridge && c.MethodName == "m1");
        configs.Should().Contain(c => c.ArticleType == ArticleType.CalibratorCartridge && c.MethodName == "m2");
        
        // Should have diluent D1 only once (shared by both methods)
        configs.Where(c => c.Identity == "D1" && c.ArticleType == ArticleType.DiluentCartridge).Should().HaveCount(1);
    }

    [Fact]
    public async Task InitializeForInstrument_EnrichesConfigurationWithArticleData()
    {
        var method = BuildMethod(
            "m1",
            new[] { BuildTest("t1", true, "D1", null) },
            Array.Empty<MethodReagent>());

        _methodsServiceMock.Setup(m => m.GetDetailedMethods()).ReturnsAsync(new List<Method> { method });
        _methodsServiceMock.Setup(m => m.GetPhadiaQualityControlCartridgeNames()).ReturnsAsync(new HashSet<string>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, Article>>>()))
            .ReturnsAsync(new List<Article>());
        _articlesServiceMock.Setup(a => a.GetArticles(It.IsAny<Expression<Func<Article, bool>>>(), It.IsAny<Expression<Func<Article, string?>>>()))
            .ReturnsAsync(new List<string?>());
        _articlesServiceMock.Setup(a => a.GetArticleFromIdentity(It.IsAny<string?>(), It.IsAny<string?>(), null))
            .ReturnsAsync((string? identity, string? _, ArticleType? _) => new Article
            {
                ArticleNumber = identity + "-NUM",
                ArticleType = ArticleType.ImmunoCapCarrier,
                FriendlyName = "Friendly " + identity,
                Quantity = 5,
                Shape = ArticleIndicatorShape.Rectangular,
                Text1 = "Text1Value"
            });

        await _sut.InitializeForInstrument(_instrumentSerial, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configs = await _dbContext.LoadListConfigurations.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        var t1Config = configs.First(c => c.Identity == "t1");
        
        t1Config.ArticleNumber.Should().Be("t1-NUM");
        t1Config.ArticleType.Should().Be(ArticleType.ImmunoCapCarrier);
        t1Config.FriendlyName.Should().Be("Friendly t1");
        t1Config.Quantity.Should().Be(5);
        t1Config.Shape.Should().Be(ArticleIndicatorShape.Rectangular);
        t1Config.Unit.Should().Be(ArticleQuantityUnit.Tests);
        t1Config.Text1.Should().Be("Text1Value");
    }

    #region Builders

    private static Method BuildMethod(string methodName, IEnumerable<MethodTest> tests, IEnumerable<MethodReagent> reagents)
    {
        var m = new Method
        {
            MethodName = methodName,
            Tests = tests.ToList(),
            Reagents = reagents.ToList()
        };
        foreach (var r in m.Reagents)
        {
            r.Method = m;
        }

        return m;
    }

    private static MethodTest BuildTest(string testName, bool active, string diluent, string? devSol)
    {
        return new MethodTest
        {
            TestName = testName,
            Active = active,
            DiluentIdentity = diluent,
            DevelopmentSolutionIdentity = devSol
        };
    }

    private static MethodReagent BuildReagent(ReagentType type, string? defaultIdentity = null)
    {
        return new MethodReagent
        {
            ReagentType = type,
            DefaultReagentIdentity = defaultIdentity
        };
    }

    #endregion

    private sealed class TestLoadListDbContext : LoadListDbContext
    {
        public TestLoadListDbContext(IConfiguration configuration)
            : base(configuration, null, null) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            }
        }
    }
}