using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;
using URF.Core.Helper.Helpers;
using URF.Core.Services;
using PosApp.Admin.Api.Helpers;
using URF.Core.EF.Trackable;
using Amazon.S3;
using Amazon.S3.Transfer;
using PosApp.Admin.Api.Data.Models;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Fonts;
using Tesseract;

namespace PosApp.Admin.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UploadController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly HttpClientEx httpClientEx = new HttpClientEx();

        public UploadController(
            IHttpContextAccessor httpContextAccessor,
            Microsoft.Extensions.Options.IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("UploadFile")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFileAsync(IFormCollection data)
        {
            try
            {
                if (_appSettings.AmazonSecret.IsStringNullOrEmpty())
                    return UploadFile(data.Files[0], "files");
                else
                    return await UploadFileToS3(data.Files[0]);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex);
            }
        }

        [HttpPost("UploadImage")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadImageAsync(IFormCollection data)
        {
            try
            {
                if (_appSettings.AmazonSecret.IsStringNullOrEmpty())
                    return UploadFile(data.Files[0], "images");
                else
                    return await UploadFileToS3(data.Files[0]);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex);
            }
        }

        [DisableRequestSizeLimit]
        [HttpPost("UploadAvatar")]
        public async Task<IActionResult> UploadAvatarAsync(IFormCollection data)
        {
            try
            {
                if (_appSettings.AmazonSecret.IsStringNullOrEmpty())
                    return UploadFile(data.Files[0], "avatars");
                else
                    return await UploadFileToS3(data.Files[0]);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex);
            }
        }

        [HttpPost("UploadSignature")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadSignatureAsync(IFormCollection data)
        {
            try
            {
                return await UploadAndRemoveBgToS3Async(data.Files[0]);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.HandleException(ex);
            }
        }

        private string CorrectFileName(string value)
        {
            if (value.IsStringNullOrEmpty())
                return string.Empty;
            value = value.ToNoSign().Replace("/", "-").Replace("#", "-").Replace("?", "-");
            value = Regex.Replace(value, "[^a-zA-Z0-9_./]+", "-", RegexOptions.Compiled);
            while (value.Contains("--")) value = value.Replace("--", "-");
            value = value.Trim(new[] { ' ', '-', '.' });
            value = value.ToLower();
            return value;
        }
        private ReceiptResult ParseReceiptText(string text)
        {
            var transactions = new List<RTransaction>();
            decimal totalAmount = 0;

            // Mẫu regex để tìm các dòng có thông tin giao dịch
            var paymentRegex = new Regex(@"THANH TOÁN\s+\d+\s+VND\s+(\d+)");
            var cancelRegex = new Regex(@"HỦY T TOÁN\s+\d+\s+VND\s+(\d+)");
            var totalRegex = new Regex(@"TỔNG CỘNG.*?VND\s+(\d+)");

            // Tìm thông tin giao dịch
            foreach (Match match in paymentRegex.Matches(text))
            {
                transactions.Add(new RTransaction
                {
                    Type = "Thanh toán",
                    Amount = decimal.Parse(match.Groups[1].Value),
                    PaymentMethod = "Credit" // Cần bổ sung logic phân loại Debit/Credit nếu có
                });
            }

            foreach (Match match in cancelRegex.Matches(text))
            {
                transactions.Add(new RTransaction
                {
                    Type = "Hủy thanh toán",
                    Amount = decimal.Parse(match.Groups[1].Value),
                    PaymentMethod = "Debit"
                });
            }

            // Tìm tổng số tiền
            var totalMatch = totalRegex.Match(text);
            if (totalMatch.Success)
            {
                totalAmount = decimal.Parse(totalMatch.Groups[1].Value);
            }

            return new ReceiptResult
            {
                Transactions = transactions,
                TotalAmount = totalAmount
            };
        }
        private string ExtractTextFromImage(string filePath)
        {
            using (var engine = new TesseractEngine(@"./tessdata", "vie", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(filePath))
                {
                    using (var page = engine.Process(img))
                    {
                        return page.GetText();
                    }
                }
            }
        }
        private async Task<byte[]> DownloadFileAsync(string url)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                return await httpClient.GetByteArrayAsync(url);
            }
        }
        private async Task<IActionResult> UploadFileToS3(IFormFile file)
        {
            using (var client = new AmazonS3Client(_appSettings.AmazonKey, _appSettings.AmazonSecret, new AmazonS3Config
            {
                ServiceURL = _appSettings.AmazonUrl,
                ForcePathStyle = true
            }))
            {
                using (var uploadStream = new MemoryStream())
                {
                    file.CopyTo(uploadStream);
                    var fileName = SecurityHelper.GenerateVerifyCode() + "_" + ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    fileName = CorrectFileName(fileName).Replace(")", string.Empty).Replace("(", string.Empty).Replace(" ", "-");

                    var extension = fileName.Substring(fileName.LastIndexOf("."));
                    var contentType = URF.Core.Helper.Helpers.UtilityHelper.GetMimeType(extension);
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        Key = fileName,
                        ContentType = contentType,
                        InputStream = uploadStream,
                        CannedACL = S3CannedACL.PublicRead,
                        BucketName = _appSettings.AmazonBucketName,
                    };
                    var fileTransferUtility = new TransferUtility(client);
                    await fileTransferUtility.UploadAsync(uploadRequest);
                    return Ok(ResultApi.ToEntity(_appSettings.AmazonUrl + "/" + _appSettings.AmazonBucketName + "/" + fileName));
                }
            }
        }
        private IActionResult UploadFile(IFormFile file, string folderName)
        {
            var resource = System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"resources");
            if (!Directory.Exists(resource + "/" + folderName))
                Directory.CreateDirectory(resource + "/" + folderName);
            var folder = resource + "/" + folderName;
            using (var newMemoryStream = new MemoryStream())
            {
                file.CopyTo(newMemoryStream);
                var fileName = SecurityHelper.GenerateVerifyCode() + "_" + ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                fileName = CorrectFileName(fileName).Replace(")", string.Empty).Replace("(", string.Empty).Replace(" ", "-");
                var extension = fileName.Substring(fileName.LastIndexOf("."));

                // save file
                var extensionFormat = extension.Replace(".", string.Empty);
                var localFileName = Guid.NewGuid().ToString().Replace("-", string.Empty) + "." + extensionFormat;
                System.IO.File.WriteAllBytes(folder + "/" + localFileName, newMemoryStream.ToArray());
                newMemoryStream.Close();
                return Ok(ResultApi.ToEntity("/resources/" + folderName + "/" + localFileName));
            }
        }
        private async Task<IActionResult> UploadAndRemoveBgToS3Async(IFormFile file)
        {
            using (var client = new AmazonS3Client(_appSettings.AmazonKey, _appSettings.AmazonSecret, new AmazonS3Config
            {
                ServiceURL = _appSettings.AmazonUrl,
                ForcePathStyle = true
            }))
            {
                using (var bgStream = new MemoryStream())
                {
                    file.CopyTo(bgStream);
                    var fileName = SecurityHelper.GenerateVerifyCode() + "_" + ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    fileName = CorrectFileName(fileName)
                        .Replace(")", string.Empty)
                        .Replace("(", string.Empty)
                        .Replace(" ", "-");
                    using (var clientBg = new HttpClient())
                    {
                        using (var formData = new MultipartFormDataContent())
                        {
                            var keys = _appSettings.RemoveBgKey.Split(";");
                            var random = new Random().Next(0, keys.Length);

                            var key = keys[random];
                            formData.Headers.Add("X-Api-Key", key);
                            formData.Add(new StringContent("auto"), "size");
                            formData.Add(new ByteArrayContent(bgStream.ToArray()), "image_file", fileName);
                            var response = clientBg.PostAsync("https://api.remove.bg/v1.0/removebg", formData).Result;

                            if (response.IsSuccessStatusCode)
                            {
                                using (var uploadStream = new MemoryStream())
                                {
                                    await response.Content.CopyToAsync(uploadStream);
                                    var extension = fileName.Substring(fileName.LastIndexOf("."));
                                    var contentType = URF.Core.Helper.Helpers.UtilityHelper.GetMimeType(extension);

                                    try
                                    {
                                        var uploadRequest = new TransferUtilityUploadRequest
                                        {
                                            Key = fileName,
                                            ContentType = contentType,
                                            InputStream = uploadStream,
                                            CannedACL = S3CannedACL.PublicRead,
                                            BucketName = _appSettings.AmazonBucketName,
                                        };
                                        var fileTransferUtility = new TransferUtility(client);
                                        await fileTransferUtility.UploadAsync(uploadRequest);
                                        return Ok(ResultApi.ToEntity(_appSettings.AmazonUrl + "/" + _appSettings.AmazonBucketName + "/" + fileName));
                                    }
                                    catch (Exception ex)
                                    {
                                        return Ok(ResultApi.ToException(ex));
                                    }
                                }
                            }
                            else return Ok(ResultApi.ToError());
                        }
                    }
                }
            }
        }
    }

    public class FontResolver : IFontResolver
    {
        public string DefaultFontName => "Arial";
        public byte[] GetFont(string faceName)
        {
            // Load dữ liệu font từ đường dẫn tới font
            string fontPath = "resources/fonts/Arial.ttf";
            if (fontPath != null)
            {
                return File.ReadAllBytes(fontPath);
            }

            // Trả về null để sử dụng font mặc định nếu không tìm thấy
            return null;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Arial", StringComparison.CurrentCultureIgnoreCase))
            {
                if (isBold && isItalic)
                {
                    return new FontResolverInfo("resources/fonts/Arial.ttf");
                }
                else if (isBold)
                {
                    return new FontResolverInfo("resources/fonts/Arial.ttf");
                }
                else if (isItalic)
                {
                    return new FontResolverInfo("resources/fonts/Arial.ttf");
                }
                else
                {
                    return new FontResolverInfo("resources/fonts/Arial.ttf");
                }
            }
            return null;
        }
    }

    public class RTransaction
    {
        public string Type { get; set; }  // Loại giao dịch (Thanh toán, Hủy thanh toán)
        public decimal Amount { get; set; }  // Số tiền
        public string PaymentMethod { get; set; }  // Phương thức thanh toán (Debit, Credit)
    }

    public class ReceiptResult
    {
        public decimal TotalAmount { get; set; }
        public List<RTransaction> Transactions { get; set; }
    }
}
