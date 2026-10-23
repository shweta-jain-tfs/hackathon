using Microsoft.JSInterop;

namespace Pharos.Common.Web.Components.Base;

/// <summary>
/// Base class for Blazor components (including pages) that need to use javascript interop.
/// </summary>
/// <example>
/// Razor:
/// @inherits <see cref="ComponentWithClientScriptBase"/>
///
/// Razor.cs:
/// OnAfterRender(bool firstRender) {
///     if (firstRender) {
///         _module = await <see cref="ImportCommonUtilityJSAsync"/>("ModuleWithTestFunction.js"); // script is located in wwwroot/js/
///         await _module.InvokeVoidAsync("test"); // calls test() in the javascript module.
///     }
/// }
///
/// See dispose example in <see cref="DisposeAsync"/>.
/// </example>
public abstract class ComponentWithClientScriptBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Javascripts are stored in wwwroot/js for each project.
    /// This method generates the path where the scripts are located based on the
    /// assembly name where the component type is defined.
    /// </summary>
    /// <example>
    /// Pharos.Common.Web.Components.PhDatePicker --> ./_content/Pharos.Common.Web/js/
    /// </example>
    /// <param name="componentType">The type of the component</param>
    /// <param name="isRCL">True if the assembly is a razor component library. Set to false if used in the main client application.</param>
    /// <returns></returns>
    protected static string GetClientScriptPath(Type componentType, bool isRCL = true)
    {
        var librarySubfolder = "";
        if (isRCL)
        {
            var assemblyName = componentType.Assembly.GetName().Name;
            librarySubfolder = $"_content/{assemblyName}/";
        }

        return $"./{librarySubfolder}js/";
    }

    /// <summary>
    /// Load javascript module from wwwroot/js in the same project as the blazor component. Use this for scripts that are associated with the blazor component or the Pharos module.
    /// </summary>
    /// <remarks>
    /// Load the script in <see cref="ComponentBase.OnAfterRender(bool)"/>.
    /// Dispose the reference in <see cref="DisposeAsync"/>
    /// </remarks>
    /// <param name="filename">The filename of the script. Leave empty to use [componennt name].js as filename.</param>
    /// <returns>A javascript object reference containing the script module.</returns>
    protected async Task<IJSObjectReference> ImportJSAsync(string filename = "")
    {
        var componentType = GetType();

        if (string.IsNullOrEmpty(filename))
        {
            filename = $"{StripGenericFromTypeName(componentType.Name)}.js";
        }

        var path = GetClientScriptPath(componentType);

        return await ImportModuleAsync($"{path}{filename}");
    }

    private static string StripGenericFromTypeName(string name)
    {
        return name.Split('`').First();
    }

    /// <summary>
    /// Load a utility javascript module from wwwroot/js in the component library. Use this for common scripts that are unrelated to the blazor component.
    /// </summary>
    /// <remarks>
    /// Load the script in <see cref="ComponentBase.OnAfterRender(bool)"/>.
    /// Dispose the reference in <see cref="DisposeAsync"/>
    /// </remarks>
    /// <param name="filename">The filename of the script.</param>
    /// <returns>A javascript object reference containing the script module.</returns>
    protected async Task<IJSObjectReference> ImportCommonUtilityJSAsync(string filename)
    {
        // All utility scripts are located in this assembly
        var path = GetClientScriptPath(typeof(ComponentWithClientScriptBase));

        return await ImportModuleAsync($"{path}{filename}");
    }

    /// <summary>
    /// Load a script module from a specified URL.
    /// </summary>
    /// <remarks>
    /// Blazor caches the module, so it's fetched once even if multiple components on the same page use the script.
    /// </remarks>
    /// <param name="scriptUrl">URL to the script module</param>
    /// <returns>Javascript module reference.</returns>
    /// <exception cref="ArgumentException">If the filename is invalid</exception>
    private async Task<IJSObjectReference> ImportModuleAsync(string scriptUrl)
    {
        if (!scriptUrl.EndsWith(".js"))
        {
            throw new ArgumentException("Script name must end with .js");
        }

        var module = await JS.InvokeAsync<IJSObjectReference>(
            "import", scriptUrl);

        return module;
    }

    /// <summary>
    /// Dispose javascript modules in the overriden version of this method.
    /// </summary>
    /// <example>
    /// await _module.SafeDisposeAsync();
    /// GC.SuppressFinalize(this);
    /// </example>
    public abstract ValueTask DisposeAsync();
}
