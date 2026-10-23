using Pharos.Client.App.InstrumentManagement.Views.Dashboard;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.InstrumentManagement.Models.Dashboard
{
    internal sealed class InstrumentStateDataSource(HttpClient httpClient)
    {
        public event Action<bool>? OnControlInstrumentStateCompleted;

        public async Task AssayAbortAsync()
        {
            var client = new CurrentInstrumentClient(httpClient);
            try
            {
                await client.AbortAssayAsync();
                OnControlInstrumentStateCompleted?.Invoke(true);
            }
            catch
            {
                OnControlInstrumentStateCompleted?.Invoke(false);
                throw;
            }
        }

        public async Task AssayEndAsync(EndAssayOptions endAssayOptions)
        {
            var client = new CurrentInstrumentClient(httpClient);
            try
            {
                await client.EndAssayAsync(Map(endAssayOptions));

                OnControlInstrumentStateCompleted?.Invoke(true);
            }
            catch
            {
                OnControlInstrumentStateCompleted?.Invoke(false);
                throw;
            }
        }

        private static EndAssayFinishOptions Map(EndAssayOptions endAssayOptions)
        {
            return endAssayOptions switch
            {
                EndAssayOptions.LoadedSamplesAreProcessed => EndAssayFinishOptions.FinishAllSamplesScheduled,
                EndAssayOptions.StartedTestsAreProcessed => EndAssayFinishOptions.FinishAllDispensedTests,
                _ => throw new ArgumentOutOfRangeException(nameof(endAssayOptions), endAssayOptions, null)
            };
        }

        public async Task AssayStartAsync()
        {
            var client = new CurrentInstrumentClient(httpClient);
            try
            {
                await client.StartAssayAsync();

                OnControlInstrumentStateCompleted?.Invoke(true);
            }
            catch

            {
                OnControlInstrumentStateCompleted?.Invoke(false);
                throw;
            }
        }

        public async Task AssayStartCmrAsync()
        {
            var client = new CurrentInstrumentClient(httpClient);
            try
            {
                await client.StartCmrAssayAsync();

                OnControlInstrumentStateCompleted?.Invoke(true);
            }
            catch
            {
                OnControlInstrumentStateCompleted?.Invoke(false);
                throw;
            }
        }

        public async Task<bool> PowerSaveAsync()
        {
            var client = new CurrentInstrumentClient(httpClient);
            try
            {
                await client.PowerSaveAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task ControlLightAsync()
        {
            var client = new CurrentInstrumentClient(httpClient);
            await client.ControlLightAsync();
        }

        public async Task PowerOffAsync()
        {
            var client = new CurrentInstrumentClient(httpClient);
            try
            {
                await client.PowerOffAsync();

                OnControlInstrumentStateCompleted?.Invoke(true);
            }
            catch
            {
                OnControlInstrumentStateCompleted?.Invoke(false);
                throw;
            }
        }

        public async Task StandbyAsync()
        {
            var client = new CurrentInstrumentClient(httpClient);
            try
            {
                await client.StandbyAsync();

                OnControlInstrumentStateCompleted?.Invoke(true);
            }
            catch
            {
                OnControlInstrumentStateCompleted?.Invoke(false);
                throw;
            }
        }

        public async Task UnlockHoodAsync()
        {
            var client = new CurrentInstrumentClient(httpClient);
            try
            {
                await client.UnlockHoodAsync();

                OnControlInstrumentStateCompleted?.Invoke(true);
            }
            catch
            {
                OnControlInstrumentStateCompleted?.Invoke(false);
                throw;
            }
        }
    }
}
