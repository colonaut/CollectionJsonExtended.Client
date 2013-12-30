using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Web.Mvc;
using System.Web.Mvc.Routing;
using System.Web.Routing;
//[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(CollectionJsonPostAttribute), "Start")]
using WebActivatorEx;

namespace CollectionJsonExtended.Client.Attributes
{

    //https://aspnetwebstack.codeplex.com/wikipage?title=Attribute%20Routing%20in%20MVC%20and%20Web%20API
    //search for IDirectRoute

    //TRY THIS
    //public abstract class RouteProviderAttribute : Attribute
    //{
    //    protected RouteProviderAttribute(string template) { }

    //    public string Template { get; }
    //    public string Name { get; set; }
    //    public int Order { get; set; }
    //    public virtual RouteValueDictionary Constraints { get; }
    //}


    public abstract class MyRouteProviderAttribute : Attribute, IDirectRouteProvider
    {
        
        static readonly Dictionary<string, int> GeneratedRoutNames =
            new Dictionary<string, int>();

        private int _order = -1;
        private string _version = "1.0";
        private string _name;

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
        
        //TODO test if all that routing shit works...

        //TODO now try to publish the needed information directly, when route is created

        //TODO if works ude this as base for implementstions of attributes and scan for thid in publish,attributes must implement IRouteProvider then (should also work without mapmvc then vur needs consistent routess and names it's not separated then...)
        protected MyRouteProviderAttribute()
            : this(string.Empty)
        {

        }

        protected MyRouteProviderAttribute(string template)
        {
            Template = template;
        }
        

        public string Template { get; private set; }

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


        public RouteEntry CreateRoute(DirectRouteProviderContext context)
        {
            DirectRouteBuilder builder = context.CreateBuilder(Template);
            
            if (_name == null)
                _name = GetGeneratedRoutName(builder);
            
            builder.Name = _name;
            builder.Order = Order;

            if (builder.Constraints == null)
                builder.Constraints = new RouteValueDictionary();
            
            foreach (var constraint in Constraints)
            {
                builder.Constraints.Add(constraint.Key, constraint.Value);    
            }


            var reflectedActionDescriptor = (ReflectedActionDescriptor)builder.Actions.Single();
            var methodInfo = reflectedActionDescriptor.MethodInfo;
            var entityType = methodInfo.ReturnType.GetGenericArguments().Single();


            var buildResult = builder.Build();
            return buildResult;
        }
    }



    //END TRY THIS

    //RouteCollectionJsonQueryAttribute (via url params retrieve a collection, no param returns all, if implemented)
    //RouteCollectionJsonQueriesAttribute //returns the top level queries array
    //RouteCollectionJsonTemplateAttribute

    //the abstract attribute
    public abstract class RouteCollectionJsonAttribute : MyRouteProviderAttribute, IRouteInfoProvider
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
    public sealed class RouteProviderCollectionJsonItemAttribute : RouteCollectionJsonAttribute
    {
        public RouteProviderCollectionJsonItemAttribute(string template)
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