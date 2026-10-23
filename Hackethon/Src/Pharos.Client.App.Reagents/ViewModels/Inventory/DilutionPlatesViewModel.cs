using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.ViewModels.Inventory
{
    public class DilutionPlatesViewModel
    {
        private readonly InstrumentFilterViewModel _instrumentFilterViewModel;

        public DrawerDilutionPlatesViewModel DrawerDilutionPlatesListViewModel { get; }
        public InternalDilutionPlatesViewModel InternalDilutionPlatesListViewModel { get; }

        public DilutionPlatesViewModel(DrawerDilutionPlatesViewModel drawerDilutionPlatesListViewModel,
            InternalDilutionPlatesViewModel internalDilutionPlatesListViewModel,
            InstrumentFilterViewModel instrumentFilterViewModel)
        {
            DrawerDilutionPlatesListViewModel = drawerDilutionPlatesListViewModel;
            InternalDilutionPlatesListViewModel = internalDilutionPlatesListViewModel;
            _instrumentFilterViewModel = instrumentFilterViewModel;

            // Subscribe to child event
            DrawerDilutionPlatesListViewModel.InstrumentFilterChanged += OnInstrumentFilterChanged;
        }

        /// <summary>
        /// Initializes the instrument filter and proactively applies the default filter
        /// to ensure filter propagation works properly from the start
        /// </summary>
        public async Task InitializeAsync()
        {
            // Load the instrument filter view model first to get the default filter
            await _instrumentFilterViewModel.LoadAsync();
            
            // Apply the default instrument filter proactively before grids start loading
            if (!string.IsNullOrEmpty(_instrumentFilterViewModel.DefaultFilterValueDisplayName))
            {
                var defaultFilter = new DataSourceFilter(
                    nameof(InternalDilutionPlate.Instrument),
                    FilterOperator.EqualsTo,
                    _instrumentFilterViewModel.DefaultFilterValueDisplayName);
                
                InternalDilutionPlatesListViewModel.DrawerDilutionPlatesFilters = new List<DataSourceFilter> { defaultFilter };
            }
        }

        private void OnInstrumentFilterChanged(List<DataSourceFilter> selectedInstruments)
        {
            InternalDilutionPlatesListViewModel.DrawerDilutionPlatesFilters = selectedInstruments;
        }
    }
}
