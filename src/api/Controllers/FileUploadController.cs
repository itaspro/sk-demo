using Microsoft.AspNetCore.Mvc;

namespace app.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(ILogger<FileUploadController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0 || Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf")
        {
            return BadRequest("Invalid file format. Please upload a PDF file.");
        }

        // Save the file to a temporary location
        string tempFilePath = Path.GetTempFileName();
        using (var stream = System.IO.File.Create(tempFilePath))
        {
            await file.CopyToAsync(stream);
        }

        // Start the background job to extract content from the PDF
        //_ = Task.Run(async () => await ExtractPdfContent(tempFilePath, file.FileName));

         return Ok($"PDF file uploaded successfully. Content extraction is in progress.{file.FileName}");
    }
}
