// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pharos.Common.Web.Components.DataGrid.Infrastructure;

// The grid cascades this so that descendant columns can talk back to it. It's an internal type
// so that it doesn't show up by mistake in unrelated components.
internal sealed class InternalGridContext<TGridItem>(PhDataGrid<TGridItem> grid)
{
    public PhDataGrid<TGridItem> Grid { get; } = grid;
}