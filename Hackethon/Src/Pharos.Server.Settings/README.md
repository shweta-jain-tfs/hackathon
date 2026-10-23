# Development instructions for the Pharos Settings module

Pharos settings utilize the standard [.NET Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
with minor adjustments.

Configuration in .NET is performed using one or more configuration providers.
Configuration providers in Pharos read configuration data from key-value pairs
from the following configuration sources:

* Settings files, such as appsettings.json
  * Settings files must only contain settings invisible to users after Pharos
	installation (such as database connection strings). Users cannot modify them
	from UI.
* Environment variables
  * Environment variables override settings provided in setting files. They should
    only be used in a development environment (e.g. for automated testing).
* Pharos [custom configuration provider](https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider)
  * Settings from this provider are discovered during Pharos startup are stored
    in the database.

## Initializing Pharos configuration provider

To enable Pharos settings discovery and storage add the `AddEntityConfiguration`
extension method call to the `Program.cs` file in the Pharos.Server project.
The call must be made before you start adding services and initializing modules.

```
var builder = WebApplication.CreateBuilder(options);
...
configuration.AddEntityConfiguration(Modules.ModuleList.Select(m => m.Assembly).ToList());
// Adding services follows...
```
The `AddEntityConfiguration` method searches the provided list of assemblies for
root configuration sections marked by the `RootConfigSectionAttribute` and builds
the settings tree. At this point only default setting values are available.
When the Settings module is fully initialized, current setting values are loaded
from the database.

__NOTE:__ If you cannot see your section in the loaded configuration, make sure
that its declaring assembly is included into the the list in the `AddEntityConfiguration`
call.


## How to add new settings

To add a new configuration section (a group of settings) you need to do the following:
1. Create a settings class and declare required settings.
2. Add new settings to the settings tree.
3. Add new settings to the UI (This is done automatically by Settings framework).

### 1. Declaring new settings

When declaring new settings, a settings class:
* Must be non-abstract with a public parameter-less constructor.
* Contain public read-write properties to bind (fields are not bound).


If you want to declare a setting that has a default value use `DefaultValueAttribute`
on the property.

NOTE: A settings class does not need to be public, unless you want to read some
settings in more than one module.

### 2. Adding new settings to the settings tree

Pharos only includes those settings to the tree that are reachable from one of root
configuration sections marked by the `RootConfigSectionAttribute`. Thus, when you
are adding new settings there are three options:

1. Declare a new root configuration section class marked by the `RootConfigSectionAttribute`.
2. Declare a new settings group class and include this group as an object property into one of the existing settings classes.
3. Add new settings to an existing group (no need to do anything).

If existing settings are moved, old entries are removed from the database on startup.

### 3. Adding new settings to the UI

The Settings module scans the settings classes starting from the root sections to
create a UI element specification. 
Lis settings have been created, they can be used as an example - Pharos.Server.Lis.Application.Settings
The following attributes are used to decorate
classes and properties of a config section and provide information to the UI:

### 4. Initializing settings to DB

By implementing `IConfigSectionWithInitialValues`, its method `InitializeConfigSection` is executed during initialization.
This allows defining values that will be stored to the database on the first run. The isDevelopment parameter can be 
used to control whether values are initialized for development environments, production environments, or both.

#### Mandatory Attributes

* `DisplayNameLocalAttribute` - provides a localized display name (header or label)
  for a settings element (option, page, or group). Translations for display names are also mandatory.
  The translations are stored in `SettingsTranslations.resx` resource file.


#### Optional Attributes

* `DefaultValueAttribute` - DO NOT USE THIS ATTRIBUTE. Use intitializers instead, e.g.: `public int MySetting {get; set;} = 10;`
* `DependsFromAttribute` - specifies a UI-dependency for a setting, group, or page. The dependent setting is enabled/visible only when the controlling property has the expected value.
	For a single property and value: `[DependsFrom(nameof(MyProp), nameof(MyEnum.ValueA), DependencyType.Disable)]`
	For multiple conditions use the array overload. Each entry in the properties array is matched against the corresponding entry in the values array:
	`[DependsFrom(new[] { nameof(PropA), nameof(PropB) }, new[] { nameof(MyEnum.ValueA), nameof(MyEnum.ValueB) }, DependencyType.Disable)]`
	To enable the setting when a property matches any one of several values, repeat the same property name once per value (the setting is enabled if at least one pair matches).
	`[DependsFrom(new[] { nameof(PropA), nameof(PropA) }, new[] { nameof(MyEnum.ValueA), nameof(MyEnum.ValueB) }, DependencyType.Disable)]`
* `GroupHeaderAttribute` - defines the name for the default group of the page where free options and pages go.
	Is by default 'General'.
* `RequiredAttribute` - specifies that the setting value is required (when editing) meaning that the field cannot be null or whitespace.
* `ReadOnlySettingAttribute` - specifies that the setting is visible in the UI but cannot be edited by the user.
	This is useful when the value is controlled by the system or by a provider-managed list.
* `UniqueAttribute` - specifies that two distinct, same-type items cannot have equal value (when editing). With the parameter CaseInsensitive it specifies that the comparison should be case insensitive.
* `SettingsRangeAttribute` - specifies that the setting value must be within a specified range.
* `MaxLengthAttribute` - specifies a max length for string option value.
* `DisallowedCharactersAttribute` - specifies a regular expression that identifies characters which are not allowed in a string option value.
	For example, `[DisallowedCharacters(@"[<>]")]` rejects values containing `<` or `>`.
* `SettingsGroupAttribute` - specifies that a class works like a settings group within the parent page.
* `PageListAttribute` - Specifies that a list has a page structure for every entry in the list.
	The PageList, by default, only has one column which is the value of a named property in the attribute -
	NOTE: A regular PageList page (list class) must have a property named 'Name', this is the named property.
	If the PageList attribute is not present on an enumerable 
	a regular table which has a column for each property in the class will be created. 
	This regular table's class can only have Options as properties, i.e. bool, string, int, double, date.
* `ProviderPageListAttribute` - specifies that a PageList is managed by a keyed `IPageListEntryProvider`.
	The attribute takes the keyed provider service key and the name of a string property on the PageList entry type that stores the provider entry key:
	`[ProviderPageList(MyProviderKeys.MyEntryProvider, nameof(MyEntry.ExternalKey))]`.
	The provider returns `PageListEntry` values containing a stable `Key` and a `DisplayName`.
	The settings framework matches existing entries by the configured key property, creates missing entries, and removes entries that are no longer returned by the provider.
	The key property must be a public read/write string property and is always hidden in the UI.
	A provider-managed PageList does not need a `Name` property on the entry type; the provider `DisplayName` is used for the visible list label and page path.
	Provider-managed PageLists cannot be manually created, copied, or deleted in the UI.
	Register the provider in DI as a keyed `IPageListEntryProvider` using the same key as the attribute.
* `ExcludeFromImportExportAttribute` - specifies that a setting or settings subtree is excluded from settings import and export.
	On an option property, only that setting is excluded.
	On a complex property, PageList property, or settings class, all settings below that key are excluded.
	On a property inside a PageList entry class, the matching setting is excluded for every PageList entry.
	This can be combined with `ProviderPageListAttribute` when provider-managed entries should not be transferred between systems.
* `TableColumnAttribute` - To have more than the Name column in a PageList in the UI it is possible by adding 
	this attribute to a property in the PageList entry class or in a nested page/group below the entry class. NOTE: the property must be an option, i.e. string, int, double, date, choice(enum) or bool.
* `HelpTextAttribute` - provides an additional text to supplement the display name
* `DynamicDropdownAttribute` - specifies which properties will be dynamic dropdowns and from where the values will come. 
	Two approaches are supported: Provider-based and PageList-based.
	The value stored to DB must also be defined. Supported type is a string for the property.
* `PermissionAttribute` - specifies which permission is required to view/edit the setting, group, page or sub-menu item.
	You are able to define multiple permissions by applying the attribute multiple times.
* `CopyFunctionAttribute` - CURRENTLY ONLY IN PLACE FOR LIS CHANNELS. Specifies that a copy button will be available in the UI for the setting and
	settings can be copied.
* `ValueAfterCopyAttribute` - CURRENTLY ONLY IN PLACE FOR LIS CHANNELS. Specifies the type and value that a setting should be set to when copied.
* `DisplayModeRadioButtons` - specifies that the setting will be displayed as radio buttons in the UI. Used for enum type settings.
* `PageListMaxItemsAttribute` - specifies a max for items in pagelist.
* `PageListMinItemsAttribute` - specifies the minimum number of items that must remain in a PageList after deletion.
* `ObfuscatedSettingAttribute` - marks a property as obfuscated, and it will be displayed in the UI as a password field.
* `ProtectedDataAttribute` - marks a property as protected, and its value will be stored in a encrypted column in the settings database.
* `SettingsAction` - marks a property in a settings class as an action button rendered on the settings page.
  The constructor takes an `ActionKey` enum value that identifies which action to execute when the button is clicked
  (e.g. `new SettingsAction(ActionKey.Import)`).
* `ShowOnlyInRndModeAttribute` - hides a property from the UI when RnD mode is not enabled
* `AbsoluteDirectoryPathAttribute` - marks a property as an absolute directory path, and it will be validated accordingly
* `NonOverlappingRangesAttribute` - specifies that two ranges in the same settings class must not overlap. The ranges are defined with start-position and length properties. Optional enabled conditions can be provided with matching property/value arrays.

__TODO__


#### Translations

Pharos settings translations are stored in resource file named `SettingsTranslations.resx` in Pharos.Server.Settings module.



## Consuming settings on the server side

The recommended way of reading the setting values is to use [.NET Options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options).

According to options patterns you need to:
1. Configure options class binding.
2. Inject options into your code using one of the [options interfaces](https://learn.microsoft.com/en-us/dotnet/core/extensions/options#options-interfaces).

### Binding settings

To bind Pharos settings that are stored in the database, use the `SettingsServiceCollectionExtensions.ConfigureSettings<TSettings>()` method.
This method ensures that the settings class is bound to a  correct configuration path.

Settings that are read from files of environment variables, can be bound using
the .NET standard `Configure<TOptions>()` method call.

### Injecting settings

To read Pharos settings stored in the database use `IOptionsSnapshot<TOptions>` (scoped)
or `IOptionsMonitor<TOptions>` (singleton).

`IOptions<TOptions>` can only be used for static settings (e.g. from `appsettings.json`),
because it will not reload values when the settings change.

### Tracking changes in settings

To track changes in a specific settings group previously bound using `ConfigureSettings<TSettingsGroup>()` call
create a nofication handler for the `SettingsChangedEvent<TSettingsGroup>` event.

This event is fired whenever settings in the specified group are changed and saved.

Do not use `IOptionsMonitor<TOptions>.OnChange` callback unless you want to track changes in all settings
(not a sepcific group).
