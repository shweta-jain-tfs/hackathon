namespace Pharos.Client.App.Parameters.Packages.ViewModels;

public sealed class PackageDetailsViewModel(IEntityDataService<PackageDetails> dataService) : EntityEditorViewModel<PackageDetails>(dataService);
