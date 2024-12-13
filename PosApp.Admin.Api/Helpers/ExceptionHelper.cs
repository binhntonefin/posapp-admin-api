using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace PosApp.Admin.Api.Helpers
{
    public class ExceptionHelper
    {
        public static ContentResult HandleException(Exception ex, int? userId = null)
        {
            if (ex is UnauthorizedAccessException)
            {
                return new ContentResult()
                {
                    StatusCode = 401,
                    Content = "Invalid login or access token",
                };
            }
            else
            {
                CreateLogException(ex, userId);
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = ex.Message,
                };
            }
        }

        private static void CreateLogException(Exception ex, int? userId = null)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}
