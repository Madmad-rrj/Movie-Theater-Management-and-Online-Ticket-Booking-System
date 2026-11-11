using CinemaTicketBooking.Api.DTOs.Common;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaTicketBooking.Api.Controllers;

public sealed class CheckoutExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not CheckoutException exception)
        {
            return;
        }
        if (exception.Code == "OPERATION_IN_PROGRESS")
        {
            context.HttpContext.Response.Headers.RetryAfter = "1";
        }
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.Result = new ObjectResult(ApiErrorResponse.Create(
            context.HttpContext, exception.Code, exception.Message, exception.Details)) { StatusCode = exception.StatusCode };
        context.ExceptionHandled = true;
    }
}
