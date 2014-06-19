#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2014 Karsten Nikolai Strand
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

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

using Pomona.Common.Internals;
using Pomona.Common.Linq;
using Pomona.Common.Proxies;
using Pomona.Common.Serialization.Json;
using Pomona.Common.Serialization.Patch;
using Pomona.Common.Web;

namespace Pomona.Common
{
    public abstract class ClientBase : IPomonaClient
    {
        internal ClientBase()
        {
        }


        public abstract string BaseUri { get; }
        public abstract IWebClient WebClient { get; }
        public event EventHandler<ClientRequestLogEventArgs> RequestCompleted;


        public abstract void Delete<T>(T resource)
            where T : class, IClientResource;


        public abstract object Get(string uri, Type type);

        public abstract T Get<T>(string uri);


        public abstract T GetLazy<T>(string uri)
            where T : class, IClientResource;


        public abstract T Patch<T>(T target, Action<T> updateAction, Action<IRequestOptions<T>> options = null)
            where T : class, IClientResource;


        public abstract object Post<T>(Action<T> postAction)
            where T : class, IClientResource;


        public abstract IQueryable<T> Query<T>();
        public abstract IQueryable<T> Query<T>(string uri);
        public abstract T Reload<T>(T resource);
        public abstract bool TryGetResourceInfoForType(Type type, out ResourceInfoAttribute resourceInfo);


        protected void RaiseRequestCompleted(WebClientRequestMessage request,
            WebClientResponseMessage response,
            Exception thrownException = null)
        {
            var eh = RequestCompleted;
            if (eh != null)
                eh(this, new ClientRequestLogEventArgs(request, response, thrownException));
        }


        internal abstract object Post<T>(string uri, Action<T> postAction, RequestOptions options)
            where T : class, IClientResource;


        internal abstract object Post<T>(string uri, T form, RequestOptions options)
            where T : class, IClientResource;
    }

    public abstract class ClientBase<TClient> : ClientBase
    {
        private static readonly ClientTypeMapper typeMapper;
        private readonly string baseUri;

        private readonly IRequestDispatcher dispatcher;
        private readonly IWebClient webClient;


        static ClientBase()
        {
            // Preload resource info attributes..
            typeMapper = new ClientTypeMapper(typeof(TClient).Assembly);
        }

        protected ClientBase(string baseUri, IWebClient webClient)
        {
            this.webClient = webClient ?? new HttpWebRequestClient();

            this.baseUri = baseUri;

            var serializerFactory =
                new PomonaJsonSerializerFactory(new ClientSerializationContextProvider(typeMapper, this));
            this.dispatcher = new RequestDispatcher(
                typeMapper,
                this.webClient,
                serializerFactory,
                RaiseRequestCompleted);

            InstantiateClientRepositories();
        }


        public override string BaseUri
        {
            get { return this.baseUri; }
        }

        public IEnumerable<Type> ResourceTypes
        {
            get { return typeMapper.ResourceTypes; }
        }

        public override IWebClient WebClient
        {
            get { return this.webClient; }
        }

        internal static ClientTypeMapper ClientTypeMapper
        {
            get { return typeMapper; }
        }


        public override void Delete<T>(T resource)
        {
            var uri = ((IHasResourceUri)resource).Uri;
            this.dispatcher.SendRequest(uri, null, "DELETE");
        }


        public override object Get(string uri, Type type)
        {
            return this.dispatcher.SendRequest(uri, null, "GET", null, type);
        }


        public override T Get<T>(string uri)
        {
            return (T)Get(uri, typeof(T));
        }


        public override T GetLazy<T>(string uri)
        {
            var typeInfo = this.GetResourceInfoForType(typeof(T));
            var proxy = (LazyProxyBase)Activator.CreateInstance(typeInfo.LazyProxyType);
            proxy.Uri = uri;
            proxy.Client = this;
            proxy.ProxyTargetType = typeInfo.PocoType;
            return (T)((object)proxy);
        }


        private string GetUriOfType(Type type)
        {
            return BaseUri + this.GetResourceInfoForType(type).UrlRelativePath;
        }


        public override T Patch<T>(T target, Action<T> updateAction, Action<IRequestOptions<T>> options = null)
        {
            var patchForm = (T)typeMapper.CreatePatchForm(typeof(T), target);
            updateAction(patchForm);

            var requestOptions = new RequestOptions<T>();
            if (options != null)
                options(requestOptions);

            return Patch(patchForm, requestOptions);
        }


        public override object Post<T>(Action<T> postAction)
        {
            ExtendedResourceInfo info;
            string uri;
            if (ExtendedResourceInfo.TryGetExtendedResourceInfo(typeof(T), this, out info))
                uri = GetUriOfType(info.ServerType);
            else
                uri = GetUriOfType(typeof(T));

            return Post(uri, postAction, null);
        }


        public override IQueryable<T> Query<T>()
        {
            return Query<T>(null);
        }


        public override IQueryable<T> Query<T>(string uri)
        {
            return
                typeMapper.WrapExtendedQuery<T>(
                    st => new RestQueryProvider(this).CreateQuery(uri ?? GetUriOfType(st), st));
        }


        public override T Reload<T>(T resource)
        {
            var resourceWithUri = resource as IHasResourceUri;
            if (resourceWithUri == null)
            {
                throw new ArgumentException("Could not find resource URI, resouce not of type IHasResourceUri.",
                    "resource");
            }

            if (resourceWithUri.Uri == null)
                throw new ArgumentException("Uri on resource was null.", "resource");

            if (!typeof(T).IsInterface)
                throw new ArgumentException("Type should be an interface inherited from a known resource type.");

            var resourceType = this.GetMostInheritedResourceInterface(typeof(T));
            return (T)Get(resourceWithUri.Uri, resourceType);
        }


        public override bool TryGetResourceInfoForType(Type type, out ResourceInfoAttribute resourceInfo)
        {
            return typeMapper.TryGetResourceInfoForType(type, out resourceInfo);
        }


        public string GetRelativeUriForType(Type type)
        {
            var resourceInfo = this.GetResourceInfoForType(type);
            return resourceInfo.UrlRelativePath;
        }


        internal override object Post<T>(string uri, Action<T> postAction, RequestOptions options)
        {
            var postForm = (T)typeMapper.CreatePostForm(typeof(T));
            postAction(postForm);
            return Post(uri, postForm, options);
        }


        internal override object Post<T>(string uri, T form, RequestOptions options)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
            if (form == null)
                throw new ArgumentNullException("form");

            return dispatcher.SendRequest(uri, form, "POST", options);
        }
        private void AddIfMatchToPatch(object postForm, RequestOptions requestOptions)
        {
            string etagValue = null;
            var resourceInfo = typeMapper.GetMostInheritedResourceInterfaceInfo(postForm.GetType());
            if (resourceInfo.HasEtagProperty)
                etagValue = (string)resourceInfo.EtagProperty.GetValue(postForm, null);

            if (etagValue != null)
            {
                requestOptions.ModifyRequest(
                    request => request.Headers.Add("If-Match", string.Format("\"{0}\"", etagValue)));
            }
        }


        private void InstantiateClientRepositories()
        {
            var generatedAssembly = GetType().Assembly;
            var repoTypes = generatedAssembly.GetTypes()
                .Where(x => typeof(IClientRepository).IsAssignableFrom(x) && !x.IsInterface && !x.IsGenericType).ToList();
            var repositoryImplementations =
                repoTypes
                    .Select(
                        x =>
                            new
                            {
                                Interface =
                                    x.GetInterfaces()
                                        .First(y => y.Assembly == generatedAssembly && y.Name == "I" + x.Name),
                                Implementation = x
                            })
                    .ToDictionary(x => x.Interface, x => x.Implementation);

            foreach (
                var prop in
                    GetType().GetProperties().Where(x => typeof(IClientRepository).IsAssignableFrom(x.PropertyType)))
            {
                var repositoryInterface = prop.PropertyType;
                var repositoryImplementation = repositoryImplementations[repositoryInterface];

                Type[] typeArgs;
                if (!repositoryInterface.TryExtractTypeArguments(typeof(IQueryableRepository<>), out typeArgs))
                    throw new InvalidOperationException("Expected IQueryableRepository to inherit IClientRepository..");

                var tResource = typeArgs[0];
                var uri = GetUriOfType(tResource);
                prop.SetValue(this, Activator.CreateInstance(repositoryImplementation, this, uri, null, null), null);
            }
        }


        private T Patch<T>(T form, RequestOptions requestOptions)
            where T : class
        {
            if (form == null)
                throw new ArgumentNullException("form");

            var uri = GetUriOfForm(form);

            AddIfMatchToPatch(form, requestOptions);
            return (T)dispatcher.SendRequest(uri, form, "PATCH", requestOptions);
        }


        private string GetUriOfForm(object form)
        {
            if (form == null)
                throw new ArgumentNullException("form");
            var delta = form as IDelta;
            if (delta != null)
                return ((IHasResourceUri)delta.Original).Uri;
            var extendedProxy = form as IExtendedResourceProxy;
            if (extendedProxy != null)
                return GetUriOfForm(extendedProxy.WrappedResource);
            throw new InvalidOperationException("Unable to retrieve uri from resource of type " + form.GetType().FullName);
        }
    }
}