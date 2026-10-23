namespace Pharos.Server.Reagents.Application.Queries.Inventory
{

    public class DrawerDilutionPlatesResultDto
    {
        public int Id { get; set; }
        public int PlatesCount { get; set; }
        public int WellsCount { get; set; }
        public List<DrawerDilutionPlateDto> Items { get; set; } = default!;
    }

    public class InternalDilutionPlatesResultDto
    {
        public int Id { get; set; }
        public int PlatesCount { get; set; }
        public int FreeWellsCount { get; set; }
        public List<InternalDilutionPlateDto> Items { get; set; } = default!;
    }   

    public class DrawerDilutionPlateDto
    {
        public int Id { get; set; }
        public int DrawerPosition { get; set; }
        public int DilutionPlates { get; set; }
        public ReagentPositionStatus Status { get; set; }
        public string Instrument { get; set; } = string.Empty;
    }

    public class InternalDilutionPlateDto
    {
        public int Id { get; set; }
        public int FreeWells { get; set; }
        public string Instrument { get; set; } = string.Empty;
        public string InstrumentNumber { get; set; } = string.Empty;
        public InternalDilutionPlatePosition Position { get; set; }
    }

    public enum InternalDilutionPlatePosition
    {
        Left = 1,
        Right = 2
    }
}