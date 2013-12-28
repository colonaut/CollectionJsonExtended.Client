using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.UI;
using CollectionJsonExtended.Client.Attributes;

namespace CollectionJsonExtended.Client
{
    public class RoutesInfo
    {
        static readonly IList<RoutesInfo> RoutesInfoCollection;

        
        static RoutesInfo()
        {
            RoutesInfoCollection = new List<RoutesInfo>();
        }

        public RoutesInfo(Type entityType)
        {
            EntityType = entityType;
            //TODO: throws if not found. We will have to set a better error message via try catch
            //an entity must have an id. otherwise the cj spec is useless.
            //we might think of open the code at some place to treat another property as id.
            IdentifierType = entityType.GetProperty("Id",
                BindingFlags.IgnoreCase
                | BindingFlags.Instance
                | BindingFlags.Public)
                .PropertyType;
            
            ItemLinks = new List<RouteInfo>();
            Links = new List<RouteInfo>();
            Queries = new List<RouteInfo>();
        }


        /*Properties*/
        public Type EntityType { get; private set; }
        
        public Type IdentifierType { get; private set; }

        public RouteInfo Create { get; set; }
        
        public RouteInfo Delete { get; set; }
        
        public RouteInfo Item { get; set; }
        
        public IList<RouteInfo> ItemLinks { get; private set; }

        public IList<RouteInfo> Links { get; private set; }
        
        public IList<RouteInfo> Queries { get; private set; }


        /*public static methods */
        public static RoutesInfo GetPublishedRoutesInfo(Type entityType)
        {
            return RoutesInfoCollection.SingleOrDefault(x => x.EntityType == entityType);
        }

        public static void PublishRoutesInfo()
        {
            var controllerTypes =
                Assembly.GetCallingAssembly().GetTypes()
                    //Find our controller types    
                    .Where(type => type.IsSubclassOf(typeof (Controller)))
                    .ToList();

            foreach (var controllerType in controllerTypes)
                PublishRoutesInfo(controllerType);
            
            var debug = RoutesInfoCollection;
        }


        /*private static methods */
        static void PublishRoutesInfo(Type controllerType)
        {
            //var controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
            var routePrefixTemplate =
                (string) controllerType.GetCustomAttributes(typeof (RoutePrefixAttribute))
                    .Select(a => "//" + a.GetType().GetRuntimeProperty("Prefix").GetValue(a))
                    .SingleOrDefault()
                ?? string.Empty;

            var methodInfos =
                controllerType.GetMethods(BindingFlags.Public
                                          | BindingFlags.Instance
                                          | BindingFlags.DeclaredOnly)
                    .Where(mi =>
                        mi.ReturnType.IsGenericType
                        && mi.ReturnType.IsSubclassOf(typeof(CollectionJsonResult)));

            foreach (var methodInfo in methodInfos)
                PublishRoutesInfo(methodInfo, routePrefixTemplate);
        }

        static void PublishRoutesInfo(MethodInfo methodInfo, string routePrefixTemplate)
        {
            var entityType = methodInfo.ReturnType.GetGenericArguments().Single();

            var routesInfo = GetPublishedRoutesInfo(entityType);
            if (routesInfo == null)
            {
                routesInfo = new RoutesInfo(entityType);
                RoutesInfoCollection.Add(routesInfo);
            }

            var createAttribute =
                methodInfo.GetCustomAttributes<RouteCollectionJsonCreateAttribute>().SingleOrDefault();
            if (createAttribute != null)
                routesInfo.Create = new RouteInfo
                                    {
                                        BaseUriTemplate = routePrefixTemplate,
                                        RelativeUriTemplate = createAttribute.Template,
                                        Render = createAttribute.Render
                                    };

            var deleteAttribute =
                methodInfo.GetCustomAttributes<RouteCollectionJsonDeleteAttribute>()
                    .SingleOrDefault();
            if (deleteAttribute != null)
                routesInfo.Delete = new RouteInfo
                                    {
                                        BaseUriTemplate = routePrefixTemplate,
                                        RelativeUriTemplate = deleteAttribute.Template,
                                        Render = deleteAttribute.Render
                                    };

            var queryAttribute =
                methodInfo.GetCustomAttributes<RouteCollectionJsonQueryAttribute>()
                    .SingleOrDefault();
            if (queryAttribute != null)
                routesInfo.Queries.Add(new RouteInfo
                                       {
                                           BaseUriTemplate = routePrefixTemplate,
                                           RelativeUriTemplate = queryAttribute.Template,
                                           Rel = queryAttribute.Rel,
                                           Render = queryAttribute.Render
                                       });

            var itemAttributes =
                methodInfo.GetCustomAttributes<RouteCollectionJsonItemAttribute>()
                    .ToList();
            var itemAttribute =
                itemAttributes.SingleOrDefault(a => a.Rel == null);
            if (itemAttribute != null)
                routesInfo.Item = new RouteInfo
                                  {
                                      BaseUriTemplate = routePrefixTemplate,
                                      RelativeUriTemplate = itemAttribute.Template
                                  };
            var itemLinkAttribute =
                itemAttributes.SingleOrDefault(a => a.Rel != null);
            if (itemLinkAttribute != null)
                routesInfo.ItemLinks.Add(new RouteInfo
                                         {
                                             BaseUriTemplate = routePrefixTemplate,
                                             RelativeUriTemplate = itemLinkAttribute.Template,
                                             Rel = itemLinkAttribute.Rel,
                                             Render = itemLinkAttribute.Render
                                         });
        }
    };


    public class RouteInfo
    {
        public string BaseUriTemplate { get; set; }
        public string RelativeUriTemplate { get; set; } //TODO (?) validate the route on consistence with type of entity (Id, Guid, etc.) and make parsable for output
        public RenderType Render { get; set; }
        public string Rel { get; set; }
    }
}