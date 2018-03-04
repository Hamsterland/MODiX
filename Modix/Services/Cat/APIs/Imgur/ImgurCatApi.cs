﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace Modix.Services.Cat.APIs.Imgur
{
    public class ImgurCatApi : ICatApi
    {
        private const string Secret = "secret";
        private const string Url = "https://api.imgur.com/3/gallery/r/cats/page/";

        private readonly IHttpClient _httpClient;
        private readonly List<string> _linkPool = new List<string>();

        public ImgurCatApi(IHttpClient httpClient)
        {
            _httpClient = httpClient;

            // Add the header to the HTTP client for Imgur authorisation
            // TODO Verify if HTTPClient allows for the same header to be applied multiple times
            // We could have an error here, since the client is supposed to be singleton
            httpClient.AddHeader("Authorization", $"Client-ID {Secret}");
        }

        public async Task<string> Fetch(CancellationToken cancellationToken)
        {
            try
            {
                // If we have any cat URLs in the pool, try to fetch those first
                if (_linkPool.Any())
                    return GetCachedCat();

                using (var response = await _httpClient.GetAsync(Url, cancellationToken))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        var imgur = Deserialise(content);

                        // We may have succeeded in the response, but Imgur may not like us,
                        // check if Imgur has returned us a successful result
                        if (!imgur.Success)
                            return null;

                        // We have a caching mechanism in place, therefore attempt to get
                        // all links of the URLs and cache
                        var links = imgur.Album.SelectMany(x => x.Images).Select(x => x.Link).ToList();

                        // We want to return the first link, since we wanted to fetch a
                        // URL to begin with, so remove the first link and return that
                        // to the user
                        var primaryLink = links.First();

                        links.Remove(primaryLink);

                        _linkPool.AddRange(links);

                        return primaryLink;
                    }

                    // If there is a bad result, empty string will be returned
                    Log.Warning("Invalid HTTP Status Code");
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Warning("Failed fetching Imgur cat", ex.InnerException);
            }

            // We somehow got nothing, so return null and deal in the invoker
            return null;
        }

        private static Response Deserialise(string content)
            => JsonConvert.DeserializeObject<Response>(content);

        private string GetCachedCat()
        {
            // Get the top of the list
            var cachedCat = _linkPool.First();

            // Remove the URL, so we don't recycle it
            _linkPool.Remove(cachedCat);

            return cachedCat;
        }
    }
}