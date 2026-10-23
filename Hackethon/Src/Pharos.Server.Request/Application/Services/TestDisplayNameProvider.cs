using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Services;

internal class TestDisplayNameProvider : ITestDisplayNameProvider
{
    private readonly IMethodsService _methodsService;
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;

    public TestDisplayNameProvider(IMethodsService methodsService, IOptionsSnapshot<RequestSettings> requestSettings)
    {
        _methodsService = methodsService;
        _requestSettings = requestSettings;
    }

    public async Task<string> GetTestDisplayName(string methodName, string testName)
    {
        var isUsingLabTestNames = _requestSettings.Value.UseLaboratoryTestNames;
        if (!isUsingLabTestNames)
        {
            return testName;
        }

        var laboratoryTestName = await _methodsService.GetMethodTest(
            methodName,
            testName,
            mt => mt.LabTestNames.OrderBy(n => n.SortOrder).Select(mlt => mlt.TestName).FirstOrDefault());
        return laboratoryTestName ?? testName;
    }
}