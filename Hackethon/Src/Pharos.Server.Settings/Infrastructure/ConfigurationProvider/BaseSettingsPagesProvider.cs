using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Localization;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Domain.Common;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Localization;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

/// <summary>
/// Base class for settings pages providers. Provides functionality to visit configuration assemblies
/// and generate a hierarchical page structure from configuration sections.
/// </summary>
/// <remarks>
/// This class extends <see cref="SettingsVisitor"/> and provides specific logic for creating
/// settings pages, groups, options, and page lists from configuration classes decorated with
/// appropriate attributes (e.g., <see cref="RootConfigSectionAttribute"/>, <see cref="DisplayNameLocalAttribute"/>).
/// </remarks>
internal abstract class BaseSettingsPagesProvider : SettingsVisitor
{
    protected readonly IValueConverter _valueConverter;
    protected readonly IStringLocalizer? _localizer;
    protected readonly Dictionary<string, Page> _pages = [];
    protected ISettingsParentItem? _currentParent;
    protected readonly Stack<ISettingsParentItem> _parents = new();
    protected static IEnumerable<Assembly> _assemblies = [];
    protected static Func<Assembly, IEnumerable<Type>>? _getConfigSections;
    protected List<string> _currentKeys = [];
    protected readonly Stack<List<string>> _keys = new();
    private readonly HashSet<string> _forcedHiddenOptionKeys = new(StringComparer.Ordinal);
    protected int _pageStructureDepth;
    protected bool IsPageStructure => _pageStructureDepth > 0;


    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSettingsPagesProvider"/> class.
    /// </summary>
    /// <param name="valueConverter">The value converter for transforming property values to options.</param>
    /// <param name="localizer">The string localizer for translating resource keys.</param>
    protected BaseSettingsPagesProvider(IValueConverter valueConverter, IStringLocalizer? localizer = null)
    {
        _valueConverter = valueConverter;
        _localizer = localizer;
    }

    /// <summary>
    /// Initializes the static assemblies collection used for discovering configuration sections.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for configuration sections.</param>
    /// <param name="getConfigSections">Optional function to filter configuration section types from assemblies.</param>
    public static void Initialize(IEnumerable<Assembly> assemblies, Func<Assembly, IEnumerable<Type>>? getConfigSections = null)
    {
        _assemblies = assemblies;
        _getConfigSections = getConfigSections;
    }

    #region Protected Methods

    /// <summary>
    /// Visits all configured assemblies and generates a dictionary of pages.
    /// </summary>
    /// <param name="getConfigSections">Optional function to filter configuration section types from assemblies.</param>
    /// <returns>A dictionary mapping page keys to <see cref="Page"/> instances.</returns>
    protected Dictionary<string, Page> VisitAssembliesAndGetPages(Func<Assembly, IEnumerable<Type>>? getConfigSections = null)
    {
        VisitAssemblies(_assemblies, _getConfigSections ?? getConfigSections);
        return _pages;
    }

    /// <summary>
    /// Visits an object type and creates corresponding page structure (pages, groups, options, pagelists).
    /// </summary>
    protected override void VisitObject(string? parentPath, Type objectType, object instance, bool seededSection = false)
    {
        if (string.IsNullOrEmpty(parentPath))
        {
            InitializeTopLevelPage(objectType);
        }

        var currentPath = GetCurrentPath(parentPath, objectType);
        var properties = GetAllReadWriteProperties(objectType);

        foreach (var property in properties)
        {
            ProcessProperty(property, objectType, instance, currentPath);
        }

        SetNonOverlappingRanges(objectType, currentPath);
        PopNavigationStacks();
    }

    /// <summary>
    /// Visits an array property and creates a <see cref="PageList"/> structure.
    /// </summary>
    protected override void VisitArray(PropertyInfo property, Type elementType, string keyPrefix, object? array, bool seededSection = false)
    {
        var isProviderManaged = property.GetCustomAttribute<ProviderPageListAttribute>() != null;
        if (!isProviderManaged && elementType.GetProperty(PageList.RequiredTablePropertyName) == null)
        {
            throw new InvalidOperationException($"PageList Page [{elementType.Name}] must have property '{PageList.RequiredTablePropertyName}'");
        }

        var pageList = CreatePageList(property, elementType, keyPrefix);
        _pageStructureDepth++;

        // Build PageList structure
        BuildPageListStructure(pageList, property, elementType, keyPrefix);
        // Build PageList entries if initialized
        BuildPageListEntries(pageList, property, elementType, keyPrefix, array);

        _pageStructureDepth--;
        _currentParent?.Children?.Add(pageList);
    }

    /// <summary>
    /// Visits a value property and creates an <see cref="Option"/> item.
    /// </summary>
    protected override void VisitValue(PropertyInfo? property, string key, object? value, object? defaultValue)
    {
        if (property == null)
        {
            return;
        }

        var option = CreateOptionFromProperty(property, key, value, defaultValue);
        PageHelper.SetDependency(property, key, option, _currentKeys);

        _currentKeys.Add(key);
        _currentParent?.Children.Add(option);
    }

    #endregion

    #region Virtual Methods

    /// <summary>
    /// Initializes a top-level page for a root configuration section.
    /// </summary>
    /// <param name="objectType">The type representing the root configuration section.</param>
    protected virtual void InitializeTopLevelPage(Type objectType)
    {
        var label = GetTopLevelPageLabel(objectType);
        var header = GetTopLevelPageHeader(objectType);
        var page = CreateTopLevelPage(objectType, label, header);

        _pages.Add(page.Key, page);
        PushNavigationStacks(page);
    }

    /// <summary>
    /// Gets the label resource key for a top-level page.
    /// </summary>
    protected virtual string GetTopLevelPageLabel(Type objectType)
    {
        return objectType.GetCustomAttribute<DisplayNameLocalAttribute>()?.ResourceName
            ?? throw new ArgumentException($"DisplayNameLocalAttribute is missing from root level page '{SettingsServiceCollectionExtensions.GetConfigSectionName(objectType)}'");
    }

    /// <summary>
    /// Gets the header resource key for a top-level page.
    /// </summary>
    protected virtual string GetTopLevelPageHeader(Type objectType)
    {
        return objectType.GetCustomAttribute<GroupHeaderAttribute>()?.ResourceName ?? Translations.Settings_General_Group_Header;
    }

    /// <summary>
    /// Creates a top-level <see cref="Page"/> instance.
    /// </summary>
    protected virtual Page CreateTopLevelPage(Type objectType, string label, string header)
    {
        var localizedLabel = GetLocalizedText(label);
        return new Page
        {
            Label = localizedLabel,
            Header = GetLocalizedText(header),
            Url = localizedLabel,
            Key = SettingsServiceCollectionExtensions.GetConfigSectionName(objectType),
            Permissions = GetPermissionForType(objectType),
            ExcludeFromImportExport = HasExcludeFromImportExportAttribute(objectType)
        };
    }

    /// <summary>
    /// Gets the permission string for a type.
    /// </summary>
    protected static string[] GetPermissionForType(Type type)
    {
        return type
        .GetCustomAttributes<PermissionAttribute>()
            .Select(a => a.Permission)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Gets localized text for a resource key.
    /// </summary>
    protected virtual string GetLocalizedText(string resourceKey)
    {
        if (_localizer == null)
        {
            return resourceKey;
        }

        return _localizer[resourceKey];
    }

    /// <summary>
    /// Gets the label resource key for a property.
    /// </summary>
    protected virtual string GetPropertyLabel(PropertyInfo property)
    {
        return property.GetCustomAttribute<DisplayNameLocalAttribute>()?.ResourceName
            ?? throw new ArgumentException($"DisplayNameLocalAttribute is missing from '{property?.Name}'");
    }

    /// <summary>
    /// Gets the header resource key for a type.
    /// </summary>
    protected virtual string GetTypeHeader(Type type)
    {
        return type.GetCustomAttribute<GroupHeaderAttribute>()?.ResourceName ?? Translations.Settings_General_Group_Header;
    }

    /// <summary>
    /// Gets the help text resource key for a type.
    /// </summary>
    protected virtual string GetHelpText(Type type)
    {
        return type.GetCustomAttribute<HelpTextAttribute>()?.ResourceName ?? string.Empty;
    }

    /// <summary>
    /// Adds a page to the internal pages collection.
    /// </summary>
    protected virtual void AddPageToCollection(Page page)
    {
        _pages.Add(page.Key, page);
    }

    #endregion

    #region Private Helper Methods - Object Processing

    private static string GetCurrentPath(string? parentPath, Type objectType)
    {
        return string.IsNullOrEmpty(parentPath)
            ? SettingsServiceCollectionExtensions.GetConfigSectionName(objectType)
            : parentPath;
    }

    private void ProcessProperty(PropertyInfo property, Type objectType, object instance, string currentPath)
    {
        var type = property.PropertyType;
        var propertyPath = BuildPropertyPath(currentPath, property.Name);

        _currentKeys.Add(propertyPath);

        if (type == typeof(SettingsAction))
        {
            var action = property.GetValue(instance, null) as SettingsAction
                ?? throw new InvalidOperationException($"Settings action '{property.DeclaringType?.FullName}.{property.Name}' must be initialized.");
            ProcessActionButtonProperty(property, objectType, propertyPath, action);
            return;
        }

        if (TypeIsValue(type))
        {
            ProcessValueProperty(property, objectType, instance, propertyPath);
        }
        else if (TypeIsArrayOrCompatible(type, out var elementType))
        {
            var array = property.GetValue(instance, null);
            VisitArray(property, elementType!, propertyPath, array);
        }
        else if (TypeIsDictionaryInterface(type) || !type.IsClass)
        {
            throw new NotSupportedException($"Property {property.Name} in {objectType.FullName} has an unsupported type {type.Name}.");
        }
        else
        {
            VisitComplexType(property, objectType, instance, type, propertyPath);
        }
    }

    private void ProcessActionButtonProperty(PropertyInfo property, Type objectType, string propertyPath, SettingsAction action)
    {
        var helpText = property.GetCustomAttribute<HelpTextAttribute>()?.ResourceName ?? string.Empty;

        var button = new ActionButton
        {
            Key = propertyPath,
            Label = GetLocalizedText(GetPropertyLabel(property)),
            LabelHelpText = GetLocalizedText(helpText),
            Url = _currentParent?.Url ?? string.Empty,
            Permissions = GetPermissionAttributeValue(property, objectType),
            ActionKey = action.ActionKey,
            ButtonType = action.ButtonType,
            ShowOnlyInRndMode = property.GetCustomAttribute<ShowOnlyInRndModeAttribute>() != null
        };

        _currentParent?.Children.Add(button);
    }

    private void ProcessValueProperty(PropertyInfo property, Type objectType, object instance, string propertyPath)
    {
        // Get the *declared* default value of the property.
        // For properties inside a settings group this means:
        //   - Use the value from the property's initializer
        //     (e.g. ImmunoCap = new() { WasteTypeEmpty = ..., ... })
        //   - NOT the current value on the existing instance.
        //
        // Using `var propInstance = Activator.CreateInstance(objectType)!`
        // instead of `instance` gives us the value as defined by the initializer,
        // rather than whatever the runtime has set at runtime.
        var defaultValue = property.GetValue(instance, null) ?? string.Empty;
        var value = property.GetValue(instance, null);

        // validate seeded values so no invalid values go to DB.
        // NOTE: Do not validate values while building the *page structure* for PageLists.
        // PageList structure is built using a fresh Activator.CreateInstance(elementType),
        // which can legitimately contain default values (e.g. 0 for int) that violate
        // runtime constraints like [SettingsRange(1,5)]. Those constraints should be
        // enforced when saving/importing settings, not during page metadata generation.
        if (!IsPageStructure)
        {
            ValidatePropertyValue(property, objectType, value);
        }
         VisitValue(property, propertyPath, value, defaultValue);
    }

    private static void ValidatePropertyValue(PropertyInfo property, Type objectType, object? value)
    {
        if (!ValueValidator.IsValidValue(property, value))
        {
            throw new ValidationException($"Property {property.Name} in {objectType.FullName} has an invalid value.");
        }
    }

    private static string BuildPropertyPath(string currentPath, string propertyName)
    {
        return currentPath + PageHelper.SettingsKeyDeLimiter + propertyName;
    }

    private void SetNonOverlappingRanges(Type objectType, string currentPath)
    {
        if (_currentParent == null)
        {
            return;
        }

        var attributes = objectType.GetCustomAttributes<NonOverlappingRangesAttribute>().ToList();
        if (attributes.Count == 0)
        {
            return;
        }

        var options = PageHelper.FindOptions(_currentParent);
        foreach (var attribute in attributes)
        {
            var validation = CreateNonOverlappingRangesValidation(objectType, currentPath, attribute);
            foreach (var rangeKey in validation.RangeKeys)
            {
                if (!options.TryGetValue(rangeKey, out var option))
                {
                    throw new ArgumentException($"Property '{rangeKey}' defined in {nameof(NonOverlappingRangesAttribute)} was not found in '{objectType.FullName}'.");
                }

                option.NonOverlappingRanges.Add(validation);
            }
        }
    }

    private static NonOverlappingRangesValidation CreateNonOverlappingRangesValidation(
        Type objectType,
        string currentPath,
        NonOverlappingRangesAttribute attribute)
    {
        var enabledWhenProperties = attribute.EnabledWhenProperties.ToList();
        var enabledWhenValues = attribute.EnabledWhenValues.ToList();
        if (enabledWhenProperties.Count != enabledWhenValues.Count)
        {
            throw new ArgumentException($"The number of enabled properties and values must match in '{objectType.FullName}' {nameof(NonOverlappingRangesAttribute)}.");
        }

        ValidatePropertyExists(objectType, attribute.FirstStartProperty);
        ValidatePropertyExists(objectType, attribute.FirstLengthProperty);
        ValidatePropertyExists(objectType, attribute.SecondStartProperty);
        ValidatePropertyExists(objectType, attribute.SecondLengthProperty);
        foreach (var property in enabledWhenProperties)
        {
            ValidatePropertyExists(objectType, property);
        }

        return new NonOverlappingRangesValidation
        {
            FirstStartKey = BuildPropertyPath(currentPath, attribute.FirstStartProperty),
            FirstLengthKey = BuildPropertyPath(currentPath, attribute.FirstLengthProperty),
            SecondStartKey = BuildPropertyPath(currentPath, attribute.SecondStartProperty),
            SecondLengthKey = BuildPropertyPath(currentPath, attribute.SecondLengthProperty),
            EnabledWhen = CreateEnabledWhenDependencies(currentPath, enabledWhenProperties, enabledWhenValues)
        };
    }

    private static List<Dependency> CreateEnabledWhenDependencies(
        string currentPath,
        List<string> enabledWhenProperties,
        List<string> enabledWhenValues)
    {
        var dependencies = new List<Dependency>();
        for (var i = 0; i < enabledWhenProperties.Count; i++)
        {
            dependencies.Add(new Dependency
            {
                MainOptionKey = BuildPropertyPath(currentPath, enabledWhenProperties[i]),
                MainOptionValue = enabledWhenValues[i],
                Type = DependencyType.Disable
            });
        }

        return dependencies;
    }

    private static void ValidatePropertyExists(Type objectType, string propertyName)
    {
        if (objectType.GetProperty(propertyName) == null)
        {
            throw new ArgumentException($"Property '{propertyName}' defined in {nameof(NonOverlappingRangesAttribute)} was not found in '{objectType.FullName}'.");
        }
    }

    #endregion

    #region Private Helper Methods - Complex Type Processing

    /// <summary>
    /// Processes a complex type property, creating either a <see cref="Group"/> or a <see cref="Page"/>.
    /// </summary>
    protected void VisitComplexType(PropertyInfo property, Type objectType, object instance, Type type, string propertyPath)
    {
        var propInstance = Activator.CreateInstance(type)!;
        var item = property.GetValue(instance, null);
        var label = GetPropertyLabel(property);
        var header = GetTypeHeader(type);
        var permissions = GetPermissionAttributeValue(property, type);
        var excludeFromImportExport = HasExcludeFromImportExportAttribute(property, type);

        if (IsSettingsGroup(property, objectType))
        {
            CreateAndVisitGroup(property, propertyPath, label, permissions, excludeFromImportExport, type, item, propInstance);
        }
        else
        {
            CreateAndVisitPage(property, propertyPath, label, header, permissions, excludeFromImportExport, type, item, propInstance);
        }
    }

    private bool IsSettingsGroup(PropertyInfo property, Type objectType)
    {
        var hasGroupAttribute = property.CustomAttributes.Any(a => a.AttributeType == typeof(SettingsGroupAttribute));

        if (hasGroupAttribute && _currentParent is Group)
        {
            throw new ArgumentException($"Cannot have group '{property.Name}' inside group '{objectType.Name}', use page inside group instead");
        }

        return hasGroupAttribute;
    }

    private void CreateAndVisitGroup(PropertyInfo property, string propertyPath, string label, string[] permissions, bool excludeFromImportExport, Type type, object? item, object propInstance)
    {
        var group = CreateGroup(propertyPath, label, permissions);
        group.ExcludeFromImportExport = excludeFromImportExport;
        group.ShowOnlyInRndMode = property.GetCustomAttribute<ShowOnlyInRndModeAttribute>() != null;
        PageHelper.SetDependency(property, propertyPath, group, _currentKeys);

        _currentKeys.Add(propertyPath);
        _currentParent?.Children.Add(group);

        PushNavigationStacks(group);
        VisitObject(propertyPath, type, item ?? propInstance);
    }

    private void CreateAndVisitPage(PropertyInfo property, string propertyPath, string label, string header, string[] permissions, bool excludeFromImportExport, Type type, object? item, object propInstance)
    {
        var page = CreateNestedPage(propertyPath, label, header, permissions);
        page.ExcludeFromImportExport = excludeFromImportExport;
        page.ShowOnlyInRndMode = property.GetCustomAttribute<ShowOnlyInRndModeAttribute>() != null;
        PageHelper.SetDependency(property, propertyPath, page, _currentKeys);

        _currentKeys.Add(propertyPath);
        _currentParent?.Children.Add(page);
        AddPageToCollection(page);

        PushNavigationStacks(page);
        VisitObject(propertyPath, type, item ?? propInstance);
    }

    private Group CreateGroup(string propertyPath, string label, string[] permissions)
    {
        var localizedLabel = GetLocalizedText(label);
        return new Group
        {
            Key = propertyPath,
            HelpUrl = _currentParent?.Url + PageHelper.SettingsKeyDeLimiter + localizedLabel,
            Url = _currentParent?.Url + PageHelper.SettingsLabelDeLimiter + localizedLabel,
            Label = localizedLabel,
            Permissions = permissions
        };
    }

    private Page CreateNestedPage(string propertyPath, string label, string header, string[] permissions)
    {
        var localizedLabel = GetLocalizedText(label);
        return new Page
        {
            Key = propertyPath,
            Url = _currentParent?.Url + PageHelper.SettingsLabelDeLimiter + localizedLabel,
            Label = localizedLabel,
            Header = GetLocalizedText(header),
            Permissions = permissions
        };
    }

    #endregion

    #region Private Helper Methods - PageList Processing

    private static PageList CreatePageList(PropertyInfo property, Type elementType, string keyPrefix)
    {
        var providerAttr = property.GetCustomAttribute<ProviderPageListAttribute>();
        if (providerAttr != null)
        {
            ValidateProviderManagedPageList(property, elementType, providerAttr);
        }

        var maxItems = property.GetCustomAttribute<PageListMaxItemsAttribute>()?.MaxItems;
        var minItems = property.GetCustomAttribute<PageListMinItemsAttribute>()?.MinItems;
        ValidatePageListMinItems(property, maxItems, minItems);

        return new PageList
        {
            Key = keyPrefix,
            Permissions = GetPermissionAttributeValue(property, elementType),
            HasCopyFunction = property.GetCustomAttribute<CopyFunctionAttribute>() != null,
            IsProviderManaged = providerAttr != null,
            ExcludeFromImportExport = HasExcludeFromImportExportAttribute(property, elementType),
            EntryProviderKey = providerAttr?.ProviderKey,
            ProviderKeyPropertyName = providerAttr?.KeyPropertyName,
            MaxItems = maxItems,
            MinItems = minItems,
            ShowOnlyInRndMode = property.GetCustomAttribute<ShowOnlyInRndModeAttribute>() != null
        };
    }

    private static void ValidatePageListMinItems(PropertyInfo property, int? maxItems, int? minItems)
    {
        if (!maxItems.HasValue || !minItems.HasValue || maxItems.Value > minItems.Value)
        {
            return;
        }

        throw new InvalidOperationException(
            $"PageList '{property.Name}' on '{property.DeclaringType?.FullName}' must have PageListMaxItems greater than PageListMinItems. " +
            $"PageListMaxItems: {maxItems.Value}, MinItems: {minItems.Value}.");
    }

    private static void ValidateProviderManagedPageList(PropertyInfo property, Type elementType, ProviderPageListAttribute attribute)
    {
        var keyProperty = elementType.GetProperty(attribute.KeyPropertyName);
        if (keyProperty == null)
        {
            throw new InvalidOperationException(
                $"Provider-managed PageList '{property.Name}' on '{property.DeclaringType?.FullName}' must have key property '{attribute.KeyPropertyName}' on entry type '{elementType.FullName}'.");
        }

        if (!keyProperty.CanRead || !keyProperty.CanWrite || keyProperty.PropertyType != typeof(string))
        {
            throw new InvalidOperationException(
                $"Provider-managed PageList key property '{elementType.FullName}.{attribute.KeyPropertyName}' must be a read/write string property.");
        }
    }

    private void BuildPageListStructure(PageList pageList, PropertyInfo property, Type elementType, string keyPrefix)
    {
        var label = GetPropertyLabel(property);
        var header = GetTypeHeader(elementType);
        var helpText = GetHelpText(elementType);

        var page = new Page
        {
            Key = keyPrefix,
            Label = GetLocalizedText(label),
            Header = GetLocalizedText(header),
            Url = _currentParent?.Url + PageHelper.SettingsLabelDeLimiter + GetLocalizedText(label),
            LabelHelpText = GetLocalizedText(helpText),
            Permissions = GetPermissionAttributeValue(property, elementType)
        };

        _currentKeys.Add(keyPrefix);
        PushNavigationStacks(page);

        var propInstance = Activator.CreateInstance(elementType)!;
        VisitObjectWithProviderKey(keyPrefix, elementType, propInstance, pageList.ProviderKeyPropertyName);

        pageList.PageStructure = page;
        ConfigureProviderManagedKeyOption(pageList, page, keyPrefix);
        ConfigurePageListColumns(pageList, page, keyPrefix);
    }

    private void VisitObjectWithProviderKey(string keyPrefix, Type elementType, object instance, string? providerKeyPropertyName)
    {
        var forcedHiddenKey = AddProviderManagedKeyOption(keyPrefix, providerKeyPropertyName);
        try
        {
            VisitObject(keyPrefix, elementType, instance);
        }
        finally
        {
            if (forcedHiddenKey != null)
            {
                _forcedHiddenOptionKeys.Remove(forcedHiddenKey);
            }
        }
    }

    private string? AddProviderManagedKeyOption(string keyPrefix, string? providerKeyPropertyName)
    {
        if (string.IsNullOrWhiteSpace(providerKeyPropertyName))
        {
            return null;
        }

        var forcedHiddenKey = keyPrefix + PageHelper.SettingsKeyDeLimiter + providerKeyPropertyName;
        _forcedHiddenOptionKeys.Add(forcedHiddenKey);
        return forcedHiddenKey;
    }

    private static void ConfigureProviderManagedKeyOption(PageList pageList, Page page, string keyPrefix)
    {
        if (!pageList.IsProviderManaged)
        {
            return;
        }

        var keyPropertyName = pageList.ProviderKeyPropertyName
            ?? throw new InvalidOperationException($"Provider-managed PageList at key '{keyPrefix}' is missing its provider key property name.");

        var keyOption = FindEntryOption(page, keyPrefix, keyPropertyName)
            ?? throw new InvalidOperationException($"Provider-managed PageList at key '{keyPrefix}' is missing key option '{keyPropertyName}'.");

        keyOption.IsHidden = true;
    }

    private static void ConfigurePageListColumns(PageList pageList, Page page, string keyPrefix)
    {
        var namePropertyOption = FindNameColumnOption(page, keyPrefix);
        if (namePropertyOption == null)
        {
            if (!pageList.IsProviderManaged)
            {
                throw new ArgumentException($"No correct Name property found in PageList at key '{keyPrefix}'");
            }

            namePropertyOption = CreateProviderManagedDisplayNameOption(keyPrefix);
            page.Children.Insert(0, namePropertyOption);
        }

        namePropertyOption.Required = true;
        namePropertyOption.IsNameColumn = true;
        pageList.ColumnHeaderLabels.Add(namePropertyOption.Label);

        var additionalColumns = PageListColumnHelper.GetColumnOptions(page)
            .Where(o => o.IsColumnHeader)
            .Select(o => o.Label)
            .ToList();

        pageList.ColumnHeaderLabels.AddRange(additionalColumns);
    }

    private static Option? FindNameColumnOption(Page page, string keyPrefix)
    {
        return FindEntryOption(page, keyPrefix, PageList.RequiredTablePropertyName);
    }

    private static Option? FindEntryOption(Page page, string pageKey, string propertyName)
    {
        var optionKey = pageKey + PageHelper.SettingsKeyDeLimiter + propertyName;
        return page.Children.OfType<Option>().FirstOrDefault(c => c.Key == optionKey);
    }

    private void BuildPageListEntries(PageList pageList, PropertyInfo property, Type elementType, string keyPrefix, object? array)
    {
        if (array == null)
        {
            return;
        }

        var label = GetPropertyLabel(property);
        var header = GetTypeHeader(elementType);
        var helpText = GetHelpText(elementType);
        var index = 0;

        // Initialized page list entries
        foreach (var item in (IEnumerable)array)
        {
            var pageListEntry = CreatePageListEntry(item, elementType, keyPrefix, index, label, header, helpText, pageList.ProviderKeyPropertyName);
            pageList.PageEntries.Add(pageListEntry);
            index++;
        }
    }

    /// <summary>
    ///  Method for creating a single PageList entry page for initialized PageList entries.
    /// </summary>
    /// <returns></returns>
    private Page CreatePageListEntry(
        object item,
        Type elementType,
        string keyPrefix,
        int index,
        string label,
        string header,
        string helpText,
        string? providerKeyPropertyName)
    {
        var pageListKeyPrefix = keyPrefix + PageHelper.SettingsKeyDeLimiter + index;
        var pageListPage = CreatePageListEntryPage(pageListKeyPrefix, label, header, helpText);

        AddPageToCollection(pageListPage);
        PushNavigationStacks(pageListPage);

        VisitObjectWithProviderKey(pageListKeyPrefix, elementType, item, providerKeyPropertyName);

        ConfigurePageListEntryLabel(pageListPage, pageListKeyPrefix, providerKeyPropertyName);
        FixPageListHelpUrl(pageListPage, pageListPage.Url);

        return pageListPage;
    }

    private Page CreatePageListEntryPage(string key, string label, string header, string helpText)
    {
        return new Page
        {
            Key = key,
            Label = GetLocalizedText(label),
            Header = GetLocalizedText(header),
            LabelHelpText = GetLocalizedText(helpText)
        };
    }

    private void ConfigurePageListEntryLabel(Page pageListPage, string pageListKeyPrefix, string? providerKeyPropertyName)
    {
        var nameKey = pageListKeyPrefix + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName;
        var nameOption = pageListPage.Children.FirstOrDefault(c => c.Key == nameKey) as Option;
        if (nameOption == null)
        {
            if (string.IsNullOrWhiteSpace(providerKeyPropertyName))
            {
                throw new ArgumentException($"No correct Name property in PageList entry at key '{pageListKeyPrefix}'");
            }

            var keyOption = pageListPage.Children.FirstOrDefault(c => c.Key == pageListKeyPrefix + PageHelper.SettingsKeyDeLimiter + providerKeyPropertyName) as Option
                ?? throw new ArgumentException($"Provider-managed PageList entry at key '{pageListKeyPrefix}' is missing key option '{providerKeyPropertyName}'.");

            nameOption = CreateProviderManagedDisplayNameOption(pageListKeyPrefix, keyOption.Value);
            pageListPage.Children.Insert(0, nameOption);
        }

        nameOption.Required = true;
        nameOption.IsNameColumn = true;
        pageListPage.Label = nameOption.Value!;
        var entryIndex = pageListKeyPrefix.Split(PageHelper.SettingsKeyDeLimiter)[^1];
        pageListPage.Url = _currentParent!.Url + PageHelper.SettingsLabelDeLimiter + entryIndex;
    }

    private static Option CreateProviderManagedDisplayNameOption(string keyPrefix, string? value = null)
    {
        return new Option
        {
            Key = keyPrefix + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName,
            Label = PageList.ProviderManagedDisplayNameColumnHeader,
            Value = value ?? string.Empty,
            DefaultValue = string.Empty,
            Type = OptionType.String,
            IsReadOnly = true,
            IsNameColumn = true,
            Required = true
        };
    }

    /// <summary>
    /// Recursively fixes the LabelFromRoot property for all children in a PageList structure.
    /// This is necessary when PageList entries are initialized with data.
    /// </summary>
    protected static void FixPageListHelpUrl(ISettingsParentItem pageListPage, string labelFromRoot)
    {
        foreach (var child in pageListPage.Children)
        {
            switch (child)
            {
                case Page page:
                    page.Url = labelFromRoot + page.Url;
                    FixPageListHelpUrl(page, labelFromRoot);
                    break;

                case Group group:
                    group.Url = labelFromRoot + group.Url;
                    FixPageListHelpUrl(group, labelFromRoot);
                    break;

                case PageList pageList:
                    if (pageList.PageStructure == null)
                    {
                        throw new ArgumentException("PageStructure cannot be null!");
                    }
                    pageList.PageStructure.Url = labelFromRoot + pageList.PageStructure.Url;
                    FixPageListHelpUrl((Page)pageList.PageStructure, labelFromRoot);

                    foreach (var pageEntry in pageList.PageEntries)
                    {
                        pageEntry.Url = labelFromRoot + pageEntry.Url;
                        FixPageListHelpUrl((Page)pageEntry, labelFromRoot);
                    }
                    break;
            }
        }
    }

    #endregion

    #region Private Helper Methods - Option Creation

    protected Option CreateOptionFromProperty(PropertyInfo property, string key, object? value, object? defaultValue)
    {
        var required = property.CustomAttributes.Any(a => a.AttributeType == typeof(RequiredAttribute));
        var option = _valueConverter.ValueToOption(property, key, value, defaultValue, required, GetLocalizedText);


        option.IsObfuscated = property.CustomAttributes.Any(a => a.AttributeType == typeof(ObfuscateSettingAttribute));
        option.IsEncrypted = property.CustomAttributes.Any(a => a.AttributeType == typeof(ProtectedDataAttribute));
        option.IsAbsoluteDirectoryPath = property.GetCustomAttribute<AbsoluteDirectoryPathAttribute>() != null;

        var isHidden = _forcedHiddenOptionKeys.Contains(key);
        var label = isHidden ? property.Name : GetPropertyLabel(property);
        var helpText = property.GetCustomAttribute<HelpTextAttribute>()?.ResourceName ?? string.Empty;

        option.Label = GetLocalizedText(label);
        option.LabelHelpText = GetLocalizedText(helpText);
        option.ShowOnlyInRndMode = property.GetCustomAttribute<ShowOnlyInRndModeAttribute>() != null;

        // Apply attributes to options
        var uniqueAttribute = property.GetCustomAttribute<UniqueAttribute>();
        option.Unique = uniqueAttribute != null;
        option.UniqueIsCaseInsensitive = uniqueAttribute?.CaseInsensitive ?? false;
        option.Permissions = GetPermissionAttributeValue(property);
        option.IsHidden = isHidden;
        option.IsReadOnly = property.GetCustomAttribute<ReadOnlySettingAttribute>() != null;
        option.ExcludeFromImportExport = HasExcludeFromImportExportAttribute(property);
        if (property.CustomAttributes.Any(a => a.AttributeType == typeof(ValueAfterCopy)))
        {
            var copyValue = property.GetCustomAttribute<ValueAfterCopy>()?.Value;
            option.ValueAfterCopy = copyValue!;
        }
        // If a column in a PageList
        option.IsColumnHeader = property.GetCustomAttribute<TableColumnAttribute>() != null;

        return option;
    }

    private static bool HasExcludeFromImportExportAttribute(PropertyInfo property, Type? type = null)
    {
        return property.GetCustomAttribute<ExcludeFromImportExportAttribute>() != null
            || HasExcludeFromImportExportAttribute(type);
    }

    private static bool HasExcludeFromImportExportAttribute(Type? type)
    {
        return type?.GetCustomAttribute<ExcludeFromImportExportAttribute>() != null;
    }

    /// <summary>
    /// Gets the permission attribute value from a property or type.
    /// </summary>
    /// <param name="property">The property to check for permission attribute.</param>
    /// <param name="type">Optional type to check if property attribute is not found.</param>
    /// <returns>The permission string, or empty string if no permission is defined.</returns>
    protected static string[] GetPermissionAttributeValue(PropertyInfo property, Type? type = null)
    {
        var propertyPermissions = property
        .GetCustomAttributes<PermissionAttribute>()
            .Select(a => a.Permission)
            .Distinct()
            .ToArray();

        if (propertyPermissions.Length > 0)
        {
            return propertyPermissions;
        }

        if (type != null)
        {
            var typePermissions = type
                .GetCustomAttributes<PermissionAttribute>()
                .Select(a => a.Permission)
                .Distinct()
                .ToArray();

            if (typePermissions.Length > 0)
            {
                return typePermissions;
            }
        }

        return [];
    }

    #endregion

    #region Private Helper Methods - Navigation Stack Management

    private void PushNavigationStacks(ISettingsParentItem parent)
    {
        _currentParent = parent;
        _parents.Push(parent);

        var keys = new List<string>();
        _currentKeys = keys;
        _keys.Push(keys);
    }

    private void PopNavigationStacks()
    {
        if (_parents.Count > 0)
        {
            _parents.Pop();
            _currentParent = _parents.Count == 0 ? null : _parents.Peek();

            _keys.Pop();
            _currentKeys = _keys.Count == 0 ? [] : _keys.Peek();
        }
    }

    #endregion
}
