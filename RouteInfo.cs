using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using CollectionJsonExtended.Core;

namespace CollectionJsonExtended.Client
{
    public sealed class RouteInfo : UrlInfoBase
    {
        public RouteInfo(Type entityType)
            : base(entityType)
        {
            
        }


        public HttpStatusCode StatusCode { get; set; }

        public string RouteName { get; set; }

        public ICollection<string> AllowedMethods { get; set; }

        public ActionDescriptor ActionDescriptor { get; set; }

    }
}