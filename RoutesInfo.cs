using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
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
            EntityIdType = entityType.GetProperty("Id",
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
        
        public Type EntityIdType { get; private set; }

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

        public static void PublishRoutesInfo(Assembly callingAssembly)
        {
            var controllerTypes =
                callingAssembly.GetTypes()
                    //Find our controller types    
                    .Where(type => type.IsSubclassOf(typeof (Controller)))
                    .ToList();

            foreach (var controllerType in controllerTypes)
            {
                PublishRoutesInfo(controllerType);
            }

            var debug = RoutesInfoCollection;
            var debug2 = RouteTable.Routes;


        }


        /*private static methods */
        static void PublishRoutesInfo(Type controllerType)
        {
            var routePrefixTemplate =
                (string) controllerType.GetCustomAttributes(typeof (RoutePrefixAttribute))
                    .Select(a => a.GetType().GetRuntimeProperty("Prefix").GetValue(a))
                    .SingleOrDefault()
                ?? string.Empty;

            var controllerName = new ReflectedControllerDescriptor(controllerType).ControllerName;

            var methodInfos =
                controllerType.GetMethods(BindingFlags.Public
                                          | BindingFlags.Instance
                                          | BindingFlags.DeclaredOnly)
                    .Where(mi =>
                        mi.ReturnType.IsGenericType
                        && mi.ReturnType.IsSubclassOf(typeof(CollectionJsonResult)));

            foreach (var methodInfo in methodInfos)
            {
                PublishRoutesInfo(methodInfo, controllerName, routePrefixTemplate);
            }
        }

        static void PublishRoutesInfo(MethodInfo methodInfo, string controllerName, string routePrefixTemplate)
        {
            var entityType = methodInfo.ReturnType.GetGenericArguments().Single();
            var actionName = methodInfo.Name;
            
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
                                        RouteName = createAttribute.Name,
                                        ControllerName = controllerName,
                                        ActionName = actionName,
                                        Version = createAttribute.Version,
                                        Render = createAttribute.Render,

                                        BaseUriTemplate = routePrefixTemplate,
                                        RelativeUriTemplate = createAttribute.Template
                                    };

            var deleteAttribute =
                methodInfo.GetCustomAttributes<RouteCollectionJsonDeleteAttribute>()
                    .SingleOrDefault();
            if (deleteAttribute != null)
                routesInfo.Delete = new RouteInfo
                                    {
                                        RouteName = deleteAttribute.Name,
                                        ControllerName = controllerName,
                                        ActionName = actionName,
                                        Version = deleteAttribute.Version,
                                        Render = deleteAttribute.Render,
                                        
                                        BaseUriTemplate = routePrefixTemplate,
                                        RelativeUriTemplate = deleteAttribute.Template
                                    };

            var queryAttribute =
                methodInfo.GetCustomAttributes<RouteCollectionJsonQueryAttribute>()
                    .SingleOrDefault();
            if (queryAttribute != null)
                routesInfo.Queries.Add(new RouteInfo
                                       {
                                           RouteName = queryAttribute.Name,
                                           ControllerName = controllerName,
                                           ActionName = actionName,
                                           Version = queryAttribute.Version,
                                           Rel = queryAttribute.Rel,
                                           Render = queryAttribute.Render,

                                           BaseUriTemplate = routePrefixTemplate,
                                           RelativeUriTemplate = queryAttribute.Template
                                       });

            var itemAttributes =
                methodInfo.GetCustomAttributes<RouteProviderCollectionJsonItemAttribute>()
                    .ToList();
            var itemAttribute =
                itemAttributes.SingleOrDefault(a => a.Rel == null);
            if (itemAttribute != null)
            {
                routesInfo.Item = new RouteInfo
                                  {
                                      RouteName = itemAttribute.Name,
                                      ControllerName = controllerName,
                                      ActionName = actionName,
                                      Version = itemAttribute.Version,

                                      BaseUriTemplate = routePrefixTemplate,
                                      RelativeUriTemplate = itemAttribute.Template
                                  };
            }
            var itemLinkAttribute =
                itemAttributes.SingleOrDefault(a => a.Rel != null);
            if (itemLinkAttribute != null)
                routesInfo.ItemLinks.Add(new RouteInfo
                                         {
                                             RouteName = itemLinkAttribute.Name,
                                             ControllerName = controllerName,
                                             ActionName = actionName,
                                             Version = itemLinkAttribute.Version,
                                             Rel = itemLinkAttribute.Rel,
                                             Render = itemLinkAttribute.Render,

                                             BaseUriTemplate = routePrefixTemplate,
                                             RelativeUriTemplate = itemLinkAttribute.Template                                             
                                         });
        }
    };


    public class RouteInfo
    {
        public string RouteName { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public RenderType Render { get; set; }
        public string Rel { get; set; }
        public string Version { get; set; }
        //still needed?
        public string BaseUriTemplate { get; set; }
        public string RelativeUriTemplate { get; set; } //TODO (?) validate the route on consistence with type of entity (Id, Guid, etc.) and make parsable for output
    }
}