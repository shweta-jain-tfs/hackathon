// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pharos.Common.Web.Models.DataGrid;

/// <summary>
/// Describes the direction in which a column is sorted.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Automatic sort order. Used internally by the data grid.
    /// </summary>
    Auto,

    /// <summary>
    /// Ascending order.
    /// </summary>
    Ascending,

    /// <summary>
    /// Descending order.
    /// </summary>
    Descending,
}