using Microsoft.AspNetCore.Mvc;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using WebApiClient.interfaces;
using System;
using System.IO;

namespace WebApiClient.controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ImageController : ControllerBase // Rename controller to ImageController
    {
        [HttpPost("process")] // Change route to /image/process
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
                var font = fontFamily.CreateFont(10, FontStyle.Regular);

                var text = request.Watermark.Text;

                var textSize = TextMeasurer.MeasureSize(text, new TextOptions(font));
                var textLocation = new PointF(image.Width - textSize.Width - 10, image.Height - textSize.Height - 10); // Adjust padding as needed

                var backgroundRectangle = new RectangularPolygon(textLocation.X - 5, textLocation.Y - 5, textSize.Width + 10, textSize.Height + 10); // Adjust padding as needed
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

                // Return the Base64 string of the watermarked image
                return Ok(new { ProcessedImage = $"data:image/webp;base64,{outputBase64}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
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
}
