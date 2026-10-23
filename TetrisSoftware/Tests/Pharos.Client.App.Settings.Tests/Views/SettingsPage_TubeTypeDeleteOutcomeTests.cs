using Pharos.Client.App.Settings.Views.Settings;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.Views;

public class SettingsPage_TubeTypeDeleteOutcomeTests
{
    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public void BuildTubeTypeDeleteOutcome_WhenSomeEntriesAreSkipped_SplitsDeletedAndFailedEntries()
    {
        var selected = new List<SettingsPage.TubeTypeDeleteCandidate>
        {
            new("Samples:TubeTypes:TubeTypeList:0", "Tube A"),
            new("Samples:TubeTypes:TubeTypeList:1", "Tube B"),
            new("Samples:TubeTypes:TubeTypeList:2", "Tube C")
        };

        var response = new DeleteResponse
        {
            Deleted = 1,
            Skipped = 2,
            SkippedIds = new List<int> { 12 },
            SkippedPageListEntryKeys =
            [
                "Samples:TubeTypes:TubeTypeList:1",
                "Samples:TubeTypes:TubeTypeList:2"
            ],
            SkippedTubeTypeReasons =
            [
                new DeleteSkippedTubeTypeReason
                {
                    PageListEntryKey = "Samples:TubeTypes:TubeTypeList:1",
                    UsedByRequests = true,
                    UsedByQualityControls = false
                },
                new DeleteSkippedTubeTypeReason
                {
                    PageListEntryKey = "Samples:TubeTypes:TubeTypeList:2",
                    IsDefaultTubeType = true
                }
            ]
        };

        var outcome = SettingsPage.BuildTubeTypeDeleteOutcome(selected, response);

        outcome.SelectedCount.Should().Be(3);
        outcome.DeletedCount.Should().Be(1);
        outcome.SkippedCount.Should().Be(2);
        outcome.DeletedEntries.Should().BeEquivalentTo(["Tube A"]);
        outcome.BlockedEntries.Should().BeEquivalentTo(
        [
            new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("Tube B", "SettingsSamples_UsedByRequests"),
            new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("Tube C", "SettingsSamples_SetAsDefaultTubeType")
        ]);
    }

    [Fact]
    public void BuildTubeTypeDeleteOutcome_WhenNoEntriesAreSkipped_AllSelectedAreReportedAsDeleted()
    {
        var selected = new List<SettingsPage.TubeTypeDeleteCandidate>
        {
            new("Samples:TubeTypes:TubeTypeList:0", "Tube A"),
            new("Samples:TubeTypes:TubeTypeList:1", "Tube B")
        };

        var response = new DeleteResponse
        {
            Deleted = 2,
            Skipped = 0,
            SkippedIds = new List<int>()
        };

        var outcome = SettingsPage.BuildTubeTypeDeleteOutcome(selected, response);

        outcome.DeletedEntries.Should().BeEquivalentTo(["Tube A", "Tube B"]);
        outcome.BlockedEntries.Should().BeEmpty();
        SettingsPage.ShouldShowTubeTypeDeleteOutcome(outcome).Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public void ShouldShowTubeTypeDeleteOutcome_WhenAnyEntryIsBlocked_ReturnsTrue()
    {
        var outcome = new SettingsPage.TubeTypeDeleteOutcome(
            SelectedCount: 2,
            DeletedCount: 1,
            SkippedCount: 1,
            DeletedEntries: ["Tube A"],
            BlockedEntries:
            [
                new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("Tube B", "SettingsSamples_UsedByRequests")
            ]);

        SettingsPage.ShouldShowTubeTypeDeleteOutcome(outcome).Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public void BuildTubeTypeDeleteOutcome_WhenEntriesAreReindexed_UsesSkippedEntryKeys()
    {
        var selected = new List<SettingsPage.TubeTypeDeleteCandidate>
        {
            new("Samples:TubeTypes:TubeTypeList:0", "Tube A"),
            new("Samples:TubeTypes:TubeTypeList:1", "Tube B"),
            new("Samples:TubeTypes:TubeTypeList:2", "Tube C")
        };

        var response = new DeleteResponse
        {
            Deleted = 1,
            Skipped = 2,
            SkippedIds = new List<int>(),
            SkippedPageListEntryKeys =
            [
                "Samples:TubeTypes:TubeTypeList:1",
                "Samples:TubeTypes:TubeTypeList:2"
            ],
            SkippedTubeTypeReasons =
            [
                new DeleteSkippedTubeTypeReason
                {
                    PageListEntryKey = "Samples:TubeTypes:TubeTypeList:1",
                    UsedByRequests = false,
                    UsedByQualityControls = true
                },
                new DeleteSkippedTubeTypeReason
                {
                    PageListEntryKey = "Samples:TubeTypes:TubeTypeList:2",
                    UsedByRequests = true,
                    UsedByQualityControls = true
                }
            ]
        };

        var outcome = SettingsPage.BuildTubeTypeDeleteOutcome(selected, response);

        outcome.DeletedEntries.Should().BeEquivalentTo(["Tube A"]);
        outcome.BlockedEntries.Should().BeEquivalentTo(
        [
            new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("Tube B", "SettingsSamples_UsedByQualityControls"),
            new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("Tube C", "SettingsSamples_UsedByRequestsAndQualityControls")
        ]);
    }

    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public void BuildTubeTypeDeleteOutcome_UsesSkippedReasonEntryKey()
    {
        var selected = new List<SettingsPage.TubeTypeDeleteCandidate>
        {
            new("Samples:TubeTypes:TubeTypeList:0", "Normal"),
            new("Samples:TubeTypes:TubeTypeList:1", "Pediatric"),
            new("Samples:TubeTypes:TubeTypeList:2", "T3")
        };

        var response = new DeleteResponse
        {
            Deleted = 0,
            Skipped = 3,
            SkippedIds = new List<int>(),
            SkippedPageListEntryKeys =
            [
                "Samples:TubeTypes:TubeTypeList:0",
                "Samples:TubeTypes:TubeTypeList:1",
                "Samples:TubeTypes:TubeTypeList:2"
            ],
            SkippedTubeTypeReasons =
            [
                new DeleteSkippedTubeTypeReason
                {
                    PageListEntryKey = "Samples:TubeTypes:TubeTypeList:0",
                    UsedByRequests = true,
                    UsedByQualityControls = true
                },
                new DeleteSkippedTubeTypeReason
                {
                    PageListEntryKey = "Samples:TubeTypes:TubeTypeList:1",
                    UsedByRequests = true,
                    UsedByQualityControls = true
                },
                new DeleteSkippedTubeTypeReason
                {
                    PageListEntryKey = "Samples:TubeTypes:TubeTypeList:2",
                    UsedByRequests = false,
                    UsedByQualityControls = true
                }
            ]
        };

        var outcome = SettingsPage.BuildTubeTypeDeleteOutcome(selected, response);

        outcome.DeletedEntries.Should().BeEmpty();
        outcome.BlockedEntries.Should().BeEquivalentTo(
        [
            new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("Normal", "SettingsSamples_UsedByRequestsAndQualityControls"),
            new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("Pediatric", "SettingsSamples_UsedByRequestsAndQualityControls"),
            new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("T3", "SettingsSamples_UsedByQualityControls")
        ]);
    }

    [Fact]
    public void BuildTubeTypeDeleteOutcome_WhenDefaultReasonIsReported_UsesDefaultReasonEntryKey()
    {
        var selected = new List<SettingsPage.TubeTypeDeleteCandidate>
        {
            new("Samples:TubeTypes:TubeTypeList:0", "T4"),
            new("Samples:TubeTypes:TubeTypeList:1", "T5")
        };

        var response = new DeleteResponse
        {
            Deleted = 1,
            Skipped = 1,
            SkippedPageListEntryKeys =
            [
                "Samples:TubeTypes:TubeTypeList:0"
            ],
            SkippedTubeTypeReasons =
            [
                new DeleteSkippedTubeTypeReason
                {
                    PageListEntryKey = "Samples:TubeTypes:TubeTypeList:0",
                    IsDefaultTubeType = true
                }
            ]
        };

        var outcome = SettingsPage.BuildTubeTypeDeleteOutcome(selected, response);

        outcome.DeletedEntries.Should().BeEquivalentTo(["T5"]);
        outcome.BlockedEntries.Should().BeEquivalentTo(
        [
            new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry("T4", "SettingsSamples_SetAsDefaultTubeType")
        ]);
    }
}
