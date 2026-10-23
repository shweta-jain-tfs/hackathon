using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

public class ColorValueConverter()
    : ValueConverter<Color?, string?>(
        color => color == null ? null : ColorTranslator.ToHtml(color.Value),
        colorString => colorString == null ? null : ColorTranslator.FromHtml(colorString))
{
    // Convert Color to string
    // Convert string to Color
}