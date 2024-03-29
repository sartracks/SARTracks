﻿using System;
using System.Web;
using DotNetOpenAuth;

namespace SarTracks.Website.Services
{
    public class EmbeddedResourceUrlService : IEmbeddedResourceRetrieval
    {
        private static string pathFormat = "{0}/Resource/GetWebResourceUrl?assemblyName={1}&typeName={2}&resourceName={3}";
        //private static string pathFormat = "{0}/Resource/GetWebResourceUrl";

        public Uri GetWebResourceUrl(Type someTypeInResourceAssembly, string manifestResourceName)
        {
            if (manifestResourceName.Contains("http"))
            {
                return new Uri(manifestResourceName);
            }
            else
            {
                var assembly = someTypeInResourceAssembly.Assembly;

                // HACK
                //string completeUrl = HttpContext.Current.Request.Url.ToString();
                //string host = completeUrl.Substring(0,
                //    completeUrl.IndexOf(HttpContext.Current.Request.Url.AbsolutePath));
                //string host = System.Web.Mvc.UrlHelper.GenerateContentUrl("~", new HttpContextWrapper(HttpContext.Current));
                string host = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + System.Web.Mvc.UrlHelper.GenerateContentUrl("~", new HttpContextWrapper(HttpContext.Current));

                var path = string.Format(pathFormat,
                            host,
                            HttpUtility.UrlEncode(assembly.FullName),
                            HttpUtility.UrlEncode(someTypeInResourceAssembly.ToString()),
                            HttpUtility.UrlEncode(manifestResourceName));

                return new Uri(path);
            }
        }
    }
}