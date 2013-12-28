using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Routing;
using System.Web.Routing;

namespace CollectionJsonExtended.Client.Extensions
{
    public static class CollectionJsonRouteExtensions
    {

        public static void PublishCollectionJsonAttributeRoutes(this RouteCollection routes)
        {
            RoutesInfo.PublishRoutesInfo();
        }



    }

}