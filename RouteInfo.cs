using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using CollectionJsonExtended.Client.Attributes;

namespace CollectionJsonExtended.Client
{
    public class RouteInfo
    {
        public static readonly IList<RouteInfo> InternalCache; //public for debug
        
        static RouteInfo()
        {
            InternalCache = new List<RouteInfo>();
        }
        
        public Type EntityType { get; set; }
        
        public HttpStatusCode HttpStatusCode { get; set; }
        
        public ActionDescriptor ActionDescriptor { get; set; }

        public string RouteName { get; set; }

        public ICollection<string> AllowedMethods { get; set; }

        public Is Kind { get; set; }

        public string Relation { get; set; }

        public string Template { get; set; }
        

        public static RouteInfo[] GetPublishedRouteInfos(Type entityType)
        {
            return InternalCache.Where(r => r.EntityType == entityType).ToArray();
        } 

        public static void Publish(RouteInfo routeInfo)
        {
            InternalCache.Add(routeInfo);
        }

    }
}