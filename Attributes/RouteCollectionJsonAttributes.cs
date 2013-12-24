using System;
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

    //the abstract
    public abstract class RouteCollectionJsonAttribute : RouteProviderAttribute, IRouteInfoProvider
    {

        protected RouteCollectionJsonAttribute(string template,
            string version)
            : base(template)
        {
            Version = version;
        }
        
        protected string Version { get; set; }

        public override RouteValueDictionary DataTokens
        {
            get { return new RouteValueDictionary
                         {
                            {
                                 "CollectionJson",
                                 new CollectionJsonRouteDataToken
                                 {
                                     Version = Version
                                 }
                            }
                         };
            }
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RouteCollectionJsonCreateAttribute : RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonCreateAttribute(string template,
            string version = "1.0")
            : base(template, version)
        {
            
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
    public sealed class RouteCollectionJsonQueryAttribute : RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonQueryAttribute(string template,
            string version = "1.0")
            : base(template, version)
        {
            
        }

        
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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    //post/get/query/etc. should only support the param. (they use route prefix, or incoming controller request uri, if not set)
    public sealed class RouteCollectionJsonAttributes : RouteCollectionJsonAttribute
    {
        public RouteCollectionJsonAttributes(string template,
            string version = "1.0")
            : base(template, version)
        {
            Version = version;
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

}