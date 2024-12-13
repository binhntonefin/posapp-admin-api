using Amazon.S3.Transfer;
using Amazon.S3;
using PosApp.Admin.Api.Services.Contract;
using URF.Core.EF.Trackable.Models;
using URF.Core.EF.Trackable;
using URF.Core.Helper.Helpers;
using System.Text.RegularExpressions;
using URF.Core.Helper.Extensions;
using Microsoft.Extensions.Options;
using Amazon;

namespace PosApp.Admin.Api.Services.Implement
{
    public class UploadService : IUploadService
    {
        private readonly AppSettings _appSettings;
        public UploadService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<ResultApi> UploadFileToS3(MemoryStream newMemoryStream, string fileName, bool generate = true)
        {
            using (var client = new AmazonS3Client(_appSettings.AmazonKey, _appSettings.AmazonSecret, new AmazonS3Config
            {
                ServiceURL = _appSettings.AmazonUrl,
                ForcePathStyle = true
            }))
            {
                if (generate)
                    fileName = SecurityHelper.GenerateVerifyCode() + "_" + fileName.Trim('"');
                else fileName = fileName.Trim('"');
                fileName = CorrectFileName(fileName).Replace(")", string.Empty).Replace("(", string.Empty).Replace(" ", "-");                
                var arrayFileNames = fileName.Split('_');
                if (!arrayFileNames.IsNullOrEmpty() && arrayFileNames[0].Length == 6)
                {
                    arrayFileNames[0] = SecurityHelper.GenerateVerifyCode(6);
                    fileName = string.Join('_', arrayFileNames);
                }
                var extension = fileName.Substring(fileName.LastIndexOf("."));
                var contentType = UtilityHelper.GetMimeType(extension);
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    Key = fileName,
                    ContentType = contentType,
                    InputStream = newMemoryStream,
                    CannedACL = S3CannedACL.PublicRead,
                    BucketName = _appSettings.AmazonBucketName,
                };
                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);
                return ResultApi.ToEntity(_appSettings.AmazonUrl + "/" + _appSettings.AmazonBucketName + "/" + fileName);
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
    }
}
