using Microsoft.AspNetCore.Mvc;

namespace Almny.Api.Abstractions;

public static class ResultExtensions
{
    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess() : onFailure(result.Error);

    public static TOut Match<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);

    public static ObjectResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a success result to problem details.");

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = result.Error.Code,
            Detail = result.Error.Description
        })
        {
            StatusCode = statusCode
        };
    }
}
