using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WFAI.Application.Dtos.Wrappers;

namespace WFAI.API.Extensions;

public static class ResponseResultExtensions
{
    public static IResult ToApiResult(
     this IResponseWrapper response,
     int? successCode = null,
     int? failureCode = null)
    {
        var statusCode = response.StatusCode;

        if (statusCode <= 0)
        {
            statusCode = response.IsSuccessful
                ? successCode ?? StatusCodes.Status200OK
                : failureCode ?? StatusCodes.Status400BadRequest;
        }

        return Results.Json(
            response,
            statusCode: statusCode,
            contentType: "application/json");
    }
}