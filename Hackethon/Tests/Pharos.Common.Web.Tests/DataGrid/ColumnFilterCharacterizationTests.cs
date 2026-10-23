using System.Text.Json;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Tests.DataGrid;

/// <summary>
/// Characterization tests for DataGrid: Filter Handling.
/// <para>
/// These tests intentionally describe the filter behavior <b>as it is today</b> rather than how it
/// ideally should be. They act as a safety net: the planned internal simplification of the filter
/// models must not change the <see cref="PropertyFilterExpression"/> stream sent to the
/// <c>ItemsProvider</c>, nor the save/apply state round-trip used for persisted grid state.
/// </para>
/// </summary>
public class ColumnFilterCharacterizationTests
{
    public enum Status
    {
        Draft = 0,
        Active = 1,
        Archived = 2
    }

    // ---------------------------------------------------------------------
    // StringColumnFilter
    // ---------------------------------------------------------------------

    [Fact]
    public void StringFilter_ProducesNoExpressions_WhenValueIsEmpty()
    {
        var filter = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"));

        // An empty value must be treated as "no filter" so the grid does not send a
        // Contains("") expression that would be a no-op on the server.
        filter.FilterExpressions.Should().BeEmpty();
    }

    [Fact]
    public void StringFilter_UsesContains_ByDefault()
    {
        var filter = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"))
        {
            Value = "an"
        };

        filter.FilterExpressions.Should().ContainSingle()
            .Which.Should().Be(new PropertyFilterExpression<string>("FirstName", PropertyFilterOperator.Contains, "an"));
    }

    [Fact]
    public void StringFilter_UsesEqualsTo_WhenExactMatchRequested()
    {
        // isExactMatch is a constructor flag, so the operator choice is fixed for the lifetime
        // of the filter and never depends on the value the user typed.
        var filter = new StringColumnFilter<Person, string>(
            new TestColumn<Person, string>("FirstName"),
            isExactMatch: true)
        {
            Value = "Ann"
        };

        filter.FilterExpressions.Should().ContainSingle()
            .Which.Operator.Should().Be(PropertyFilterOperator.EqualsTo);
    }

    [Fact]
    public void StringFilter_AppliesQuickFilterDefault_OnlyWhenColumnIsQuickFilter()
    {
        var quickColumn = new TestColumn<Person, string>("FirstName", quickFilter: true, quickFilterDefault: "Ann");
        var regularColumn = new TestColumn<Person, string>("FirstName", quickFilter: false, quickFilterDefault: "Ann");

        // Defaults are deliberately ignored for non-quick-filter columns, because such columns
        // have no visible clear/reset affordance for the user.
        new StringColumnFilter<Person, string>(quickColumn).Value.Should().Be("Ann");
        new StringColumnFilter<Person, string>(regularColumn).Value.Should().BeNull();
    }

    [Fact]
    public void StringFilter_RoundTripsState()
    {
        var filter = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"))
        {
            Value = "Ann"
        };

        var restored = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"));
        restored.ApplyState(filter.SaveState());

        restored.Value.Should().Be("Ann");
        restored.Value.Should().Be("Ann");
        restored.FilterExpressions.Should().BeEquivalentTo(filter.FilterExpressions);
    }

    [Fact]
    public void StringFilter_Clear_ResetsToEmptyString()
    {
        var filter = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"))
        {
            Value = "Ann"
        };

        filter.Clear();

        // Note the asymmetry with the initial state (null): Clear yields string.Empty.
        filter.Value.Should().BeEmpty();
        filter.FilterExpressions.Should().BeEmpty();
    }

    // ---------------------------------------------------------------------
    // Generic quick filters
    // ---------------------------------------------------------------------

    [Fact]
    public void SingleValueQuickFilter_ProducesNoExpressions_WhenValueIsNull()
    {
        var filter = new SingleValueQuickFilter<int>("Id", "Id");

        filter.FilterExpressions.Should().BeEmpty();
        filter.ActiveFilterCount.Should().Be(0);
        filter.HasValue.Should().BeFalse();
    }

    [Fact]
    public void SingleValueQuickFilter_EmitsTypedExpressions_ForZeroFalseAndFirstEnumValue()
    {
        var numericFilter = new SingleValueQuickFilter<int>("Id", "Id")
        {
            HasValue = true,
            Value = 0
        };

        numericFilter.FilterExpressions.Should().ContainSingle()
            .Which.Should().Be(new PropertyFilterExpression<int>("Id", PropertyFilterOperator.EqualsTo, 0));

        var boolFilter = new SingleValueQuickFilter<bool>("IsActive", "Is active")
        {
            HasValue = true,
            Value = false
        };

        boolFilter.FilterExpressions.Should().ContainSingle()
            .Which.Should().Be(new PropertyFilterExpression<bool>("IsActive", PropertyFilterOperator.EqualsTo, false));

        var enumFilter = new SingleValueQuickFilter<Status>("Status", "Status")
        {
            HasValue = true,
            Value = Status.Draft
        };

        enumFilter.FilterExpressions.Should().ContainSingle()
            .Which.Should().Be(new PropertyFilterExpression<Status>("Status", PropertyFilterOperator.EqualsTo, Status.Draft));
    }

    [Fact]
    public void SingleValueQuickFilter_ClearAndResetToDefault_WorkAsExpected()
    {
        var filter = new SingleValueQuickFilter<int>("Id", "Id", defaultValue: 7, hasDefaultValue: true)
        {
            HasValue = true,
            Value = 42
        };
        var revisionBeforeClear = filter.ResetRevision;

        filter.Clear();

        filter.HasValue.Should().BeFalse();
        filter.ResetRevision.Should().Be(revisionBeforeClear + 1);

        filter.ResetToDefault();

        filter.HasValue.Should().BeTrue();
        filter.Value.Should().Be(7);
        filter.ResetRevision.Should().Be(revisionBeforeClear + 2);
    }

    [Fact]
    public void MultiSelectQuickFilter_UsesOrOperator_AndEmitsSelectedValues_InSourceOrder()
    {
        var filter = new MultiSelectQuickFilter<string>(
            "City",
            "City",
            new[] { "oslo", "bergen", "tromso" });

        filter.LogicalOperator.Should().Be(LogicalFilterOperator.Or);

        filter.SelectableValues.Single(x => x.Value == "tromso").IsSelected = true;
        filter.SelectableValues.Single(x => x.Value == "oslo").IsSelected = true;

        filter.ActiveFilterCount.Should().Be(2);
        filter.FilterExpressions.Should().Equal(
            new PropertyFilterExpression<string>("City", PropertyFilterOperator.EqualsTo, "oslo"),
            new PropertyFilterExpression<string>("City", PropertyFilterOperator.EqualsTo, "tromso"));
    }

    [Fact]
    public void NumericFilter_ProducesNoExpressions_WhenNothingIsSelected()
    {
        var filter = new NumericColumnFilter<Person, int>(new TestColumn<Person, int>("Id"));

        filter.FilterExpressions.Should().BeEmpty();
        filter.ActiveFilterCount.Should().Be(0);
    }

    [Fact]
    public void NumericFilter_EmitsExpressionsInEqualGreaterLessOrder()
    {
        var filter = new NumericColumnFilter<Person, int>(new TestColumn<Person, int>("Id"))
        {
            EqualToValue = new NumericFilterValues { IsSelected = true, Value = 5 },
            GreaterThanValue = new NumericFilterValues { IsSelected = true, Value = 1 },
            LessThanValue = new NumericFilterValues { IsSelected = true, Value = 9 }
        };

        // The emission order is part of the observable contract: consumers combine these with
        // LogicalOperator.And, and some server implementations are order sensitive when logging.
        filter.FilterExpressions.Should().Equal(
            new PropertyFilterExpression<double>("Id", PropertyFilterOperator.EqualsTo, 5d),
            new PropertyFilterExpression<double>("Id", PropertyFilterOperator.GreaterThan, 1d),
            new PropertyFilterExpression<double>("Id", PropertyFilterOperator.LessThan, 9d));
        filter.ActiveFilterCount.Should().Be(3);
    }

    [Fact]
    public void NumericFilter_IgnoresValues_ThatAreNotSelected()
    {
        var filter = new NumericColumnFilter<Person, int>(new TestColumn<Person, int>("Id"))
        {
            // A value is present but the user did not tick the checkbox, so it must not filter.
            GreaterThanValue = new NumericFilterValues { IsSelected = false, Value = 42 }
        };

        filter.FilterExpressions.Should().BeEmpty();
    }

    [Fact]
    public void NumericFilter_RoundTripsState_IncludingUnselectedValues()
    {
        var filter = new NumericColumnFilter<Person, int>(new TestColumn<Person, int>("Id"))
        {
            GreaterThanValue = new NumericFilterValues { IsSelected = true, Value = 1 },
            LessThanValue = new NumericFilterValues { IsSelected = false, Value = 9 }
        };

        var restored = new NumericColumnFilter<Person, int>(new TestColumn<Person, int>("Id"));
        restored.ApplyState(filter.SaveState());

        // SaveState persists the value even when unselected, so the user sees their previous
        // input again when re-opening the filter.
        restored.GreaterThanValue.IsSelected.Should().BeTrue();
        restored.GreaterThanValue.Value.Should().Be(1);
        restored.LessThanValue.IsSelected.Should().BeFalse();
        restored.LessThanValue.Value.Should().Be(9);
    }

    [Fact]
    public void NumericFilter_ApplyDefault_ClearsInsteadOfApplyingAValue()
    {
        var column = new TestColumn<Person, int>("Id", quickFilter: true, quickFilterDefault: 7);
        var filter = new NumericColumnFilter<Person, int>(column)
        {
            EqualToValue = new NumericFilterValues { IsSelected = true, Value = 3 }
        };

        filter.ApplyDefault();

        // Numeric filters deliberately have no default: ApplyDefault degenerates to Clear.
        filter.FilterExpressions.Should().BeEmpty();
    }

    // ---------------------------------------------------------------------
    // BoolColumnFilter
    // ---------------------------------------------------------------------

    [Fact]
    public void BoolFilter_AlwaysEmitsOneExpression_EvenWhenFalse()
    {
        var filter = new BoolColumnFilter<Person, bool>(new TestColumn<Person, bool>("IsActive"));

        // Unlike every other filter type, the bool filter has no "unset" state: false is a
        // meaningful filter value, so an expression is emitted unconditionally.
        filter.FilterExpressions.Should().ContainSingle()
            .Which.Should().Be(new PropertyFilterExpression<bool>("IsActive", PropertyFilterOperator.Contains, false));
    }

    [Fact]
    public void BoolFilter_RoundTripsState()
    {
        var filter = new BoolColumnFilter<Person, bool>(new TestColumn<Person, bool>("IsActive"))
        {
            Value = true
        };

        var restored = new BoolColumnFilter<Person, bool>(new TestColumn<Person, bool>("IsActive"));
        restored.ApplyState(filter.SaveState());

        restored.Value.Should().BeTrue();
    }

    // ---------------------------------------------------------------------
    // EnumColumnFilter
    // ---------------------------------------------------------------------

    [Fact]
    public void EnumFilter_UsesOrOperator_AndOneExpressionPerSelectedValue()
    {
        var filter = new EnumColumnFilter<Person, Status>(new TestColumn<Person, Status>("Status"));
        filter.SelectableValues.Single(x => x.Value == (int)Status.Draft).IsSelected = true;
        filter.SelectableValues.Single(x => x.Value == (int)Status.Archived).IsSelected = true;

        // Multi-select filters must be OR-combined, otherwise selecting two values would
        // always return an empty result set.
        filter.LogicalOperator.Should().Be(LogicalFilterOperator.Or);
        filter.FilterExpressions.Should().Equal(
            new PropertyFilterExpression<int>("Status", PropertyFilterOperator.EqualsTo, (int)Status.Draft),
            new PropertyFilterExpression<int>("Status", PropertyFilterOperator.EqualsTo, (int)Status.Archived));
        filter.ActiveFilterCount.Should().Be(2);
    }

    [Fact]
    public void EnumFilter_PopulatesSelectableValuesFromEnum()
    {
        var filter = new EnumColumnFilter<Person, Status>(new TestColumn<Person, Status>("Status"));

        filter.SelectableValues.Select(x => x.DisplayName)
            .Should().Equal(nameof(Status.Draft), nameof(Status.Active), nameof(Status.Archived));
        filter.FilterExpressions.Should().BeEmpty();
    }

    [Fact]
    public void EnumFilter_RoundTripsState()
    {
        var filter = new EnumColumnFilter<Person, Status>(new TestColumn<Person, Status>("Status"));
        filter.SelectableValues.Single(x => x.Value == (int)Status.Active).IsSelected = true;

        var restored = new EnumColumnFilter<Person, Status>(new TestColumn<Person, Status>("Status"));
        restored.ApplyState(filter.SaveState());

        restored.SelectableValues.Where(x => x.IsSelected).Select(x => x.Value)
            .Should().Equal((int)Status.Active);
    }

    // ---------------------------------------------------------------------
    // DateTimeColumnFilter
    // ---------------------------------------------------------------------

    [Fact]
    public void DateTimeFilter_Between_EmitsInclusiveRange()
    {
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        filter.SelectBetween(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        filter.FilterExpressions.Should().Equal(
            new PropertyFilterExpression<DateTimeOffset>("Created", PropertyFilterOperator.GreaterThanOrEqualTo, new DateTimeOffset(new DateTime(2026, 1, 1))),
            new PropertyFilterExpression<DateTimeOffset>("Created", PropertyFilterOperator.LessThanOrEqualTo, new DateTimeOffset(new DateTime(2026, 1, 31))));
    }

    [Fact]
    public void DateTimeFilter_Today_EmitsSingleDayRange()
    {
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        filter.SelectToday();

        var expressions = filter.FilterExpressions.ToList();

        expressions.Should().HaveCount(2);

        expressions[0].Should().BeOfType<PropertyFilterExpression<DateTimeOffset>>()
                    .Which.Should().Be(new PropertyFilterExpression<DateTimeOffset>("Created", PropertyFilterOperator.GreaterThanOrEqualTo, new DateTimeOffset(DateTime.Today)));

        expressions[1].Should().BeOfType<PropertyFilterExpression<DateTimeOffset>>()
                    .Which.Should().Be(new PropertyFilterExpression<DateTimeOffset>("Created", PropertyFilterOperator.LessThanOrEqualTo, new DateTimeOffset(DateTime.Today)));
    }

    [Fact]
    public void DateTimeFilter_LastDays_EmitsRangeStartingNDaysAgo()
    {
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        filter.SelectLastDays(7);

        var expressions = filter.FilterExpressions.ToList();

        expressions[0].Operator.Should().Be(PropertyFilterOperator.GreaterThanOrEqualTo);
        expressions[0].UntypedValue.Should().Be((DateTimeOffset)DateTimeOffset.Now.AddDays(-7).Date);
        expressions[1].Operator.Should().Be(PropertyFilterOperator.LessThanOrEqualTo);
    }

    [Fact]
    public void DateTimeFilter_LastDays_FallsBackToNearMinValue_WhenCountOverflows()
    {
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        filter.SelectLastDays(double.MaxValue);

        // MinValue itself is avoided because nswag cannot parse it, so MinValue + 1 day is used.
        filter.FilterExpressions.First().UntypedValue.Should().Be(DateTimeOffset.MinValue.AddDays(1));
    }

    [Fact]
    public void DateTimeFilter_ModesAreMutuallyExclusive()
    {
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));

        filter.SelectToday();
        filter.SelectBetween(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        // Each Select* call clears the other modes, so only one mode can ever be active.
        filter.ActiveFilterCount.Should().Be(1);
        filter.Today.IsSelected.Should().BeFalse();
        filter.Between.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void DateTimeFilter_ModesCanBeCombined_WhenSetDirectlyAsTheFilterModalDoes()
    {
        // PhDateTimeColumnFilter.razor binds a separate checkbox to each mode's IsSelected, so the
        // modal can activate several modes at once. Only the Select* helpers (used by the quick
        // filter) enforce a single mode. This is why the modes cannot be collapsed into one enum.
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        filter.Today.IsSelected = true;
        filter.LastDays.IsSelected = true;

        filter.ActiveFilterCount.Should().Be(2);
        filter.FilterExpressions.Should().HaveCount(4);
        filter.LogicalOperator.Should().Be(LogicalFilterOperator.And);
    }

    [Fact]
    public void DateTimeFilter_ActiveFilterCount_CountsModesNotExpressions()
    {
        // Each mode yields a >= / <= pair, so counting expressions would double the number the
        // user sees on the filter badge.
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        filter.SelectToday();

        filter.ActiveFilterCount.Should().Be(1);
        filter.FilterExpressions.Should().HaveCount(2);
    }

    [Fact]
    public void DateTimeFilter_SelectMonthRange_FlagsIsMonthRange()
    {
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        filter.SelectMonthRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        // IsMonthRange is purely a UI presentation hint; it produces no extra expression.
        filter.IsMonthRange.Should().BeTrue();
        filter.FilterExpressions.Should().HaveCount(2);
    }

    [Fact]
    public void DateTimeFilter_ApplyDefault_SelectsLastDays_WhenDefaultIsInt()
    {
        var column = new TestColumn<Person, DateTimeOffset>("Created", quickFilter: true, quickFilterDefault: 30);

        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(column);

        filter.LastDays.IsSelected.Should().BeTrue();
        filter.LastDays.LastDays.Should().Be(30);
    }

    [Theory]
    [InlineData("today")]
    [InlineData("lastdays")]
    [InlineData("between")]
    public void DateTimeFilter_RoundTripsState(string mode)
    {
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));

        switch (mode)
        {
            case "today":
                filter.SelectToday();
                break;
            case "lastdays":
                filter.SelectLastDays(7);
                break;
            default:
                filter.SelectBetween(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
                break;
        }

        var restored = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        restored.ApplyState(filter.SaveState());

        restored.FilterExpressions.Should().BeEquivalentTo(filter.FilterExpressions);
    }

    [Fact]
    public void DateTimeFilter_Clear_RemovesAllExpressions()
    {
        var filter = new DateTimeColumnFilter<Person, DateTimeOffset>(
            new TestColumn<Person, DateTimeOffset>("Created"));
        filter.SelectLastDays(7);

        filter.Clear();

        filter.FilterExpressions.Should().BeEmpty();
        filter.ActiveFilterCount.Should().Be(0);
    }

    // ---------------------------------------------------------------------
    // Shared base behavior
    // ---------------------------------------------------------------------

    [Fact]
    public void Clear_IncrementsResetRevision_SoQuickFiltersCanResyncTheirInputs()
    {
        var filter = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"));
        var before = filter.ResetRevision;

        filter.Clear();

        filter.ResetRevision.Should().Be(before + 1);
    }

    [Fact]
    public void ResetToDefault_ClearsWhenNoQuickFilterDefaultExists()
    {
        var filter = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"))
        {
            Value = "Ann"
        };

        filter.ResetToDefault();

        filter.Value.Should().BeEmpty();
    }

    [Fact]
    public void ResetToDefault_RestoresQuickFilterDefault()
    {
        var column = new TestColumn<Person, string>("FirstName", quickFilter: true, quickFilterDefault: "Ann");
        var filter = new StringColumnFilter<Person, string>(column) { Value = "Bob" };

        filter.ResetToDefault();

        filter.Value.Should().Be("Ann");
    }

    // ---------------------------------------------------------------------
    // Polymorphic state serialization
    // ---------------------------------------------------------------------

    [Fact]
    public void FilterState_SurvivesPolymorphicJsonRoundTrip()
    {
        var stringFilter = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"))
        {
            Value = "Ann"
        };
        var numericFilter = new NumericColumnFilter<Person, int>(new TestColumn<Person, int>("Id"))
        {
            GreaterThanValue = new NumericFilterValues { IsSelected = true, Value = 1 }
        };

        // The grid persists a List<ColumnFilterState>, so the concrete type must be recoverable
        // from the serialized payload alone.
        List<ColumnFilterState> states = [stringFilter.SaveState(), numericFilter.SaveState()];

        var json = JsonSerializer.Serialize(states);
        var restoredStates = JsonSerializer.Deserialize<List<ColumnFilterState>>(json)!;

        restoredStates[0].Should().BeOfType<StringFilterState>();
        restoredStates[1].Should().BeOfType<NumericFilterState>();

        var restoredString = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"));
        restoredString.ApplyState(restoredStates[0]);
        restoredString.Value.Should().Be("Ann");

        var restoredNumeric = new NumericColumnFilter<Person, int>(new TestColumn<Person, int>("Id"));
        restoredNumeric.ApplyState(restoredStates[1]);
        restoredNumeric.FilterExpressions.Should().BeEquivalentTo(numericFilter.FilterExpressions);
    }

    [Fact]
    public void GenericQuickFilterState_SurvivesPolymorphicJsonRoundTrip()
    {
        var singleFilter = new SingleValueQuickFilter<int>("Id", "Id")
        {
            HasValue = true,
            Value = 0
        };
        var multiFilter = new MultiSelectQuickFilter<string>(
            "City",
            "City",
            new[] { "oslo", "bergen" });
        multiFilter.SelectableValues.Single(x => x.Value == "bergen").IsSelected = true;

        List<ColumnFilterState> states = [singleFilter.SaveState(), multiFilter.SaveState()];

        var json = JsonSerializer.Serialize(states);
        var restoredStates = JsonSerializer.Deserialize<List<ColumnFilterState>>(json)!;

        restoredStates[0].Should().BeOfType<SingleValueQuickFilterState>();
        restoredStates[1].Should().BeOfType<MultiSelectQuickFilterState>();

        var restoredSingle = new SingleValueQuickFilter<int>("Id", "Id");
        restoredSingle.ApplyState(restoredStates[0]);
        restoredSingle.HasValue.Should().BeTrue();
        restoredSingle.Value.Should().Be(0);

        var restoredMulti = new MultiSelectQuickFilter<string>("City", "City", new[] { "oslo", "bergen" });
        restoredMulti.ApplyState(restoredStates[1]);
        restoredMulti.FilterExpressions.Should().BeEquivalentTo(multiFilter.FilterExpressions);
    }

    [Fact]
    public void ApplyState_IgnoresStateBelongingToAnotherFilterType()
    {
        var filter = new StringColumnFilter<Person, string>(new TestColumn<Person, string>("FirstName"))
        {
            Value = "Ann"
        };

        filter.ApplyState(new BoolFilterState { PropertyName = "FirstName", Value = true });

        filter.Value.Should().Be("Ann");
    }

    // ---------------------------------------------------------------------
    // KeyValueListColumnFilter expressions and state
    // ---------------------------------------------------------------------

    [Fact]
    public void KeyValueListFilter_UsesOrOperator_BecauseSelectionsAreAlternatives()
    {
        // Selecting "Oslo" and "Bergen" must mean "either", not "both", which is impossible for a
        // single column value.
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            new Dictionary<string, string> { ["oslo"] = "Oslo" });

        filter.LogicalOperator.Should().Be(LogicalFilterOperator.Or);
    }

    [Fact]
    public void KeyValueListFilter_EmitsOneEqualsExpressionPerSelectedKey_InSourceOrder()
    {
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen", ["tromso"] = "Tromso" });

        filter.SelectableStringValues.Single(x => x.Key == "tromso").IsSelected = true;
        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected = true;

        // Order follows the data source, not the order the user clicked.
        filter.FilterExpressions.Should().Equal(
            new PropertyFilterExpression<string>("City", PropertyFilterOperator.EqualsTo, "oslo"),
            new PropertyFilterExpression<string>("City", PropertyFilterOperator.EqualsTo, "tromso"));
    }

    [Fact]
    public void KeyValueListFilter_ProducesNoExpressions_WhenNothingIsSelected()
    {
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" });

        filter.FilterExpressions.Should().BeEmpty();
    }

    [Fact]
    public void KeyValueListFilter_RoundTripsState()
    {
        var dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" };
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            dataSource);

        filter.SelectableStringValues.Single(x => x.Key == "bergen").IsSelected = true;

        var state = filter.SaveState();
        state.PropertyName.Should().Be("City");

        var restored = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            dataSource);
        restored.ApplyState(state);

        restored.FilterExpressions.Should().BeEquivalentTo(filter.FilterExpressions);
    }

    [Fact]
    public void KeyValueListFilter_AppliesState_BeforeDataSourceHasLoaded()
    {
        // Restored session state arrives before the async data source. The keys are held as
        // placeholders so the grid still filters on the first data load.
        Dictionary<string, string>? dataSource = [];
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            () => dataSource);

        filter.ApplyState(new KeyValueListFilterState
        {
            PropertyName = "City",
            SelectedKeys = ["oslo"]
        });

        filter.FilterExpressions.Should().ContainSingle()
            .Which.UntypedValue.Should().Be("oslo");

        // Once the real values arrive the selection is carried over onto them.
        dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" };

        filter.SelectableStringValues.Should().HaveCount(2);
        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected.Should().BeTrue();
    }

    [Fact]
    public void KeyValueListFilter_Clear_DeselectsEverythingAndIncrementsResetRevision()
    {
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" });

        filter.SelectableStringValues.ForEach(x => x.IsSelected = true);
        var revisionBeforeClear = filter.ResetRevision;

        filter.Clear();

        filter.ActiveFilterCount.Should().Be(0);
        filter.ResetRevision.Should().Be(revisionBeforeClear + 1);
    }

    [Fact]
    public void KeyValueListFilter_DoesNotRebuildValues_WhenDataSourceIsUnchanged()
    {
        // The pull-on-read refresh must be idempotent: repeated reads during a render pass cannot
        // discard the SelectableKeyValue instances the checkboxes are bound to.
        var dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" };
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            () => dataSource);

        var firstRead = filter.SelectableStringValues;
        firstRead.Single(x => x.Key == "oslo").IsSelected = true;

        filter.SelectableStringValues.Should().BeSameAs(firstRead);
        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected.Should().BeTrue();
    }

    [Fact]
    public void KeyValueListFilter_DoesNotRebuildValues_WhenProviderReturnsAnEqualButNewDictionary()
    {
        // Several consumers (e.g. CartridgesListView.StatusNameDict()) build a brand new dictionary
        // on every render. The refresh guard compares content, not instances, so the user's
        // selection must survive re-renders.
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            () => new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" });

        var firstRead = filter.SelectableStringValues;
        firstRead.Single(x => x.Key == "oslo").IsSelected = true;

        var invalidated = false;
        filter.SelectionInvalidated += () => invalidated = true;

        filter.SelectableStringValues.Should().BeSameAs(firstRead);
        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected.Should().BeTrue();
        invalidated.Should().BeFalse();
    }

    [Fact]
    public void KeyValueListFilter_DoesNotRaiseSelectionInvalidated_WhenRefreshOnlyAddsValues()
    {
        // Adding options cannot change what the user already filters on, so the grid must not be
        // asked to re-query.
        var dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo" };
        var filter = new KeyValueListColumnFilter<Person, string>(
            new TestColumn<Person, string>("City"),
            () => dataSource);

        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected = true;

        var invalidated = false;
        filter.SelectionInvalidated += () => invalidated = true;

        dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" };
        _ = filter.SelectableStringValues;

        invalidated.Should().BeFalse();
    }

    // ---------------------------------------------------------------------
    // KeyValueListColumnFilter default handling
    // ---------------------------------------------------------------------

    [Fact]
    public void KeyValueListFilter_AppliesSingleStringDefault()
    {
        var column = new TestColumn<Person, string>("City", quickFilter: true, quickFilterDefault: "oslo");

        var filter = new KeyValueListColumnFilter<Person, string>(
            column,
            new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" });

        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected.Should().BeTrue();
        filter.ActiveFilterCount.Should().Be(1);
    }

    [Fact]
    public void KeyValueListFilter_AppliesMultiSelectDefault()
    {
        var column = new TestColumn<Person, string>("City", quickFilter: true, quickFilterDefault: new[] { "oslo", "bergen" });

        var filter = new KeyValueListColumnFilter<Person, string>(
            column,
            new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen", ["tromso"] = "Tromso" });

        filter.ActiveFilterCount.Should().Be(2);
    }

    [Fact]
    public void KeyValueListFilter_DoesNotReapplyUnchangedDefault_WhenDataSourceRefreshes()
    {
        var column = new TestColumn<Person, string>("City", quickFilter: true, quickFilterDefault: "oslo");
        var dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" };
        var filter = new KeyValueListColumnFilter<Person, string>(column, () => dataSource);

        // The user deselects the default, then the async data source refreshes with the same
        // default still configured. The default must not fight the user's choice.
        filter.Clear();
        dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen", ["tromso"] = "Tromso" };

        filter.ActiveFilterCount.Should().Be(0);
    }

    [Fact]
    public void KeyValueListFilter_AppliesDefault_WhenDataSourceArrivesLater()
    {
        var column = new TestColumn<Person, string>("City", quickFilter: true, quickFilterDefault: "oslo");

        // Constructed before the data source is available: the key is held as a placeholder so
        // filtering still works.
        Dictionary<string, string>? dataSource = [];
        var filter = new KeyValueListColumnFilter<Person, string>(column, () => dataSource);
        filter.ActiveFilterCount.Should().Be(1);

        // The values arrive after the first render. Nothing pushes them into the filter; it picks
        // them up the next time it is read.
        dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" };

        filter.SelectableStringValues.Should().HaveCount(2);
        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected.Should().BeTrue();
    }

    [Fact]
    public void KeyValueListFilter_ApplyDefault_ReappliesEvenWhenDefaultIsUnchanged()
    {
        var column = new TestColumn<Person, string>("City", quickFilter: true, quickFilterDefault: "oslo");
        var filter = new KeyValueListColumnFilter<Person, string>(
            column,
            new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" });

        filter.Clear();
        filter.ResetToDefault();

        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected.Should().BeTrue();
    }

    [Fact]
    public void KeyValueListFilter_IgnoresDefault_WhenColumnIsNotAQuickFilter()
    {
        var column = new TestColumn<Person, string>("City", quickFilter: false, quickFilterDefault: "oslo");

        var filter = new KeyValueListColumnFilter<Person, string>(
            column,
            new Dictionary<string, string> { ["oslo"] = "Oslo" });

        filter.ActiveFilterCount.Should().Be(0);
    }

    [Fact]
    public void KeyValueListFilter_RaisesSelectionInvalidated_WhenRefreshDropsSelectedKey()
    {
        var column = new TestColumn<Person, string>("City");
        var dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" };
        var filter = new KeyValueListColumnFilter<Person, string>(column, () => dataSource);

        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected = true;

        var invalidated = false;
        filter.SelectionInvalidated += () => invalidated = true;

        // "oslo" disappears from the source, so the user's selection can no longer be honored.
        dataSource = new Dictionary<string, string> { ["bergen"] = "Bergen" };
        _ = filter.SelectableStringValues;

        invalidated.Should().BeTrue();
        filter.ActiveFilterCount.Should().Be(0);
    }

    [Fact]
    public void KeyValueListFilter_KeepsSelection_WhenRefreshStillContainsSelectedKey()
    {
        var column = new TestColumn<Person, string>("City");
        var dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo" };
        var filter = new KeyValueListColumnFilter<Person, string>(column, () => dataSource);

        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected = true;

        dataSource = new Dictionary<string, string> { ["oslo"] = "Oslo", ["bergen"] = "Bergen" };

        filter.SelectableStringValues.Should().HaveCount(2);
        filter.SelectableStringValues.Single(x => x.Key == "oslo").IsSelected.Should().BeTrue();
    }
}
