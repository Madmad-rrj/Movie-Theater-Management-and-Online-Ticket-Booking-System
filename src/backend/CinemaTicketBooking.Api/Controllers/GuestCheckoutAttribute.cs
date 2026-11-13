using CinemaTicketBooking.Api.DTOs.Checkout;
using CinemaTicketBooking.Api.DTOs.Common;
using CinemaTicketBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaTicketBooking.Api.Controllers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class GuestCheckoutAttribute() : TypeFilterAttribute(typeof(GuestCheckoutFilter));

public sealed class GuestCheckoutFilter(IGuestContextService guests, GuestRequestSecurity security) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        http.Response.Headers.CacheControl = "no-store";
        try
        {
            GuestRequestSecurity.RejectUnsupportedAuthorization(http);
            security.RequireSecureTransport(http);
            GuestContextDto guest;
            try
            {
                guest = await guests.ResolveAsync(
                    http.Request.Cookies[GuestCheckoutOptions.CookieName], http.RequestAborted);
            }
            catch (CheckoutException exception) when (exception.Code == "UNAUTHENTICATED")
            {
                security.ClearInvalidCookie(http);
                throw;
            }
            http.Features.Set(guest);
            if (!HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) &&
                !HttpMethods.IsOptions(http.Request.Method))
            {
                security.ValidateCsrf(http, guest);
            }
        }
        catch (CheckoutException exception)
        {
            // MVC exception filters do not handle authorization-filter exceptions.
            context.Result = new ObjectResult(ApiErrorResponse.Create(http, exception.Code, exception.Message, exception.Details))
            {
                StatusCode = exception.StatusCode
            };
        }
    }
}
