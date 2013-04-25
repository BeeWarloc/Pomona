﻿// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2013 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nancy.Testing;
using Pomona.Common.Web;

namespace Pomona.TestHelpers
{
    public class NancyTestingWebClient : IWebClient
    {
        private readonly Browser browser;
        private readonly IDictionary<string, string> headers = new Dictionary<string, string>();

        public NancyTestingWebClient(Browser browser)
        {
            if (browser == null) throw new ArgumentNullException("browser");
            this.browser = browser;
        }

        public IDictionary<string, string> Headers
        {
            get { return headers; }
        }

        public WebClientResponseMessage Send(WebClientRequestMessage request)
        {
            Func<string, Action<BrowserContext>, BrowserResponse> browserMethod;

            switch (request.Method.ToUpper())
            {
                case "POST":
                    browserMethod = browser.Post;
                    break;
                case "PATCH":
                    browserMethod = browser.Patch;
                    break;
                case "GET":
                    browserMethod = browser.Get;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var uri = new Uri(request.Uri);
            var browserResponse = browserMethod(uri.LocalPath, bc =>
                {
                    bc.HttpRequest();
                    ((IBrowserContextValues) bc).QueryString = uri.Query;
                    foreach (var kvp in headers.Concat(request.Headers))
                    {
                        bc.Header(kvp.Key, kvp.Value);
                    }
                    if (request.Data != null)
                    {
                        bc.Body(new MemoryStream(request.Data));
                    }
                });

            return new WebClientResponseMessage(request.Uri, browserResponse.Body.ToArray(),
                                                (HttpStatusCode) browserResponse.StatusCode, browserResponse.Headers, "1.1");
        }

        public Task<WebClientResponseMessage> SendAsync(WebClientRequestMessage requestMessage)
        {
            throw new NotImplementedException();
        }
    }
}