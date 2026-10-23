using Pharos.Client.App.Request.Models;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class ImageView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public string InstrumentNumber { get; set; } = default!;
    [Parameter] public string ImageId { get; set; } = default!;
    [Inject] ISampleTubeDataSource SampleTubeDataSource { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private bool _showTubeId;
    private string _tubeId = string.Empty;
    private DateTimeOffset _time;
    private byte[] _imageData = Array.Empty<byte>();

    private bool _imageFound;
    private bool _loadingImage;

    private async Task CloseModal()
    {
        await Modal.CloseAsync(ModalResult.Cancel());
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadImage(InstrumentNumber, ImageId);
    }

    public async Task LoadImage(string instrumentNumber, string imageId)
    {
        _loadingImage = true;

        var image = await SampleTubeDataSource.GetSampleTubeImage(instrumentNumber, imageId);

        _loadingImage = false;

        if (image.ImageFound)
        {
            _imageFound = true;
            _showTubeId = !string.IsNullOrEmpty(image.TubeId);
            _tubeId = image.TubeId;
            _time = image.Time;
            _imageData = image.ImageData;
        }
        else
        {
            _imageFound = false;
        }
    }
}