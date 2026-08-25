using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;

namespace Qms.Api.Errors;

public sealed class QmsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<QmsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            QmsForbiddenException => (StatusCodes.Status403Forbidden, "Bu işlem için yetkiniz bulunmuyor"),
            ArgumentException => (StatusCodes.Status400BadRequest, "İstek doğrulanamadı"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "İşlem mevcut durumla çakışıyor"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Kayıt başka bir kullanıcı tarafından değiştirildi"),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir sunucu hatası oluştu")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected with {StatusCode} for {Method} {Path}", status, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            },
            Exception = exception
        });
    }
}
