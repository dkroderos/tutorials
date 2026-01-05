using Ctf.Api.Options;

namespace Ctf.Api.Extensions;

public static class CommonExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: result.Error.Code, detail: result.Error.Detail);
    }

    public static IResult ToHttpResult(this Result result)
    {
        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(statusCode: result.Error.Code, detail: result.Error.Detail);
    }

    public static IResult ToSignInResult<TResult, TResponse>(
        this Result<TResult> result,
        JwtOptions options,
        HttpContext httpContext,
        Func<TResult, string> refreshTokenSelector,
        Func<TResult, TResponse> responseSelector
    )
    {
        if (result.IsFailure)
            return result.ToHttpResult();

        var cookieOptions = new CookieOptions
        {
            Expires = DateTime.UtcNow.Add(options.RefreshTokenLifetime),
            HttpOnly = true,
            Secure = options.SecureRefreshTokenCookie,
            SameSite = SameSiteMode.Strict,
        };

        var value = result.Value;
        httpContext.Response.Cookies.Append(
            CommonConstants.RefreshTokenCookieKey,
            refreshTokenSelector(value),
            cookieOptions
        );

        return Results.Ok(responseSelector(value));
    }

    public static string ToSortOrderSql(this bool isAscending) => isAscending ? "ASC" : "DESC";

    public static int ToValidPage(this int? page)
    {
        var value = page ?? 1;
        return value < 1 ? 1 : value;
    }

    public static int ToValidPageSize(this int? pageSize)
    {
        var validPageSize = pageSize.GetValueOrDefault(CommonConstants.DefaultPageSize);
        validPageSize = Math.Max(validPageSize, 1);
        return Math.Min(validPageSize, CommonConstants.MaxPageSize);
    }
}
