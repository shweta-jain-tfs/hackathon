using System.ComponentModel.DataAnnotations;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Commands.UpdateInstrument;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands;

public class UpdateInstrumentCommandHandlerTests
{
    private readonly Mock<IInstrumentDbContext> _dbContextMock;
    private readonly UpdateInstrumentCommandHandler _sut;
    private readonly Dictionary<int, Domain.Instrument> _existingInstruments = new()
    {
        { 1, new Domain.Instrument { Id = 1, DisplayName = "ExistingInstrument" } },
        { 2, new Domain.Instrument { Id = 2, DisplayName = "AnotherInstrument" } }
    };

    public UpdateInstrumentCommandHandlerTests()
    {
        var instrumentManagerMock = new Mock<IInstrumentManager>();

        _dbContextMock = new Mock<IInstrumentDbContext>();
        _dbContextMock.Setup(x => x.Instruments).ReturnsDbSet(_existingInstruments.Values);
        _dbContextMock.Setup(x => x.HasChanges()).Returns(true);
        instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<int>())).Returns((int id) => _existingInstruments[id]);

        _sut = new UpdateInstrumentCommandHandler(instrumentManagerMock.Object, _dbContextMock.Object, new Mock<ICommunicationSessionManager>().Object);
    }

    /// <summary>
    /// Should not be possible to rename instrument if another instrument has the same name.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateInstrument_Name_Exists_On_Another_Instrument_Should_ThrowException()
    {
        // Setup.
        var request = new UpdateInstrumentCommand
        {
            Id = 2,
            DisplayName = "ExistingInstrument",
            Enabled = true
        };

        // Verify
        await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(request, TestContext.Current.CancellationToken));
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// <summary>
    /// Should not be possible to rename instrument if another instrument has the same name. Not even with different case.
    /// </summary>
    /// <returns></returns>
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateInstrument_Name_Exists_With_Different_Case_On_Another_Instrument_Should_ThrowException()
    {
        // Setup.
        var request = new UpdateInstrumentCommand
        {
            Id = 2,
            DisplayName = "EXISTINGinstrument",
            Enabled = true
        };

        // Verify
        await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(request, TestContext.Current.CancellationToken));
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Should save when changing an instrument name to upper case.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateInstrument_Name_Change_To_Uppercase_Should_Save()
    {
        // Setup.
        var request = new UpdateInstrumentCommand
        {
            Id = 1,
            DisplayName = "EXISTINGINSTRUMENT",
            Enabled = true
        };

        // Verify
        await _sut.Handle(request, TestContext.Current.CancellationToken);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Should save when changing an instrument name to a unique name.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateInstrument_Name_Unique_Should_Save()
    {
        // Setup.
        var request = new UpdateInstrumentCommand
        {
            Id = 2,
            DisplayName = "InstrumentNewName",
            Enabled = true
        };

        // Verify.
        await _sut.Handle(request, TestContext.Current.CancellationToken);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
