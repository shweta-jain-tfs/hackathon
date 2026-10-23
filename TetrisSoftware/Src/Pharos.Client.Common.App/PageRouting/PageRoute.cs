namespace Pharos.Client.Common.App.PageRouting;

public static class PageRoute
{
    // Root routes (alphabetical).
    public const string About = RouteItemBase.About;
    public const string Bottle = RouteItemBase.Bottle;
    public const string Dashboard = RouteItemBase.Dashboard;
    public const string DashboardConnections = $"{RouteItemBase.Dashboard}/connections";
    public const string DashboardHumidity = $"{RouteItemBase.Dashboard}/humidity";
    public const string DashboardTemperatures = $"{RouteItemBase.Dashboard}/temperatures";
    public const string Help = RouteItemBase.Help;
    public const string Instruments = RouteItemBase.Instruments;
    public const string InstrumentsAssayScheduler = $"{RouteItemBase.Instruments}/assayscheduler";
    public const string Inventory = RouteItemBase.Inventory;
    public const string Liquid = RouteItemBase.Liquid;
    public const string LoadList = RouteItemBase.LoadList;
    public const string LockScreen = RouteItemBase.LockScreen;
    public const string Logout = RouteItemBase.Logout;
    public const string Logs = RouteItemBase.Logs;
    public const string Maintenance = RouteItemBase.Maintenance;
    public const string Reagent = RouteItemBase.Reagent;
    public const string Requests = RouteItemBase.Requests;
    public const string Results = RouteItemBase.Results;
    public const string Samples = RouteItemBase.Samples;
    public const string Settings = RouteItemBase.Settings;
    public const string SettingsUi = RouteItemBase.SettingsUi;
    public const string SolidWaste = RouteItemBase.SolidWaste;
    public const string SubSettings = $"{Settings}" + "/{*Path}";
    public const string Tasks = RouteItemBase.Tasks;
    public const string Tools = RouteItemBase.Tools;
    public const string UserAccount = "useraccount";

    // Grouped routes (alphabetical).
    public static class AnalyticalRunDetails
    {
        public const string Default = $"{RouteItemBase.Results}/{RouteItemBase.Details}";
        public const string IdParameter = $"{Default}/{_idParam}";

        private const string _idParam = "{id:int?}";
    }

    public static class CalibratorDetails
    {
        public const string Default = $"{RouteItemBase.Results}/{RouteItemBase.Calibrator}/{RouteItemBase.Details}";
        public const string IdParameter = $"{Default}/{_idParam}";

        private const string _idParam = "{id:int?}";
    }

    public static class CurveControlDetails
    {
        public const string Default = $"{RouteItemBase.Results}/{RouteItemBase.CurveControl}";
        public const string IdParameter = $"{Default}/{_idParam}";

        private const string _idParam = "{id:int?}";
    }

    public static class Parameters
    {
        public const string Default = RouteItemBase.Parameters;
        public const string Configuration = $"{Default}/{RouteItemBase.Configuration}";
        public const string LoadListLevels = $"{Default}/{RouteItemBase.LoadListLevels}";
        public const string Methods = $"{Default}/{RouteItemBase.Methods}";
        public const string Packages = $"{Default}/{RouteItemBase.Packages}";
        public const string QualityControls = $"{Default}/{RouteItemBase.QualityControl}";
        public const string Reflex = $"{Default}/{RouteItemBase.Reflex}";
        public const string Requestors = $"{Default}/{RouteItemBase.Requestors}";
        public const string TestPanels = $"{Default}/{RouteItemBase.TestPanels}";

        public static class LabQualityControlDetails
        {
            public const string Default = $"{Parameters.Default}/{RouteItemBase.LabQualityControl}/{RouteItemBase.Details}";
            public const string Create = $"{Parameters.Default}/{RouteItemBase.LabQualityControl}/{RouteItemBase.Create}";
            public const string IdParameter = $"{Default}/{_idParam}";

            private const string _idParam = "{id:int?}";

            public static string DetailsPage(int id)
            {
                return $"{Default}/{id}";
            }
        }

        public static class MethodDetails
        {
            public const string Default = $"{Parameters.Default}/{RouteItemBase.Methods}/{RouteItemBase.Details}";
            public const string IdParameter = $"{Default}/{_idParam}";

            private const string _idParam = "{id:int?}";

            public static string DetailsPage(int id)
            {
                return $"{Default}/{id}";
            }
        }

        public static class PackageDetails
        {
            public const string Default = $"{Parameters.Default}/{RouteItemBase.Packages}/{RouteItemBase.Details}";
            public const string IdParameter = $"{Default}/{_idParam}";

            private const string _idParam = "{id:int?}";
        }

        public static class PhadiaQualityControlDetails
        {
            public const string Default = $"{Parameters.Default}/{RouteItemBase.PhadiaQualityControl}/{RouteItemBase.Details}";
            public const string IdParameter = $"{Default}/{_idParam}";

            private const string _idParam = "{id:int?}";

            public static string DetailsPage(int id)
            {
                return $"{Default}/{id}";
            }
        }

        public static class ReflexDefinitionDetails
        {
            public const string Default = $"{Parameters.Default}/{RouteItemBase.Reflex}/{RouteItemBase.Details}";
            public const string IdParameter = $"{Default}/{_idParam}";

            private const string _idParam = "{id:int?}";

            public static string DetailsPage(int id)
            {
                return $"{Default}/{id}";
            }
        }

        public static class RequestorDetails
        {
            public const string Default = $"{Parameters.Default}/{RouteItemBase.Requestors}/{RouteItemBase.Details}";
            public const string IdParameter = $"{Default}/{_idParam}";

            private const string _idParam = "{id:int?}";

            public static string DetailsPage(int id)
            {
                return $"{Default}/{id}";
            }
        }

        public static class TestDetails
        {
            public const string Default = $"{Parameters.Default}/{RouteItemBase.Methods}/{RouteItemBase.Tests}/{RouteItemBase.Details}";
            public const string IdParameter = $"{Default}/{_idParam}";

            private const string _idParam = "{id:int?}";

            public static string DetailsPage(int id)
            {
                return $"{Default}/{id}";
            }
        }

        public static class TestPanelDetails
        {
            public const string Default = $"{Parameters.Default}/{RouteItemBase.TestPanels}/{RouteItemBase.Details}";
            public const string IdParameter = $"{Default}/{_idParam}";

            private const string _idParam = "{id:int?}";

            public static string DetailsPage(int id)
            {
                return $"{Default}/{id}";
            }
        }
    }

    public static class Quality
    {
        public const string Default = RouteItemBase.Quality;
        public const string Calibrators = $"{Default}/{_instrumentNumberParam}/{_methodNameParam}/{RouteItemBase.Calibrator}";
        public const string CurveControls = $"{Default}/{_instrumentNumberParam}/{_methodNameParam}/{RouteItemBase.CurveControl}";
        public const string Method = $"{Default}/{_instrumentNumberParam}/{_methodNameParam}";

        private const string _instrumentNumberParam = "{instrumentNumber}";
        private const string _methodNameParam = "{methodName}";

        public static class MethodQualityControl
        {
            public const string Default = $"{RouteItemBase.Quality}/{RouteItemBase.QualityControl}";
            public const string IdQualityControl = $"{Default}/{_idQualityControl}";

            private const string _idQualityControl = "{id:int?}";
        }
    }

    public static class QualityControlDetails
    {
        public const string Default = $"{RouteItemBase.Results}/{RouteItemBase.QualityControl}";
        public const string IdParameter = $"{Default}/{_idParam}/{_qcIdParam}/{_testParam}";

        private const string _idParam = "{id:int?}";
        private const string _qcIdParam = "{qcName?}";
        private const string _testParam = "{testName?}";
    }

    public static class SampleDetails
    {
        public const string Default = $"{RouteItemBase.Requests}/{RouteItemBase.Details}";
        public const string IdParameter = $"{Default}/{_idParam}";

        private const string _idParam = "{id:int?}";

        public static string DetailsPage(int id)
        {
            return $"{Default}/{id}";
        }
    }

    public static class Search
    {
        public const string Default = RouteItemBase.Search;
        public const string SearchParameter = $"{Default}/{_searchParam}";

        private const string _searchParam = "{search?}";
    }

    public static class Trays
    {
        public const string Default = RouteItemBase.Trays;

        public static string SelectedPosition(string trayId, int position)
        {
            return $"{SelectedTray(trayId)}&selectedPosition={position}";
        }

        public static string SelectedTray(string trayId)
        {
            return $"{Default}?trayId={trayId}";
        }
    }

    public static class Reagents
    {
        public const string Default = $"{RouteGroupBase.Reagents}";
        public const string Consumption = $"{RouteGroupBase.Reagents}/{RouteItemBase.Consumption}";
        public const string Inventory = $"{RouteGroupBase.Reagents}/{RouteItemBase.Inventory}";
        public const string Stock = $"{RouteGroupBase.Reagents}/{RouteItemBase.Stock}";
    }

    public static class UsersAndRoles
    {
        public const string Default = RouteItemBase.UsersAndRoles;
        public const string Roles = $"{Default}/{RouteItemBase.Roles}";
        public const string Users = $"{Default}/{RouteItemBase.Users}";

        public static class RoleDetails
        {
            public const string Default = $"{UsersAndRoles.Default}/{RouteItemBase.Roles}/{RouteItemBase.Details}";
            public const string IdRole = $"{Default}/{_idRole}";

            private const string _idRole = "{id:guid?}";

            public static string DetailsPage(Guid id)
            {
                return $"{Default}/{id}";
            }
        }

        public static class UserDetails
        {
            public const string Default = $"{UsersAndRoles.Default}/{RouteItemBase.Users}/{RouteItemBase.Details}";
            public const string IdUser = $"{Default}/{_idUser}";

            private const string _idUser = "{id:guid?}";

            public static string DetailsPage(Guid id)
            {
                return $"{Default}/{id}";
            }
        }
    }
}
