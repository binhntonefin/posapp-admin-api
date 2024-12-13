using URF.Core.EF.Trackable.Models;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface IUploadService
    {
        Task<ResultApi> UploadFileToS3(MemoryStream newMemoryStream, string fileName, bool generate = true);
    }
}
