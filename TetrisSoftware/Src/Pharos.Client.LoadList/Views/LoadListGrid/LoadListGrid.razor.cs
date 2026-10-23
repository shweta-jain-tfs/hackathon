using System.Linq.Expressions;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.LoadList.Views.LoadListGrid;

public partial class LoadListGrid<T> : ComponentBase
{
    [Parameter] public List<IGridColumn<T>> Columns { get; set; } = [];
    [Parameter] public ICollection<T> Rows { get; set; } = default!;
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? DefaultSortColumn { get; set; }
    [Parameter] public SortDirection? DefaultSortByAscending { get; set; }

    private string _sortByColumn = string.Empty;
    private bool _sortByAscending;
    private IQueryable<T> DisplayRows { get; set; } = default!;

    public string GetWidthClass(LoadListGridColumnSizes? colWidthClass)
    {
        if (colWidthClass == null)
        {
            return string.Empty;
        }

        var cssBuilder = new CssBuilder("")
            .AddClass("ph-loadlist-grid__col-w-sm", colWidthClass == LoadListGridColumnSizes.Sm)
            .AddClass("ph-loadlist-grid__col-w-md", colWidthClass == LoadListGridColumnSizes.Md)
            .AddClass("ph-loadlist-grid__col-w-lg", colWidthClass == LoadListGridColumnSizes.Lg)
            .AddClass("ph-loadlist-grid__col-w-xl", colWidthClass == LoadListGridColumnSizes.Xl)
            .AddClass("ph-loadlist-grid__col-w-snug", colWidthClass == LoadListGridColumnSizes.Snug)
            .AddClass("ph-loadlist-grid__col-w-fill", colWidthClass == LoadListGridColumnSizes.Fill);
        return cssBuilder.Build();
    }

    protected override void OnInitialized()
    {
        if (string.IsNullOrEmpty(_sortByColumn))
        {
            if (DefaultSortColumn != null)
            {
                if (DefaultSortByAscending == SortDirection.Ascending)
                {
                    _sortByAscending = true;
                }

                _sortByColumn = DefaultSortColumn;
            }
        }
        else
        {
            DisplayRows = Rows.AsQueryable();
        }

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        var sortedColumn = Columns.FirstOrDefault(c => c.GetPropertyName() == _sortByColumn);

        if (sortedColumn?.SortFunc != null)
        {
            ApplyCustomSort(sortedColumn);
        }
        else
        {
            ApplySort();
        }
    }

    private string AriaSortValue(IGridColumn<T> column)
    {
        if (string.IsNullOrEmpty(_sortByColumn))
        {
            _sortByColumn = column.GetPropertyName();
        }

        return _sortByColumn.Equals(column.GetPropertyName(), StringComparison.Ordinal)
            ? _sortByAscending ? "ascending" : "descending"
            : "none";
    }

    private void SortColumn(IGridColumn<T> column)
    {
        if (_sortByColumn.Equals(column.GetPropertyName(), StringComparison.Ordinal))
        {
            _sortByAscending = !_sortByAscending;
        }
        else
        {
            _sortByColumn = column.GetPropertyName();
            _sortByAscending = true;
        }

        if (column.SortFunc != null)
        {
            ApplyCustomSort(column);
        }
        else
        {
            ApplySort();
        }

        StateHasChanged();
    }

    private void ApplyCustomSort(IGridColumn<T> column)
    {
        DisplayRows = _sortByAscending
            ? Rows.OrderBy(column.SortFunc!).AsQueryable()
            : Rows.OrderByDescending(column.SortFunc!).AsQueryable();
    }

    private void ApplySort()
    {
        var sortDir = _sortByAscending ? SortDirection.Ascending : SortDirection.Descending;
        DisplayRows = Rows.AsQueryable().SortBy(_sortByColumn, sortDir);
    }
}

public interface IGridColumn<T>
{
    string Header { get; }
    object? GetValue(T item);
    string GetPropertyName();
    Func<T, object>? SortFunc { get; }
    bool Sortable { get; set; }
    bool AlignRight { get; set; }
    int ColumnSpan { get; set; }
    bool WrapText { get; set; }
}

public class GridColumn<T, TProp> : IGridColumn<T>
{
    public string Header { get; set; }
    public Expression<Func<T, TProp>> CellSelector { get; set; }
    public Func<T, object>? SortFunc { get; set; }
    public bool Sortable { get; set; } = true;
    public bool AlignRight { get; set; }
    public int ColumnSpan { get; set; } = 1;
    public bool WrapText { get; set; }

    public GridColumn(string header, Expression<Func<T, TProp>> selector)
    {
        Header = header;
        CellSelector = selector;
    }

    private Func<T, TProp>? _compiled;

    public object? GetValue(T row)
    {
        _compiled ??= CellSelector.Compile();
        return _compiled(row);
    }

    public string GetPropertyName()
    {
        var body = CellSelector.Body;

        // Unwrap boxing / nullable conversions
        while (body.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
               && body is UnaryExpression u)
        {
            body = u.Operand;
        }

        if (body is MemberExpression { Expression: ParameterExpression } m)
        {
            return m.Member.Name;
        }

        return string.Empty;
    }
}

public class ColorColumn<T> : GridColumn<T, string?>
{
    public Func<T, ArticleIndicatorShape> ShapeSelector { get; set; }
    public Func<T, ICollection<string?>> Color1Selector { get; set; }

    public ColorColumn(
        string header,
        Expression<Func<T, string?>> selector,
        Func<T, ArticleIndicatorShape> shape,
        Func<T, ICollection<string?>> color1)
        : base(header, selector)
    {
        ShapeSelector = shape;
        Color1Selector = color1;
    }
}

public class MultiColorColumn<T> : GridColumn<T, ICollection<string?>?>
{
    public Func<T, ArticleIndicatorShape> ShapeSelector { get; set; }
    public Func<T, ICollection<string?>> ColorsSelector { get; set; }

    public MultiColorColumn(
        string header,
        Expression<Func<T, ICollection<string?>?>> selector,
        Func<T, ArticleIndicatorShape> shape,
        Func<T, ICollection<string?>> color1)
        : base(header, selector)
    {
        ShapeSelector = shape;
        ColorsSelector = color1;
    }
}

public class StatusColumn<T> : GridColumn<T, int?>
{
    public Func<T, StatusType> StatusSelector { get; set; }
    public Func<T, string> TextSelector { get; set; }

    public StatusColumn(string header, Expression<Func<T, int?>> selector, Func<T, StatusType> statusSelector, Func<T, string> textSelector) : base(header, selector)
    {
        StatusSelector = statusSelector;
        TextSelector = textSelector;
    }
}

public class DateColumn<T> : GridColumn<T, DateTimeOffset?>
{
    public DateColumn(string header, Expression<Func<T, DateTimeOffset?>> selector) : base(header, selector) { }
}