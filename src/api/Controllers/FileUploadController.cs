using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Text;
using Newtonsoft.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace app.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly ILogger<FileUploadController> logger;
    private readonly IHubContext<MessageHub> hubContext;
    private readonly IConfiguration config;
    private readonly IKernel kernel;
    private readonly int DocumentLineSplitMaxTokens;
    private readonly int DocumentParagraphSplitMaxLines;

    public FileUploadController(ILogger<FileUploadController> logger, IHubContext<MessageHub> hubContext, IConfiguration config, IKernel kernel)
    {
        this.logger = logger;
        this.hubContext = hubContext;
        this.config = config;
        this.kernel = kernel;
        DocumentLineSplitMaxTokens = config.GetValue<int>("DocumentMemory:DocumentLineSplitMaxTokens" );
        DocumentParagraphSplitMaxLines = config.GetValue<int>("DocumentMemory:DocumentParagraphSplitMaxLines");
    }

    [HttpPost("{clientID}")]
    public async Task<IActionResult> Upload(IFormFile file, string clientID)
    {
        if (file == null || file.Length == 0 || Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf")
        {
            return BadRequest("Invalid file format. Please upload a PDF file.");
        }
    
        var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position =0;
        // Start the background job to extract content from the PDF
        _ = Task.Run(async () => await MemorizeContent(stream, file.FileName, clientID));

         return Ok($"PDF file uploaded successfully. Content processing is in progress.{file.FileName}");
    }

    private async Task MemorizeContent(Stream stream, string fileName, string clientID, string topic="global")
    {
        // Update progress
        await hubContext.Clients.Client(clientID).SendAsync("Progress", JsonConvert.SerializeObject(new { Progress = 0, Message = $"extracting content from {fileName }" }));

        try
        {
            var pdf = PdfDocument.Open(stream);
            var result = string.Empty;
            foreach (var page in pdf.GetPages())
            {
                // Either extract based on order in the underlying document with newlines and spaces.
                var text = ContentOrderTextExtractor.GetText(page);
                var progress = (page.Number * 100 / pdf.NumberOfPages);
                await hubContext.Clients.All.SendAsync("Progress", JsonConvert.SerializeObject(new { Progress = progress, Message = $"page:{page.Number}" }));
                result = $"{result}{text}";
                logger.LogDebug($"Indexed the PDF content successfully: {result}");
            }
            // Split the document into lines of text and then combine them into paragraphs.
            var lines = TextChunker.SplitPlainTextLines(result, DocumentLineSplitMaxTokens);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, DocumentParagraphSplitMaxLines);
            await hubContext.Clients.Client(clientID).SendAsync("Progress", JsonConvert.SerializeObject(new { Progress = 0, Message = "Process text with Semantic Kernel embedding & memory" }));
            int i = 0;
            foreach (var paragraph in paragraphs)
            {
                i++;
                await Task.Delay(500);
                var recordID = await kernel.Memory.SaveInformationAsync(
                    collection: clientID,
                    text: paragraph,
                    id: Guid.NewGuid().ToString(),
                    description: $"Document: {fileName}",
                    additionalMetadata: topic);
                logger.LogDebug($"record: {recordID}");
                await hubContext.Clients.Client(clientID).SendAsync("Process", JsonConvert.SerializeObject(new { Progress = (i * 100 / (paragraphs.Count+1)), Message= "Process text with Semantic Kernel embedding & memory" }));
            }
            logger.LogDebug($"Document content memorized successfully: {result}");
            await hubContext.Clients.Client(clientID).SendAsync("Complete", JsonConvert.SerializeObject(new { Progress = 100, Message = "PDF processed" }));
        }
        catch (Exception ex)
        {
            logger.LogError($"Error indexing the PDF content: {ex}");
            await hubContext.Clients.Client(clientID).SendAsync("Error", JsonConvert.SerializeObject(new { Message=$"{ex.Message}" }));
        }
    }
}
