using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Commands.UpdateSettings;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Validation;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Commands;

public class UpdateSettingCommandTests_ConcurrentCreation
{
    private readonly Mock<ISettingsDbContext> _dbContextMock;
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<IPageListMaxItemsValidator> _validatorMock;
    private readonly UpdateSettingCommandHandler _sut;
    private readonly Mock<IAbsoluteDirectoryWriteAccessValidator> _mockWriteAccessValidator = new();

    public UpdateSettingCommandTests_ConcurrentCreation()
    {
        _dbContextMock = new Mock<ISettingsDbContext>();
        _senderMock = new Mock<ISender>();
        _validatorMock = new Mock<IPageListMaxItemsValidator>();
        _mockWriteAccessValidator = new Mock<IAbsoluteDirectoryWriteAccessValidator>();

        _sut = new UpdateSettingCommandHandler(
            _dbContextMock.Object,
            Mock.Of<ILogger<UpdateSettingCommandHandler>>(),
            _senderMock.Object,
            _validatorMock.Object, _mockWriteAccessValidator.Object);
    }

    [Fact]
    public async Task Handle_ConcurrentCreation_MaxLimitReached_ReturnsMaxItemsExceeded()
    {
        // Arrange: Computer 1 starts editing when there are 9 entries, Computer 2 creates the 10th
        var page = CreateTestPage("Lis:LisChannels:10");
        var request = new UpdateSettingCommand(null, page);

        // Initial state: 9 entries exist (what Computer 1 saw at start)
        var initialSettings = CreateSettings("Lis:LisChannels", 0, 9);

        // Current state: 10 entries exist (Computer 2 created one)
        var currentSettings = CreateSettings("Lis:LisChannels", 0, 10);

        SetupDbContext(initialSettings, currentSettings);
        SetupValidator(maxItems: 10, isValid: false);

        // Act
        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal(SettingUpdateError.MaxItemsExceeded, result.Errors[0].Error);
        Assert.Equal("Lis:LisChannels", result.Errors[0].Key);
    }

    [Fact]
    public async Task Handle_ConcurrentCreation_SameKeyConflict_ReturnsConcurrentModification()
    {
        // Arrange: Both computers try to create entry with key "Lis:LisChannels:5"
        var page = CreateTestPage("Lis:LisChannels:5");
        var request = new UpdateSettingCommand(null, page);

        // Initial state: Entry 5 doesn't exist
        var initialSettings = CreateSettings("Lis:LisChannels", 0, 4); // 0-4 exist

        // Current state: Entry 5 now exists (Computer 2 created it)
        var currentSettings = CreateSettings("Lis:LisChannels", 0, 5); // 0-5 exist

        SetupDbContext(initialSettings, currentSettings);
        SetupValidator(maxItems: 10, isValid: true); // Not at max limit

        // Act
        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal(SettingUpdateError.ConcurrentModification, result.Errors[0].Error);
        Assert.Equal("Lis:LisChannels:5", result.Errors[0].Key);
    }

    [Fact]
    public async Task Handle_MaxLimitAndConcurrentConflict_BothTrue_PrioritizesMaxItemsError()
    {
        // Arrange: Max limit reached AND same key conflict (both conditions true)
        var page = CreateTestPage("Lis:LisChannels:10");
        var request = new UpdateSettingCommand(null, page);

        // Initial state: 9 entries
        var initialSettings = CreateSettings("Lis:LisChannels", 0, 9);

        // Current state: Entry 10 exists AND we're at max (10 entries)
        var currentSettings = CreateSettings("Lis:LisChannels", 0, 10);

        SetupDbContext(initialSettings, currentSettings);
        SetupValidator(maxItems: 10, isValid: false);

        // Act
        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert: Should return MaxItemsExceeded (higher priority)
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal(SettingUpdateError.MaxItemsExceeded, result.Errors[0].Error);
        Assert.NotEqual(SettingUpdateError.ConcurrentModification, result.Errors[0].Error);
    }

    // Helper methods

    private static Page CreateTestPage(string key)
    {
        return new Page
        {
            Key = key,
            Label = "Test",
            Children = new List<SettingsChildItem>
            {
                new Option { Key = $"{key}:Name", Value = "TestValue" }
            }
        };
    }

    private static List<Setting> CreateSettings(string prefix, int startIndex, int endIndex)
    {
        var settings = new List<Setting>();
        for (var i = startIndex; i <= endIndex; i++)
        {
            settings.Add(new Setting
            {
                Key = $"{prefix}:{i}:Name",
                Value = $"Value{i}"
            });
        }
        return settings;
    }

    private void SetupDbContext(List<Setting> initialSettings, List<Setting> currentSettings)
    {
        // Initial fetch
        var initialDbSet = CreateMockDbSet(initialSettings);

        // Current fetch during validation
        var currentDbSet = CreateMockDbSet(currentSettings);

        var callCount = 0;
        _dbContextMock.Setup(x => x.Settings)
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? initialDbSet.Object : currentDbSet.Object;
            });

        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static Mock<DbSet<Setting>> CreateMockDbSet(List<Setting> data)
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<Setting>>();

        mockSet.As<IQueryable<Setting>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Setting>(queryable.Provider));
        mockSet.As<IQueryable<Setting>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<Setting>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<Setting>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        mockSet.As<IAsyncEnumerable<Setting>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<Setting>(queryable.GetEnumerator()));

        return mockSet;
    }

    private void SetupValidator(int? maxItems, bool isValid)
    {
        _validatorMock.Setup(x => x.ValidateCanAddAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(isValid
                ? PageListMaxItemsValidationResult.Success(10, maxItems)
                : PageListMaxItemsValidationResult.Failure($"Max {maxItems} reached", 10, maxItems));
    }
}

// Helper classes for async enumeration in tests
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: [typeof(System.Linq.Expressions.Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(this, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _enumerator;

    public TestAsyncEnumerator(IEnumerator<T> enumerator)
    {
        _enumerator = enumerator;
    }

    public ValueTask<bool> MoveNextAsync() => new(_enumerator.MoveNext());

    public T Current => _enumerator.Current;

    public ValueTask DisposeAsync()
    {
        _enumerator.Dispose();
        return ValueTask.CompletedTask;
    }
}
