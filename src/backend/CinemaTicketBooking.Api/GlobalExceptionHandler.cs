using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;

namespace CinemaTicketBooking.Api;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        int statusCode;
        string code;
        string message;
        object details;

        switch (exception)
        {
            case NotFoundApiException notFound:
                statusCode = notFound.StatusCode;
                code = notFound.Code;
                message = notFound.Message;
                details = notFound.Details ?? new { };
                break;
            case ApiRequestException apiEx:
                statusCode = apiEx.StatusCode;
                code = apiEx.Code;
                message = apiEx.Message;
                details = apiEx.Details ?? new { };
                break;
            case BadHttpRequestException:
            case System.Text.Json.JsonException:
                statusCode = StatusCodes.Status400BadRequest;
                code = "VALIDATION_ERROR";
                message = "Yêu cầu không hợp lệ.";
                details = new { };
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                code = "INTERNAL_ERROR";
                message = "Đã xảy ra lỗi khi xử lý yêu cầu.";
                details = new { };
                break;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                code,
                message,
                details,
                traceId
            }
        }, cancellationToken);

        return true;
    }
}
