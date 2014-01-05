using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Mvc.Routing;
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


        public static IEnumerable<RouteInfo> Find(Type entityType)
        {
            return Cache.Where(r => r.EntityType == entityType) as IEnumerable<RouteInfo>;
        }

        public static IEnumerable<RouteInfo> GetCacheForDebug()
        {
            return Cache as IEnumerable<RouteInfo>;
        }
    }
}