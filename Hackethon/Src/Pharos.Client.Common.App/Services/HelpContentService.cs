using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pharos.Client.Common.App.HelpContent;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models.HelpContent;

namespace Pharos.Client.Common.App.Services;

public class HelpContentService : IHelpContentService
{
    private readonly HttpClient _httpClient;

    public HelpContentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private const string _defaultCulture = "en-us";
    private const string _defaultPath = "_content/Pharos.Client.App.Help/manuals";
    private const string _helpManual = "HelpManual";
    private const string _helpManualSearchIndex = "HelpManualSearchIndex";
    private const string _helpTaskCards = "HelpTaskCards";
    private const string _helpMaintenance = "HelpMaintenance";
    private readonly string _culture = CultureInfo.CurrentUICulture.Name.ToLower();
    private HelpContentManual? _manual;
    private Dictionary<string, List<ChapterSearchIndex>>? _searchIndex;
    private HashSet<HelpContentTaskCard>? _taskCards;
    private HashSet<HelpContentTaskCard>? _defaultTaskCards;

    private static readonly JsonSerializerOptions _cachedJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public async Task<HelpContentManual> GetManual()
    {
        if (_manual is not null)
        {
            return _manual;
        }

        _manual = await LoadManual(_culture);
        if (_manual is null)
        {
            _manual = await LoadManual(_defaultCulture);

            if (_manual is null)
            {
                throw new InvalidOperationException("No manual found for your selected language.");
            }
        }

        return _manual;
    }

    public async Task<Dictionary<string, List<ChapterSearchIndex>>> GetManualSearchIndex()
    {
        if (_searchIndex is not null)
        {
            return _searchIndex;
        }

        _searchIndex = await LoadManualSearchIndex(_culture);
        if (_searchIndex is null)
        {
            _searchIndex = await LoadManualSearchIndex(_defaultCulture);

            if (_searchIndex is null)
            {
                throw new InvalidOperationException("No manual search index found for your selected language.");
            }
        }

        return _searchIndex;
    }

    public async Task<Dictionary<string, (string rootChapterId, List<string> path)>> GetChapterStructure()
    {
        _manual ??= await GetManual();

        // Map each chapter to its parent (null for root)
        var parentMap = new Dictionary<string, string?>();
        foreach (var root in _manual.Chapters)
        {
            BuildParentMap(root, null);
        }

        // Now build the result dictionary
        var result = new Dictionary<string, (string rootChapterId, List<string> path)>();
        foreach (var chapterId in parentMap.Keys)
        {
            var path = new List<string>();
            var current = chapterId;
            var rootId = chapterId;
            while (!string.IsNullOrEmpty(current))
            {
                path.Add(current);
                rootId = current;
                current = parentMap[current];
            }

            path.Reverse(); // Now path is from root to chapterId
            result[chapterId] = (rootId, path);
        }

        return result;

        void BuildParentMap(Chapter chapter, string? parentId)
        {
            if (string.IsNullOrEmpty(chapter.Id))
            {
                throw new InvalidOperationException($"Chapter has a null or empty Id. Title: '{chapter.Title}'. Ensure all chapters in the JSON have an 'id' field with a non-empty value.");
            }

            parentMap[chapter.Id] = parentId;
            if (chapter.Chapters is { Count: > 0 })
            {
                foreach (var child in chapter.Chapters)
                {
                    BuildParentMap(child, chapter.Id);
                }
            }
        }
    }

    public async Task<List<HelpContentMaintenanceActivity>> GetMaintenanceActivities()
    {
        var activities = await LoadMaintenanceActivity(_culture);
        if (activities is null || activities.Count == 0)
        {
            activities = await LoadMaintenanceActivity(_defaultCulture);
        }

        if (activities is null || activities.Count == 0)
        {
            throw new InvalidOperationException("No maintenance activity found for your language");
        }

        return activities.ToList();
    }

    public async Task<HelpContentTaskCard> GetTaskCardHelp(int id)
    {
        _taskCards ??= await LoadTaskCards(_culture);

        var taskCard = _taskCards?.FirstOrDefault(x => x.Id == id);

        if (taskCard is null)
        {
            _defaultTaskCards ??= await LoadTaskCards(_defaultCulture);
            taskCard = _defaultTaskCards?.FirstOrDefault(x => x.Id == id);
        }

        return taskCard ?? throw new InvalidOperationException("No Task Card found for your selected language.");
    }

    private async Task<HelpContentManual?> LoadManual(string lang)
    {
        try
        {
            var jsonContent = await LoadJsonContent(_helpManual, lang);

            return string.IsNullOrWhiteSpace(jsonContent) ? null : ConvertToManual(jsonContent);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (DecoderFallbackException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    private async Task<Dictionary<string, List<ChapterSearchIndex>>?> LoadManualSearchIndex(string lang)
    {
        try
        {
            var jsonContent = await LoadJsonContent(_helpManualSearchIndex, lang);

            return string.IsNullOrWhiteSpace(jsonContent) ? null : ConvertToManualSearchIndex(jsonContent);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (DecoderFallbackException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    private static HelpContentManual ConvertToManual(string json)
    {
        try
        {
            var output = JsonSerializer.Deserialize<HelpContentManual>(json, _cachedJsonSerializerOptions)
                         ?? throw new InvalidOperationException("Failed to deserialize HelpContentManual from JSON.");

            foreach (var chapter in output.Chapters)
            {
                RewriteChapterUrls(chapter);
            }

            return output;
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Failed to deserialize HelpContentManual from JSON.");
        }
    }

    private static void RewriteChapterUrls(Chapter chapter)
    {
        if (chapter.Content is not null)
        {
            chapter.Content = HelpContentUrlRewriter.RewriteUrls(chapter.Content);
        }

        if (chapter.Chapters is { Count: > 0 })
        {
            foreach (var child in chapter.Chapters)
            {
                RewriteChapterUrls(child);
            }
        }
    }

    private static Dictionary<string, List<ChapterSearchIndex>> ConvertToManualSearchIndex(string json)
    {
        try
        {
            var output = JsonSerializer.Deserialize<UserManualSearchIndex>(json, _cachedJsonSerializerOptions)
                         ?? throw new InvalidOperationException("Failed to deserialize UserManualSearchIndex from JSON.");
            return output.SearchIndex;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize {nameof(ChapterSearchIndex)} collection from JSON.", ex);
        }
    }

    private async Task<HashSet<HelpContentTaskCard>?> LoadTaskCards(string lang)
    {
        try
        {
            var jsonContent = await LoadJsonContent(_helpTaskCards, lang);

            return string.IsNullOrWhiteSpace(jsonContent) ? null : ConvertToTaskCardHelp(jsonContent);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (DecoderFallbackException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static HashSet<HelpContentTaskCard> ConvertToTaskCardHelp(string json)
    {
        try
        {
            var output = JsonSerializer.Deserialize<HashSet<HelpContentTaskCard>>(json, _cachedJsonSerializerOptions)
                         ?? throw new InvalidOperationException("Task-card JSON deserialized to null.");

            foreach (var taskCard in output)
            {
                taskCard.Content = HelpContentUrlRewriter.RewriteUrls(taskCard.Content);
            }

            return output;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize {nameof(HelpContentTaskCard)} collection from JSON.", ex);
        }
    }

    private async Task<HashSet<HelpContentMaintenanceActivity>?> LoadMaintenanceActivity(string lang)
    {
        try
        {
            var jsonContent = await LoadJsonContent(_helpMaintenance, lang);

            return string.IsNullOrWhiteSpace(jsonContent) ? null : ConvertToMaintenanceActivity(jsonContent);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (DecoderFallbackException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static HashSet<HelpContentMaintenanceActivity> ConvertToMaintenanceActivity(string json)
    {
        var output = JsonSerializer.Deserialize<HashSet<HelpContentMaintenanceActivity>>(json, _cachedJsonSerializerOptions)
                     ?? throw new InvalidOperationException("Failed to deserialize HelpContentMaintenanceActivity from JSON.");
        return output;
    }

    private async Task<string?> LoadJsonContent(string fileName, string lang)
    {
        var uri = new Uri($"{_defaultPath}/{fileName}.{lang}.json", UriKind.Relative);
        var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        // Remove UTF-8 BOM if present
        var cleanBytes = RemoveUtf8Bom(bytes);

        EnsureValidUtf8(cleanBytes, fileName, lang);

        return _strictUtf8.GetString(cleanBytes);
    }

    private static void EnsureValidUtf8(byte[] bytes, string fileName, string lang)
    {
        try
        {
            _strictUtf8.GetCharCount(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new DecoderFallbackException($"The content of {fileName}.{lang}.json is not valid UTF-8. Please ensure the file is saved with UTF-8 encoding.");
        }
    }

    /// <summary>
    /// Removes UTF-8 BOM (Byte Order Mark) from the beginning of a byte array if present.
    /// UTF-8 BOM is the byte sequence: 0xEF 0xBB 0xBF
    /// </summary>
    /// <param name="bytes">The byte array to process</param>
    /// <returns>A new byte array without the BOM, or the original array if no BOM was found</returns>
    private static byte[] RemoveUtf8Bom(byte[] bytes)
    {
        // Check if the array starts with UTF-8 BOM: 0xEF 0xBB 0xBF
        if (bytes.Length >= 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF)
        {
            // Create a new array without the BOM
            var cleanBytes = new byte[bytes.Length - 3];
            Array.Copy(bytes, 3, cleanBytes, 0, cleanBytes.Length);
            return cleanBytes;
        }

        return bytes;
    }
}