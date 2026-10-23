using Riok.Mapperly.Abstractions;

// Version 5.0 of Mapperly will have support for MSBuild API for mapper defaults.
// When this version is released, we can remove this file and use Directory.build.props instead.
[assembly: MapperDefaults(
    RequiredMappingStrategy = RequiredMappingStrategy.Target,
    EnumMappingStrategy = EnumMappingStrategy.ByName,
    RequiredEnumMappingStrategy = RequiredMappingStrategy.Both,
    EnumMappingIgnoreCase = true,
    UseDeepCloning = true,
    ThrowOnMappingNullMismatch = true,
    ThrowOnPropertyMappingNullMismatch = false)]