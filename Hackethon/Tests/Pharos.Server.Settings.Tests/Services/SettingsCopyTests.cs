using Pharos.Server.Settings.Application.Commands.CopySettings;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Services;
public class SettingsCopyTests
{
    private static Page CreateSourcePage(string key, string labelFromRoot, IEnumerable<SettingsChildItem>? children = null)
    {
        return new Page
        {
            Key = key,
            Label = "SourceLabel",
            Url = labelFromRoot,
            Header = "Header",
            Dependency = [],
            Children = (children ?? Enumerable.Empty<SettingsChildItem>()).ToList()
        };
    }

    private static Option CreateNameOption(string key, string? value, string? label = null, bool isNameColumn = true, object? defaultValue = null, object? valueAfterCopy = null)
    {
        return new Option
        {
            Key = key,
            Label = label!,
            Value = value,
            DefaultValue = (string?)defaultValue,
            ValueAfterCopy = valueAfterCopy!,
            IsNameColumn = isNameColumn
        };
    }

    private static PageList CreatePageList(string key, Page structure, params Page[] entries)
    {
        return new PageList
        {
            Key = key,
            Label = "List",
            PageStructure = structure,
            PageEntries = entries.Cast<ChildPageItem>().ToList(),
            HasCopyFunction = true
        };
    }

    private static SettingsCopyResult Copy(SettingsCopy service, Dictionary<string, Page> populated, string key)
    {
        bool KeyExists(string _) => false;
        return service.CreateCopy(populated, key, KeyExists, 0);
    }

    private static SettingsCopy CreateService()
    {
        var pageTreeNavigator = new PageTreeNavigator();
        return new SettingsCopy(pageTreeNavigator);
    }

    [Fact]
    public void Returns_failure_when_source_missing()
    {
        var service = CreateService();
        var populated = new Dictionary<string, Page>(); // empty
        bool KeyExists(string _) => false;

        var result = service.CreateCopy(populated, "missing-key", KeyExists, 0);

        Assert.False(result.Success);
        Assert.Equal(CopySettingsErrorCode.SourcePageNotFound, result.ErrorCode);
    }

    [Fact]
    public void Returns_failure_when_pagelist_missing_or_disabled()
    {
        var service = CreateService();
        // Build a page without PageList owning the entry
        var sourceChildName = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var sourcePage = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { sourceChildName });
        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { sourcePage });

        var populated = new Dictionary<string, Page> { [root.Key] = root, [sourcePage.Key] = sourcePage };
        bool KeyExists(string _) => false;

        var result = service.CreateCopy(populated, sourcePage.Key, KeyExists, 0);

        Assert.False(result.Success);
        Assert.Equal(CopySettingsErrorCode.CopyDisabled, result.ErrorCode);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5315")]
    public void Computes_next_index_and_new_page_key()
    {
        var service = CreateService();

        var nameOptionA = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { nameOptionA });

        var nameOptionB = CreateNameOption("Lis:LisChannels:2:Name", "ChannelB", "ChannelB");
        var entry2 = CreateSourcePage("Lis:LisChannels:2", "Lis:LisChannels:ChannelB", new[] { nameOptionB });

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0, entry2); // indices 0 and 2 -> next should be 3

        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page>
        {
            [root.Key] = root,
            [entry0.Key] = entry0,
            [entry2.Key] = entry2
        };

        bool KeyExists(string _) => false;

        var result = service.CreateCopy(populated, entry0.Key, KeyExists, 2);
        Assert.True(result.Success);
        Assert.NotNull(result.NewPage);
        // new page should have key "Lis:LisChannels:3"
        Assert.Equal("Lis:LisChannels:3", result.NewPage!.Key);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5315")]
    public void Adjusts_name_for_copy_uniqueness()
    {
        var service = CreateService();

        var nameOptionA = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { nameOptionA });
        entry0.Label = "ChannelA";
        entry0.Url = "Lis:LisChannels:ChannelA";

        var nameOptionCopy = CreateNameOption("Lis:LisChannels:1:Name", "ChannelA_COPY", "ChannelA_COPY");
        var entry1 = CreateSourcePage("Lis:LisChannels:1", "Lis:LisChannels:ChannelA_COPY", new[] { nameOptionCopy });
        entry1.Label = "ChannelA_COPY";
        entry1.Url = "Lis:LisChannels:ChannelA_COPY";

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0, entry1);

        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page>
        {
            [root.Key] = root,
            [entry0.Key] = entry0,
            [entry1.Key] = entry1
        };

        bool KeyExists(string _) => false;

        var result = service.CreateCopy(populated, entry0.Key, KeyExists, 2);
        Assert.True(result.Success);
        var newPage = result.NewPage!;
        Assert.Equal("ChannelA_COPY2", newPage.Label);
        Assert.EndsWith("/2", newPage.Url);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5315")]
    public void Rebases_descendant_label_paths()
    {
        var service = CreateService();

        var nameOption = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var childOption = CreateNameOption("Lis:LisChannels:0:Child:Name", "Child1", "Child1", isNameColumn: false);
        var nestedPage = CreateSourcePage("Lis:LisChannels:0:Child", "Lis:LisChannels:ChannelA:Child", new[] { childOption });

        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new SettingsChildItem[] { nameOption, nestedPage });
        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);

        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        bool KeyExists(string _) => false;

        var result = service.CreateCopy(populated, entry0.Key, KeyExists, 1);
        Assert.True(result.Success);

        var newPage = result.NewPage!;
        // The nested child should be rebased to the new page’s LabelFromRoot prefix
        var child = newPage.Children.OfType<Page>().Single(p => p.Key.EndsWith(":Child"));
        Assert.StartsWith(newPage.Url + PageHelper.SettingsLabelDeLimiter, child.Url);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5315")]
    public void Extracts_settings_only_for_non_default_non_null_and_non_existing()
    {
        var service = CreateService();

        var optNonDefault = new Option
        {
            Key = "Lis:LisChannels:0:Port",
            Label = "Port",
            Value = "2",
            DefaultValue = "1",
            IsNameColumn = false
        };
        var optDefault = new Option
        {
            Key = "Lis:LisChannels:0:Baud",
            Label = "Baud",
            Value = "9600",
            DefaultValue = "9600",
            IsNameColumn = false
        };
        var optNull = new Option
        {
            Key = "Lis:LisChannels:0:NullOpt",
            Label = "NullOpt",
            Value = null,
            DefaultValue = "X",
            IsNameColumn = false
        };
        var nameOption = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");

        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new SettingsChildItem[] { nameOption, optNonDefault, optDefault, optNull });
        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);
        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });

        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        var existing = new HashSet<string> { "Lis:LisChannels:1:Port" }; // simulate existing for new index 1, Port key
        bool KeyExists(string key) => existing.Contains(key);

        var result = service.CreateCopy(populated, entry0.Key, KeyExists, 1);
        Assert.True(result.Success);

        // Expect settings for non-default, non-null, but skip existing keys
        Assert.All(result.SettingsToPersist, s =>
        {
            Assert.NotNull(s.Value);
            Assert.NotEqual("9600", s.Value); // default skipped
            Assert.NotEqual("Lis:LisChannels:1:Port", s.Key); // existing skipped
        });
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5315")]
    public void Copying_page_with_COPY_label_appends_another_COPY()
    {
        var service = CreateService();

        var originalName = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { originalName });
        entry0.Label = "ChannelA";

        var structPage = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structPage, entry0);
        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        // First copy (from original) -> ChannelA_COPY
        var firstCopy = Copy(service, populated, entry0.Key);
        // Copy the new _COPY page -> ChannelA_COPY_COPY
        var secondCopy = Copy(service, populated, firstCopy.NewPage!.Key);

        Assert.Equal("ChannelA_COPY", firstCopy.NewPage!.Label);
        Assert.Equal("ChannelA_COPY_COPY", secondCopy.NewPage!.Label);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5315")]
    public void Copying_page_with_COPY_COPY_label_appends_third_COPY()
    {
        var service = CreateService();

        var originalName = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { originalName });
        entry0.Label = "ChannelA";

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);
        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        var copy1 = Copy(service, populated, entry0.Key);                 // ChannelA_COPY
        var copy2 = Copy(service, populated, copy1.NewPage!.Key);         // ChannelA_COPY_COPY
        var copy3 = Copy(service, populated, copy2.NewPage!.Key);         // ChannelA_COPY_COPY_COPY

        Assert.Equal("ChannelA_COPY_COPY_COPY", copy3.NewPage!.Label);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5315")]
    public void Copying_numbered_copy_adds_plain_COPY_chain()
    {
        var service = CreateService();

        var originalName = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { originalName });
        entry0.Label = "ChannelA";

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);
        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        _ = Copy(service, populated, entry0.Key);              // ChannelA_COPY
        var copy2 = Copy(service, populated, entry0.Key);      // ChannelA_COPY2

        var copyOfNumbered = Copy(service, populated, copy2.NewPage!.Key);

        Assert.Equal("ChannelA_COPY2", copy2.NewPage!.Label);
        Assert.Equal("ChannelA_COPY2_COPY", copyOfNumbered.NewPage!.Label);
    }

    [Fact]
    public void Numeric_suffix_increments_when_plain_chain_conflicts()
    {
        var service = CreateService();

        var originalName = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { originalName });
        entry0.Label = "ChannelA";

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);
        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        var copy1 = Copy(service, populated, entry0.Key);               // ChannelA_COPY
        _ = Copy(service, populated, entry0.Key);                       // ChannelA_COPY2
        var chain1 = Copy(service, populated, copy1.NewPage!.Key);      // ChannelA_COPY_COPY
        var chain2 = Copy(service, populated, chain1.NewPage!.Key);     // ChannelA_COPY_COPY_COPY
        // Another copy of ChannelA_COPY_COPY (chain1) now that ChannelA_COPY_COPY_COPY exists -> ChannelA_COPY_COPY_COPY2
        var chain3 = Copy(service, populated, chain1.NewPage!.Key);

        Assert.Equal("ChannelA_COPY_COPY", chain1.NewPage!.Label);
        Assert.Equal("ChannelA_COPY_COPY_COPY", chain2.NewPage!.Label);
        Assert.Equal("ChannelA_COPY_COPY_COPY2", chain3.NewPage!.Label);
    }

    [Fact]
    public void Returns_failure_when_max_items_limit_reached()
    {
        var service = CreateService();

        var nameOption = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { nameOption });
        entry0.Label = "ChannelA";

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);
        list.MaxItems = 10; // Set max limit to 10

        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        bool KeyExists(string _) => false;

        // Try to copy when already at max limit (10 items exist)
        var result = service.CreateCopy(populated, entry0.Key, KeyExists, 10);

        Assert.False(result.Success);
        Assert.Equal(CopySettingsErrorCode.MaxItemsReached, result.ErrorCode);
        Assert.Null(result.NewPage);
    }

    [Fact]
    public void Returns_success_when_below_max_items_limit()
    {
        var service = CreateService();

        var nameOption = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { nameOption });
        entry0.Label = "ChannelA";

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);
        list.MaxItems = 10; // Set max limit to 10

        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        bool KeyExists(string _) => false;

        // Copy when below max limit (9 items exist)
        var result = service.CreateCopy(populated, entry0.Key, KeyExists, 9);

        Assert.True(result.Success);
        Assert.NotNull(result.NewPage);
    }

    [Fact]
    public void Returns_success_when_exactly_at_max_limit()
    {
        // Test the off-by-one fix: with max=10 and 9 existing items, copying should succeed
        var service = CreateService();

        var nameOption = CreateNameOption("Lis:LisChannels:0:Name", "ChannelA", "ChannelA");
        var entry0 = CreateSourcePage("Lis:LisChannels:0", "Lis:LisChannels:ChannelA", new[] { nameOption });
        entry0.Label = "ChannelA";

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);
        list.MaxItems = 10; // Set max limit to 10

        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page> { [root.Key] = root, [entry0.Key] = entry0 };

        bool KeyExists(string _) => false;

        // Copy when at 9 items should succeed (result will be 10/10)
        var result = service.CreateCopy(populated, entry0.Key, KeyExists, 9);

        Assert.True(result.Success);
        Assert.NotNull(result.NewPage);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Returns_failure_when_copy_name_exceeds_max_length()
    {
        var service = CreateService();

        // Name is 48 chars, so appending "_COPY" (5 chars) would make 53 > 50
        var longName = new string('A', 48);
        var nameOption = CreateNameOption("Lis:LisChannels:0:Name", longName, longName);
        nameOption.MaxLength = 50;

        var entry0 = CreateSourcePage("Lis:LisChannels:0", $"Lis:LisChannels:{longName}", [nameOption]);
        entry0.Label = longName;

        var structure = CreateSourcePage("Lis:LisChannels:structure", "Lis:LisChannels:structure");
        var list = CreatePageList("Lis:LisChannels", structure, entry0);

        var root = CreateSourcePage("Lis", "Lis", new SettingsChildItem[] { list });
        var populated = new Dictionary<string, Page>
        {
            [root.Key] = root,
            [entry0.Key] = entry0
        };

        var result = Copy(service, populated, entry0.Key);

        Assert.False(result.Success);
        Assert.Equal(CopySettingsErrorCode.NameTooLong, result.ErrorCode);
    }
}
