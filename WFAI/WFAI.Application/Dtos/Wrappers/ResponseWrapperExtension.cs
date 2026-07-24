using System.Text.Json;

namespace WFAI.Application.Dtos.Wrappers
{
    public static class ResponseWrapperExtension
    {
        public static async Task<IResponseWrapper<T>> ToResponse<T>(this HttpResponseMessage responseMessage)
        {
            if (!responseMessage.IsSuccessStatusCode)
            {
                var errorContent = await responseMessage.Content.ReadAsStringAsync();
                return ResponseWrapper<T>.Fail(
                    $"Request failed with status code {responseMessage.StatusCode}. Details: {errorContent}",
                    (int)responseMessage.StatusCode);
            }

            var responseAsString = await responseMessage.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseAsString))
            {
                return ResponseWrapper<T>.Fail("Empty response received from the server.", 204);
            }

            var responseObject = JsonSerializer.Deserialize<ResponseWrapper<T>>(responseAsString,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return responseObject ?? ResponseWrapper<T>.Fail("Failed to deserialize response.", 500);
        }

        public static async Task<IResponseWrapper> ToResponse(this HttpResponseMessage responseMessage)
        {
            if (!responseMessage.IsSuccessStatusCode)
            {
                var errorContent = await responseMessage.Content.ReadAsStringAsync();
                return ResponseWrapper.Fail(
                    $"Request failed with status code {responseMessage.StatusCode}. Details: {errorContent}",
                    (int)responseMessage.StatusCode);
            }

            var responseAsString = await responseMessage.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseAsString))
            {
                return ResponseWrapper.Fail("Empty response received from the server.", 204);
            }

            var responseObject = JsonSerializer.Deserialize<ResponseWrapper>(responseAsString,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return responseObject ?? ResponseWrapper.Fail("Failed to deserialize response.", 500);
        }
    }
}