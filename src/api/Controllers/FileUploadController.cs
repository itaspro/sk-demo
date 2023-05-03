using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace app.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly ILogger<FileUploadController> _logger;
    private readonly IHubContext<ProgressHub> _hubContext;
    private readonly IConfiguration _config;

    public FileUploadController(ILogger<FileUploadController> logger, IHubContext<ProgressHub> hubContext, IConfiguration config)
    {
        _logger = logger;
        _hubContext = hubContext;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0 || Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf")
        {
            return BadRequest("Invalid file format. Please upload a PDF file.");
        }

        // Save the file to a temporary location
    //    using (var stream = System.IO.File.Create(tempFilePath))
    //     {
    //         await file.CopyToAsync(stream);
    //     }     string tempFilePath = Path.GetTempFileName();
    
        var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position =0;
        // Start the background job to extract content from the PDF
        _ = Task.Run(async () => await ExtractPdfContent(stream, file.FileName));

         return Ok($"PDF file uploaded successfully. Content extraction is in progress.{file.FileName}");
    }

    private async Task ExtractPdfContent(Stream stream, string fileName)
    {
        // Update progress
        await _hubContext.Clients.All.SendAsync("ProgressUpdate", JsonConvert.SerializeObject(new { Progress = 0, FileName = fileName }));

        try
        {
            var pdf = PdfDocument.Open(stream);
            var result = String.Empty;
            foreach (var page in pdf.GetPages())
            {
                // Either extract based on order in the underlying document with newlines and spaces.
                var text = ContentOrderTextExtractor.GetText(page);
                var progress = (page.Number * 100 / pdf.NumberOfPages);
                await _hubContext.Clients.All.SendAsync("ProgressUpdate", JsonConvert.SerializeObject(new { Progress = progress, FileName = $"p:{page.Number}" }));
                result = $"{result}{text}";
                _logger.LogInformation($"Indexed the PDF content successfully: {result}");
            }

            // Save the extracted text in a file with the same filename and the .ext extension
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error indexing the PDF content: {ex}");
        }

        // Update progress
        await _hubContext.Clients.All.SendAsync("ProgressUpdate", JsonConvert.SerializeObject(new { Progress = 100, FileName = fileName }));
    }
}
