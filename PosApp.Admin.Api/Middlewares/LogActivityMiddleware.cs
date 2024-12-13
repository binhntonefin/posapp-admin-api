using PosApp.Admin.Api.Helpers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using URF.Core.Abstractions.Trackable;
using URF.Core.Abstractions;
using URF.Core.EF.Trackable.Entities;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Middlewares
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly string[] ignores = new string[] { "/lookup", "/utility", "/log", "/upload", "/import", "/export", "/notifyhub" };

        public LoggerMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task Invoke(HttpContext context, IHttpContextAccessor httpContextAccessor)
        {
            int userId = 0;
            LogActivity log = null;
            var injectedRequestStream = new MemoryStream();
            try
            {
                // get userid
                if (context.User != null)
                {
                    var identity = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (identity != null)
                        userId = identity.Value.ToInt32();
                }
                else if (httpContextAccessor.HttpContext != null)
                {
                    var identity = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (identity != null)
                        userId = identity.Value.ToInt32();
                }

                // get endpoint
                ControllerActionDescriptor controllerActionDescriptor = null;
                if (context.GetEndpoint() != null)
                {
                    controllerActionDescriptor = context
                       .GetEndpoint()
                       .Metadata
                       .GetMetadata<ControllerActionDescriptor>();
                }

                // method
                var method = context.Request.Method;

                // log
                if (controllerActionDescriptor != null && !userId.IsNumberNull() && !method.EqualsEx("GET"))
                {
                    var url = context.Request.Path.Value;
                    if (ignores.All(c => !url.ContainsEx(c)))
                    {
                        var objectId = string.Empty;
                        var routeValues = context.Request.RouteValues.ToList();
                        var actionName = controllerActionDescriptor.ActionName;
                        var controllerName = controllerActionDescriptor.ControllerName;
                        if (routeValues.Count >= 3) objectId = routeValues[2].Value.ToString();

                        using (var bodyReader = new StreamReader(context.Request.Body))
                        {
                            var bodyAsText = bodyReader.ReadToEnd();
                            if (objectId.IsStringNullOrEmpty() && !bodyAsText.IsStringNullOrEmpty())
                            {
                                var jtoken = JToken.Parse(bodyAsText);
                                if (jtoken.Type == JTokenType.Array)
                                {
                                    var jsonArray = JArray.Parse(bodyAsText);
                                    List<string> listId = new List<string>();
                                    try
                                    {
                                        foreach (var j in jsonArray)
                                        {
                                            if (j["Id"] != null)
                                                listId.Add(j["Id"].ToString());
                                        }
                                        if (listId.Count > 0)
                                        {
                                            objectId = String.Join(',', listId);
                                        }
                                    }
                                    catch { }
                                }
                                else
                                {
                                    var jsonObj = JObject.Parse(bodyAsText);
                                    if (jsonObj["Id"] != null)
                                        objectId = jsonObj["Id"].ToString();
                                }
                            }
                            log = new LogActivity
                            {
                                Url = url,
                                Method = method,
                                UserId = userId,
                                Body = bodyAsText,
                                Action = actionName,
                                ObjectId = objectId,
                                DateTime = DateTime.Now,
                                Controller = controllerName,
                                Ip = context.Connection.RemoteIpAddress.MapToIPv4().ToString(),
                            };

                            // next
                            var bytesToWrite = Encoding.UTF8.GetBytes(bodyAsText);
                            injectedRequestStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                            injectedRequestStream.Seek(0, SeekOrigin.Begin);
                            context.Request.Body = injectedRequestStream;
                            await HandleActivityAsync(context, log);
                        }
                    }
                }

                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                if (!userId.IsNumberNull())
                {
                    var logEx = CreateLogException(userId, ex);
                    await HandleExceptionAsync(context, logEx);
                }
            }
        }

        private static LogException CreateLogException(int userId, Exception ex)
        {
            return new LogException
            {
                UserId = userId,
                Exception = ex.Message,
                DateTime = DateTime.Now,
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException?.Message,
            };
        }

        private async Task HandleActivityAsync(HttpContext context, LogActivity log)
        {
            if (log != null && !log.UserId.IsNumberNull())
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var logActivityRepository = scope.ServiceProvider.GetRequiredService<IRepositoryX<LogActivity>>();

                    logActivityRepository.Insert(log);
                    await unitOfWork.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.HandleException(ex);
                }
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        private async Task HandleExceptionAsync(HttpContext context, LogException log)
        {
            if (log != null && !log.UserId.IsNumberNull())
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var logExceptionRepository = scope.ServiceProvider.GetRequiredService<IRepositoryX<LogException>>();

                logExceptionRepository.Insert(log);
                await unitOfWork.SaveChangesAsync();
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
}
