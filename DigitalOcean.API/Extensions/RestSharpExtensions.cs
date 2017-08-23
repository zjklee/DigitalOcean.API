using System;
using System.Threading.Tasks;
using DigitalOcean.API.Exceptions;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Extensions;

namespace DigitalOcean.API.Extensions
{
    public static class RestClientExtensions
    {
        public static async Task<RestResponse> ExecuteTaskAsync(this IRestClient client, IRestRequest request)
        {
            TaskCompletionSource<IRestResponse> taskCompletion = new TaskCompletionSource<IRestResponse>();
            RestRequestAsyncHandle handle = client.ExecuteAsync(request, r => taskCompletion.SetResult(r));
            return (RestResponse)(await taskCompletion.Task);
        }
    }
    
    public static class RestSharpExtensions
    {
        public static async Task<T> ExecuteTask<T>(this IRestClient client, IRestRequest request)
            where T : new()
        {
            var ret = await client.ExecuteTaskAsync(request).ConfigureAwait(false);
            return ret.ThrowIfException().Deserialize<T>();
        }

        public static async Task<IRestResponse> ExecuteTaskRaw(this IRestClient client, IRestRequest request)
        {
            var ret = await client.ExecuteTaskAsync(request).ConfigureAwait(false);
            request.OnBeforeDeserialization(ret);
            return ret.ThrowIfException();
        }

        private static IRestResponse ThrowIfException(this IRestResponse response)
        {
            if (response.ErrorException != null)
            {
                throw new Exception("There was an an exception thrown during the request.",
                    response.ErrorException);
            }

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                throw response.ResponseStatus.ToWebException();
            }

            if ((int) response.StatusCode >= 400)
            {
                throw new ApiException(response.StatusCode);
            }

            return response;
        }

        public static T Deserialize<T>(this IRestResponse response)
        {
            response.Request.OnBeforeDeserialization(response);
            var deserialize = new JsonDeserializer
            {
                RootElement = response.Request.RootElement,
                DateFormat = response.Request.DateFormat
            };
            return deserialize.Deserialize<T>(response);
        }
    }
}