using System;
using System.Runtime.InteropServices;
using System.Web.Mvc.Routing;
using System.Web.Routing;
//[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(CollectionJsonPostAttribute), "Start")]

namespace CollectionJsonExtended.Client.Attributes
{

    public class CollectionJsonRouteDataToken
    {
        public string Version { get; set; }
        public Type EntityType { get; set; }
    }

    //RouteCollectionJsonQueryAttribute (via url params retrieve a collection, no param returns all, if implemented)
    //RouteCollectionJsonQueriesAttribute //returns the top level queries array
    //RouteCollectionJsonTemplateAttribute

    //the abstract attribute
    public abstract class RouteCollectionJsonAttribute : RouteProviderAttribute, IRouteInfoProvider
    {
        
        protected RouteCollectionJsonAttribute(string template)
            : base(template)
        {
            
        }

        protected string Version = "1.0";
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    //post/get/query/etc. should only support the param. (they use route prefix, or incoming controller request uri, if not set)
    public sealed class RouteCollectionJsonBaseAttribute : RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonBaseAttribute(string template)
            : base(template)
        {
            
        }

        
        public RenderType Render
        {
            get { return RenderType.Json; }
        }


        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"httpMethod", new HttpMethodConstraint(new[] {"GET"})}
                       };

            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonItemAttribute : RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonItemAttribute(string template)
            : base(template)
        {
            
        }

        
        public string Rel = null;
        public RenderType Render = RenderType.Json;


        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"httpMethod", new HttpMethodConstraint(new[] {"GET"})}
                       };

            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonCreateAttribute : RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonCreateAttribute(string template)
            : base(template)
        {
            
        }

        public RenderType Render
        {
            get { return RenderType.HttpResponse; }
        }
        
        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"httpMethod", new HttpMethodConstraint(new[] {"POST"})}
                       };

            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonDeleteAttribute : RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonDeleteAttribute(string template)
            : base(template)
        {

        }


        public RenderType Render
        {
            get { return RenderType.HttpResponse; }
        }


        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"httpMethod", new HttpMethodConstraint(new[] {"DELETE"})}
                       };

            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonQueryAttribute : RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonQueryAttribute(string template,
            string rel) : base(template)
        {
            Rel = rel;
        }

        
        public string Rel { get; set; }
        public RenderType Render = RenderType.Json;

        
        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"httpMethod", new HttpMethodConstraint(new[] {"QUERY"})}
                       };

            }
        }
    }

    

}