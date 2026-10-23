using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.ImportPanels;

/// <summary>
/// Handler for updating panels in the database from imported panels
/// </summary>
internal class UpdatePanelParametersHandler(IParametersDbContext dbContext)
{
    /// <summary>
    /// Update all domain panels from import
    /// </summary>
    /// <param name="importedPanels">List of imported  panels</param>
    public async Task Handle(List<Panel> importedPanels)
    {
        var panelsFromDatabase = await dbContext.Panels.AsQueryable().ToListAsync();
        foreach (var importedPanel in importedPanels)
        {
            var panel = panelsFromDatabase.FirstOrDefault(x => x.PanelName == importedPanel.PanelName);
            if (panel == null)
            {
                panel = new Panel
                {
                    PanelName = importedPanel.PanelName,
                    GroupedPanel = importedPanel.GroupedPanel,
                    PanelLongName = importedPanel.PanelLongName
                };
                dbContext.Panels.Add(panel);
            }

            await UpdatePanelFromImport(panel, importedPanel);
        }

        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Update one domain panel from an imported panel
    /// </summary>
    private async Task UpdatePanelFromImport(Panel panel, Panel importedPanel)
    {
        var sortOrder = 1;
        foreach (var importedPanelTest in importedPanel.Tests)
        {
            await UpdatePanelTest(panel, importedPanelTest, sortOrder++);
        }
    }

    /// <summary>
    /// Update one domain panel test with data from import
    /// </summary>
    /// <param name="panel">Domain panel</param>
    /// <param name="importedPanelTest">Imported panel test</param>
    /// <param name="sortOrder">Test sort order to use if not supplied by the import</param>
    private async Task UpdatePanelTest(Panel panel, PanelTest importedPanelTest, int sortOrder)
    {
        var methodTest = await GetMethodTest(importedPanelTest);

        var panelTest = await GetPanelTest(panel, importedPanelTest);
        UpdatePanelTestParameters(panelTest, methodTest, importedPanelTest, sortOrder);
    }

    /// <summary>
    /// Retrieve a method test corresponding to the imported panel test
    /// </summary>
    /// <param name="importedPanelTest">Imported panel test</param>
    /// <returns>Domain method test</returns>
    private async Task<MethodTest> GetMethodTest(PanelTest importedPanelTest)
    {
        var methodTest = await dbContext.MethodTests
            .Where(mt => mt.Method.MethodName == importedPanelTest.MethodTest.Method.MethodName && mt.TestName == importedPanelTest.MethodTest.TestName)
            .FirstAsync();
        return methodTest;
    }

    /// <summary>
    /// Retrieve one panel test from the database
    /// Create it if it doesn't exist
    /// </summary>
    /// <param name="panel">Domain panel</param>
    /// <param name="importedPanelTest">Imported panel test</param>
    /// <returns>Domain panel test</returns>
    private async Task<PanelTest> GetPanelTest(Panel panel, PanelTest importedPanelTest)
    {
        PanelTest panelTest;

        var panelFromDatabase = await dbContext
            .PanelTests
            .Where(pt => pt.Panel.PanelName == panel.PanelName &&
                   pt.MethodTest.Method.MethodName == importedPanelTest.MethodTest.Method.MethodName &&
                   pt.MethodTest.TestName == importedPanelTest.MethodTest.TestName)
            .FirstOrDefaultAsync();
        if (panelFromDatabase == null)
        {
            panelTest = new PanelTest()
            {
                Panel = panel,
            };
            dbContext.PanelTests.Add(panelTest);
        }
        else
        {
            panelTest = panelFromDatabase;
        }

        return panelTest;
    }

    /// <summary>
    /// Update one domain panel test with parameters from an imported panel test
    /// </summary>
    /// <param name="panelTest">Domain panel test</param>
    /// <param name="methodTest">Domain method test</param>
    /// <param name="importedPanelTest">Imported panel test</param>
    /// <param name="sortOrder">Test sort order to use if not supplied by the import</param>
    private static void UpdatePanelTestParameters(PanelTest panelTest, MethodTest methodTest, PanelTest importedPanelTest, int sortOrder)
    {
        UpdatePanelReplicatesParameter(panelTest, methodTest, importedPanelTest);

        if (importedPanelTest.SortOrder.HasValue)
        {
            panelTest.SortOrder = importedPanelTest.SortOrder;
        }
        else
        {
            panelTest.SortOrder = sortOrder;
        }

        panelTest.MethodTest = methodTest;
        panelTest.InstrumentDilutionFactor = panelTest.InstrumentDilutionFactor > 0 ? panelTest.InstrumentDilutionFactor : methodTest.InstrumentDilutionFactor;
    }

    /// <summary>
    /// Update replicates for one domain panel test with data from imported panel test
    /// </summary>
    /// <param name="panelTest">Domain panel test</param>
    /// <param name="methodTest">Domain method test</param>
    /// <param name="importedPanelTest">Imported panel test</param>
    private static void UpdatePanelReplicatesParameter(PanelTest panelTest, MethodTest methodTest, PanelTest importedPanelTest)
    {
        if (panelTest.Replicates > 0)
        {
            return;
        }

        if (importedPanelTest.Replicates == 0)
        {
            panelTest.Replicates = methodTest.Replicates;
        }
        else
        {
            panelTest.Replicates = importedPanelTest.Replicates;
        }
    }
}
