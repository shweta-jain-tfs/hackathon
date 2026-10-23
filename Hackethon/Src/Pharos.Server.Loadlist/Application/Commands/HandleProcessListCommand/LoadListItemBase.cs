using System.Text.Json.Serialization;

namespace Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;

public abstract class LoadListItemBase
{
    protected LoadListItemBase(bool aggregateIntoArticle)
    {
        AggregateIntoArticle = aggregateIntoArticle;
    }

    public int QuantityInArticle { get; set; }
    public string InstrumentName { get; set; } = string.Empty;
    public ArticleIndicatorShape Shape { get; set; }
    public ArticleType ArticleType { get; set; }
    public string ArticleIdentity { get; set; } = string.Empty;
    public List<string?> Colors { get; set; } = [];
    public string? FriendlyName { get; set; }
    public string? MethodName { get; set; }

    [JsonIgnore]
    public bool AggregateIntoArticle { get; }

    /// <summary>
    /// The quantity of articles that needs to be loaded.
    /// Example: 1 f1 carrier pen or 1 cartridge. 
    /// </summary>
    public int QuantityToLoad { get; private set; }

    [JsonIgnore]
    internal int RequestedQuantityToLoad { get; private set; }

    public void OverrideQuantityToLoad(int quantityToLoad)
    {
        QuantityToLoad = Math.Max(0, quantityToLoad);
    }

    public LoadListStatus Status { get; private set; }

    public int? MinutesLeft { get; private set; }

    public string? Lot { get; private set; }

    [JsonIgnore]
    public Planned Planned { get; } = new()
    {
        Lot = null,
        MissingQuantity = 0,
        StartTime = null
    };

    [JsonIgnore]
    public Trigger Trigger { get; } = new()
    {
        Lot = null,
        RefillLevel = 0,
        TriggerLevel = 0,
        Loaded = null
    };

    public void SetPlanned(DateTimeOffset? startTime, int missingQuantity, string? lotNumber)
    {
        Planned.MissingQuantity = missingQuantity;

        // Only update lot number if we currently does not have one (first lot number wins)
        if (string.IsNullOrEmpty(Planned.Lot))
        {
            Planned.Lot = lotNumber;
        }

        // Only update start time if the new start time is earlier than the current one (first start time wins)
        if (startTime.HasValue && (!Planned.StartTime.HasValue || Planned.StartTime > startTime))
        {
            Planned.StartTime = startTime;
        }

        CalculateWhatToShow();
    }

    public void ResetPlannedQuantity()
    {
        Planned.MissingQuantity = 0;
        Planned.Lot = null;
        Planned.StartTime = null;

        CalculateWhatToShow();
    }

    public void SetTrigger(int refillLevel, int triggerLevel, int? loaded, string? lotNumber = null)
    {
        Trigger.RefillLevel = refillLevel;
        Trigger.TriggerLevel = triggerLevel;
        Trigger.Loaded = loaded;
        Trigger.Lot = lotNumber;

        CalculateWhatToShow();
    }

    private void CalculateWhatToShow()
    {
        var quantityNeededForPlanned = CalculateQuantity(AggregateIntoArticle, QuantityInArticle, Planned.MissingQuantity);
        var quantityNeededForTrigger = CalculateQuantity(AggregateIntoArticle, QuantityInArticle, Trigger);

        RequestedQuantityToLoad = Math.Max(quantityNeededForTrigger, quantityNeededForPlanned);

        if (Planned.MissingQuantity > 0)
        {
            QuantityToLoad = RequestedQuantityToLoad;
            Lot = Planned.Lot;

            var item = GetStatus(Planned.StartTime);
            Status = item.Status;
            MinutesLeft = item.MinutesLeft;
        }
        else
        {
            QuantityToLoad = quantityNeededForTrigger;
            Lot = Trigger.Lot;
            Status = LoadListStatus.RoutineLoading;
            MinutesLeft = null;
        }
    }

    private static int CalculateQuantity(bool aggregateIntoArticle, int articleQuantity, Trigger trigger)
    {
        var loaded = trigger.Loaded ?? 0;

        if (loaded >= trigger.TriggerLevel)
        {
            return 0;
        }

        return CalculateQuantity(aggregateIntoArticle, articleQuantity, trigger.RefillLevel - loaded);
    }

    private static int CalculateQuantity(bool aggregateIntoArticle, int articleQuantity, int missingQuantity)
    {
        if(missingQuantity <= 0)
        {
            return 0;
        }

        if (aggregateIntoArticle)
        {
            return (int)Math.Ceiling((double)missingQuantity / articleQuantity);
        }

        return missingQuantity;
    }

    private static (int? MinutesLeft, LoadListStatus Status) GetStatus(DateTimeOffset? startTime)
    {
        if(!startTime.HasValue)
        {
            return (null, LoadListStatus.RoutineLoading);
        }

        var minutes = startTime.Value.Subtract(DateTimeOffset.Now).TotalMinutes;

        if (minutes < 1)
        {
            return (0, LoadListStatus.Alarm);
        }

        var status = minutes switch
        {
            < 30 => LoadListStatus.Warning,
            _ => LoadListStatus.Pending
        };

        return ((int)minutes, status);
    }
}

public enum LoadListStatus
{
    Alarm,
    Warning,
    Pending,
    RoutineLoading
}

public class Trigger
{
    public int RefillLevel { get; set; }
    public int TriggerLevel { get; set; }
    public int? Loaded { get; set; }

    public string? Lot { get; set; }
}

public class Planned
{
    /// <summary>
    /// The total quantity in respective unit that is missing.
    /// Example: 100 ml stop solution, or 10 doses of the f1 test.
    /// </summary>
    public int MissingQuantity { get; set; }
    public string? Lot { get; set; }
    public DateTimeOffset? StartTime { get; set; }
}