using Pharos.Common.Web.Components.TrailingEdge;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Layout
{
    public partial class PhPage
    {
        /// <summary>
        /// Page content. Use a component with internal scrolling and adaptive height to avoid scroll in the page content.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// (Optional) Use for breadcrumbs and buttons aligned with the breadscrumbs. Doesn't scroll.
        /// </summary>
        [Parameter]
        public RenderFragment? PageHeader { get; set; }

        /// <summary>
        /// (Optional) Use for subnavigation controls. Doesn't scroll.
        /// </summary>
        [Parameter]
        public RenderFragment? Sidebar { get; set; }

        [Parameter]
        public bool WideSidebar { get; set; }

        /// <summary>
        /// Adds trailing-edge gradients to the page content.
        /// </summary>
        /// <remarks>
        /// Use this when the page content is intended to scroll.
        /// Do not use it when a child component, such as a data grid, provides its own scrolling.
        /// </remarks>
        [Parameter]
        public TrailingEdgeSize? TrailingEdge { get; set; }

        private string? _classNames;
        private bool HasPageHeader => PageHeader != null;
        private bool HasSidebar => Sidebar != null;

        protected override Task OnParametersSetAsync()
        {
            _classNames = new CssBuilder("ph-page")
                .AddClass("ph-page--with-header", HasPageHeader)
                .AddClass("ph-page--with-sidebar", HasSidebar)
                .AddClass("ph-page--sidebar-wide", WideSidebar)
                .Build();
            return base.OnParametersSetAsync();
        }
    }
}