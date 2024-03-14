using Microsoft.AspNetCore.Mvc;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using WebApiClient.interfaces;

namespace WebApiClient.controllers;

[Route("[controller]")]
[ApiController]
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;

    public ImageController(ILogger<ImageController> logger)
    {
        _logger = logger;
    }

    [HttpPost("process")]
    public IActionResult AddWatermark([FromBody] WatermarkApiSchema.WatermarkRequest request)
    {
        try
        {
            // Convert input Base64 string to byte array
            var inputBytes = Convert.FromBase64String(request.SourceImage.Split(",")[1]);

            // Load the input image from byte array
            using var image = Image.Load(inputBytes);

            // Define the font and text options for your watermark
            var fonts = new FontCollection();
            var fontFamily = fonts.Add("font/PressStart2P-Regular.ttf");
            var font = fontFamily.CreateFont(7, FontStyle.Regular);

            var text = request.Watermark.Text;

            var textSize = TextMeasurer.MeasureSize(text, new TextOptions(font));
            var textLocation = new PointF(image.Width - textSize.Width - 4, image.Height - textSize.Height - 4);

            var backgroundRectangle = new RectangularPolygon(textLocation.X - 2, textLocation.Y - 2, textSize.Width + 4, textSize.Height + 4);
            image.Mutate(x => x.Fill(Color.White.WithAlpha(0.6f), backgroundRectangle));

            // Apply the watermark
            image.Mutate(x =>
                x.DrawText(
                    text,
                    font,
                    Color.Black.WithAlpha(0.6f),
                    textLocation
                )
            );

            // Convert the watermarked image to Base64 string
            var outputBase64 = ConvertToBase64(image);

            var logDetails = new WatermarkApiSchema.LogDetails
            {
                Timestamp = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}",
                RequestMethod = "POST",
                RequestBody = request,
                RequestUrl = "/image/process",
                StatusCode = 200,
                Response = outputBase64
            };

            _logger.LogInformation("Image processed successfully. {@LogDetails}", logDetails);

            return Ok(new { ProcessedImage = $"data:image/webp;base64,{outputBase64}" });
        }
        catch (Exception ex)
        {
            var logDetails = new WatermarkApiSchema.LogDetails
            {
                Timestamp = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}",
                RequestMethod = "POST",
                RequestBody = request,
                RequestUrl = "/image/process",
                StatusCode = 500,
                Response = ex.Message
            };
            _logger.LogError("An error occurred while processing the image. {@LogDetails}", logDetails);
            var errorResponse = new
            {
                error = ex.Message
            };
            return StatusCode(500, errorResponse);
        }
    }

    // Convert the image to Base64 string
    private static string ConvertToBase64(Image image)
    {
        using var memoryStream = new MemoryStream();
        image.Save(memoryStream, SixLabors.ImageSharp.Formats.Webp.WebpFormat.Instance);
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}