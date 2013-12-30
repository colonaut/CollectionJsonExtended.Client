using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Web.Mvc;
using System.Web.Mvc.Routing;
using System.Web.Routing;
//[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(CollectionJsonPostAttribute), "Start")]
using System.Web.UI.HtmlControls;
using WebActivatorEx;

namespace CollectionJsonExtended.Client.Attributes
{

    //https://aspnetwebstack.codeplex.com/wikipage?title=Attribute%20Routing%20in%20MVC%20and%20Web%20API
    //search for IDirectRoute

    //TRY THIS //we probably do not need the IRouteInfoProvider.... (we go with direct route provider....)
    //public abstract class RouteProviderAttribute : Attribute
    //{
    //    protected RouteProviderAttribute(string template) { }

    //    public string Template { get; }
    //    public string Name { get; set; }
    //    public int Order { get; set; }
    //    public virtual RouteValueDictionary Constraints { get; }
    //}


    public abstract class CollectionJsonRouteProviderAttribute : Attribute, IDirectRouteProvider
    {
        //TODO test if all that routing shit works (the parsing of the you know constraints...)...
        //TODO now try to publish the needed information directly, when route is created

        static readonly Dictionary<string, int> GeneratedRoutNames =
            new Dictionary<string, int>();

        private int _order = -1;
        private string _version = "1.0";
        private string _name;


        protected CollectionJsonRouteProviderAttribute()
            : this(string.Empty)
        {

        }

        protected CollectionJsonRouteProviderAttribute(string template)
        {
            Template = template;
        }
        

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int Order
        {
            get { return _order; }
            set { _order = value; }
        }

        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }


        public virtual RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary();

            }

        }

        public virtual RouteValueDictionary DataTokens
        {
            get
            {
                return new RouteValueDictionary();
            }
        }

        public virtual HttpStatusCode SuccessStatusCode
        {
            get { return HttpStatusCode.OK; }
        }


        protected string Template { get; private set; }

        protected string Rel { get; set; } //TODO think about: own attribute for links, rel, and prompt??? (make virtal if needed here)

        protected string Render //TODO: make virtual if needed here
        {
            get { return "implement render information on base of return type (? does this work?)"; }
        }


        public RouteEntry CreateRoute(DirectRouteProviderContext context)
        {
            var builder = context.CreateBuilder(Template);
            
            if (_name == null)
                _name = GetGeneratedRoutName(builder);
            
            builder.Name = _name;
            builder.Order = Order;

            if (builder.Constraints == null)
                builder.Constraints = new RouteValueDictionary();
            
            foreach (var constraint in Constraints)            {
                builder.Constraints.Add(constraint.Key, constraint.Value);
            }

            if (builder.DataTokens == null)
                builder.DataTokens = new RouteValueDictionary();
            foreach (var dataToken in DataTokens)
            {
                builder.DataTokens.Add(dataToken.Key, dataToken.Value);
            }
            if (DataTokens.ContainsKey("RouteName"))
                DataTokens.Remove("RouteName");
            builder.DataTokens.Add("RouteName", _name);

            var buildResult = builder.Build();

            PublishRouteInfo(builder);
            
            return buildResult;
        }


        void PublishRouteInfo(DirectRouteBuilder builder)
        {
            var actionDescriptor = builder.Actions.Single();
            
            var reflectedActionDescriptor = (ReflectedActionDescriptor)actionDescriptor;
            var methodInfo = reflectedActionDescriptor.MethodInfo;
            var entityType = methodInfo.ReturnType.GetGenericArguments().Single();

            var routeInfo = new RouteInfo
            {
                EntityType = entityType,
                SuccesHttpStatusCode = SuccessStatusCode,
                ActionDescriptor = actionDescriptor,
                RouteName = _name
            };

            object httpMethodConstraint;
            if (Constraints.TryGetValue("HttpMethod", out httpMethodConstraint))
            {
                var casted = httpMethodConstraint as HttpMethodConstraint;
                if (casted != null)
                    routeInfo.AllowedMethods = casted.AllowedMethods;
            }
            

            RouteInfo.Publish(routeInfo);
        }

        static string GetGeneratedRoutName(DirectRouteBuilder builder)
        {
            var actionDescriptor = builder.Actions.Single();
            var routeName = actionDescriptor.ControllerDescriptor.ControllerName
                            + "." + actionDescriptor.ActionName;
            if (GeneratedRoutNames.ContainsKey(routeName))
                return routeName + (++GeneratedRoutNames[routeName]);

            GeneratedRoutNames.Add(routeName, 0);
            return routeName;
        }

    }



    //END TRY THIS

    //RouteCollectionJsonQueryAttribute (via url params retrieve a collection, no param returns all, if implemented)
    //RouteCollectionJsonQueriesAttribute //returns the top level queries array
    //RouteCollectionJsonTemplateAttribute

    //the old abstract attribute
    public abstract class RouteCollectionJsonAttribute : RouteProviderAttribute, IRouteInfoProvider//CollectionJsonRouteProviderAttribute
    {

        public static Dictionary<string, List<RouteCollectionJsonAttribute>> _debugInstancesDictionary =
            new Dictionary<string, List<RouteCollectionJsonAttribute>>();  
        
        protected RouteCollectionJsonAttribute(string template)
            : base(template)
        {
            if (!_debugInstancesDictionary.ContainsKey(template))
                _debugInstancesDictionary.Add(template, new List<RouteCollectionJsonAttribute>());

            _debugInstancesDictionary[template].Add(this);

            var x = _debugInstancesDictionary[template];
        }

    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonBaseAttribute : CollectionJsonRouteProviderAttribute//RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonBaseAttribute(string template)
            : base(template)
        {
            
        }

        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"HttpMethod", new HttpMethodConstraint(new[] {"GET"})}
                       };

            }
        }

        public override HttpStatusCode SuccessStatusCode { get { return base.SuccessStatusCode; } }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CollectionJsonItemRouteAttribute : CollectionJsonRouteProviderAttribute//RouteCollectionJsonAttribute
    {
        public CollectionJsonItemRouteAttribute(string template)
            : base(template)
        {
                
        }
        
        public CollectionJsonItemRouteAttribute(string template, string rel)
            : base(template)
        {
            Rel = rel;
        }
        

        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"HttpMethod", new HttpMethodConstraint(new[] {"GET"})}
                       };

            }
        }

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonCreateAttribute : CollectionJsonRouteProviderAttribute//RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonCreateAttribute(string template)
            : base(template)
        {
            
        }

        
        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"HttpMethod", new HttpMethodConstraint(new[] {"POST"})}
                       };

            }
        }

        public override HttpStatusCode SuccessStatusCode
        {
            get { return HttpStatusCode.Created; }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonDeleteAttribute : CollectionJsonRouteProviderAttribute//RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonDeleteAttribute(string template)
            : base(template)
        {
            
        }


        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"HttpMethod", new HttpMethodConstraint(new[] {"DELETE"})}
                       };

            }
        }

        public override HttpStatusCode SuccessStatusCode
        {
            get { return HttpStatusCode.NoContent; }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonQueryAttribute : CollectionJsonRouteProviderAttribute//RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonQueryAttribute(string template,
            string rel) : base(template)
        {
            Rel = rel;            
        }

        
        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"HttpMethod", new HttpMethodConstraint(new[] {"QUERY"})}
                       };

            }
        }
    }

}