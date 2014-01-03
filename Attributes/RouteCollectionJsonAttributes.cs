using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
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
using System.Web.WebPages;
using WebActivatorEx;

namespace CollectionJsonExtended.Client.Attributes
{
    
    //https://aspnetwebstack.codeplex.com/wikipage?title=Attribute%20Routing%20in%20MVC%20and%20Web%20API
    //search for IDirectRoute


    public enum Render
    {
        CollectionJson,
        Rss,
        Image
    }
    public enum Is
    {
        Base,
        Item,
        Template,
        Query,
        Create,
        Delete,
        Update
    }
    //the abstract
    public abstract class CollectionJsonRouteProviderAttribute : Attribute, IDirectRouteProvider
    {
        static readonly Dictionary<string, int> GeneratedRoutNames =
            new Dictionary<string, int>();

        /* property defaults */
        private int _routeOrder = -1;
        private string _version = "1.0";

        /* ctor */
        protected CollectionJsonRouteProviderAttribute(string template)
        {
            Template = template;
        }


        public string RouteName { get; set; }

        public int RouteOrder
        {
            get { return _routeOrder; }
            set { _routeOrder = value; }
        }

        public string Version //TODO implement version constraint (inline) or! better use it in settings... probably only useful for the whole set. check spec
        {
            get { return _version; }
            set { _version = value; }
        }

        public string Relation { get; set; } //TODO think about: own attribute for links, rel, and prompt??? (make virtal if needed here)

        public string Render { get; set; }

        public virtual RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"HttpMethod", new[] {"GET"}}
                       };
            }

        }

        public virtual RouteValueDictionary DataTokens
        {
            get { return new RouteValueDictionary(); }
        }

        public virtual HttpStatusCode StatusCode
        {
            get { return HttpStatusCode.OK; }
        }
        
        public virtual Is Kind
        {
            get { return Is.Base; }
        }

       
        protected string Template { get; private set; }


        /*public methods*/
        public RouteEntry CreateRoute(DirectRouteProviderContext context)
        {
            Contract.Assert(context != null);

            ValidateTemplate(context);
            ValidateRelation(context);
            ValidateRouteName(context);

            var builder = context.CreateBuilder(Template);
            Contract.Assert(builder != null);

            builder.Name = RouteName;
            builder.Order = RouteOrder;

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
            builder.DataTokens.Add("RouteName", RouteName);

            var buildResult = builder.Build();

            PublishRoute(builder);
            
            return buildResult;
        }

        /*private methods*/
        void ValidateTemplate(DirectRouteProviderContext context)
        {
            if ((Kind != Is.Item && Kind != Is.Delete)
                || !string.IsNullOrWhiteSpace(Template))
                return;
            var actionDescriptor =
                context.Actions.Single(); //throws if not unique
            var templates =
                actionDescriptor.GetParameters().Select(parameterDescriptor =>
                    "{" + parameterDescriptor.ParameterName + "}");
            Template = string.Join("/", templates);
        }

        void ValidateRelation(DirectRouteProviderContext context)
        {
            if ((Kind != Is.Query && Kind != Is.Delete && Kind != Is.Create)
                || !string.IsNullOrEmpty(Relation))
                return;
            var actionDescriptor =
                context.Actions.Single(); //throws if not unique
            Relation = Kind.ToString().ToLowerInvariant()
                + "." + actionDescriptor.ActionName.ToLowerInvariant();
        }

        void ValidateRouteName(DirectRouteProviderContext context)
        {
            if (RouteName != null)
                return;

            var actionDescriptor = context.Actions.Single(); //this throws if not unique!
            var routeName = actionDescriptor.ControllerDescriptor.ControllerName
                            + "." + actionDescriptor.ActionName;

            if (GeneratedRoutNames.ContainsKey(routeName))
            {
                RouteName = routeName + (++GeneratedRoutNames[routeName]);
                return;
            }

            GeneratedRoutNames.Add(routeName, 0);
            RouteName = routeName;
        }

        void PublishRoute(DirectRouteBuilder builder)
        {
            var actionDescriptor = builder.Actions.Single();
            var methodInfo = ((ReflectedActionDescriptor)actionDescriptor).MethodInfo;
            var entityType = methodInfo.ReturnType.GetGenericArguments().Single(); //this will break, if not exactly one generic argument (the entity type) is given

            var routeInfo = new RouteInfo
            {
                EntityType = entityType,
                HttpStatusCode = StatusCode,
                RouteName = RouteName,
                ActionDescriptor = actionDescriptor,
                Template = Template,
                Kind = Kind,
                Relation = Relation
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

    }

    //approach 2 (better)
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CollectionJsonRouteAttribute : CollectionJsonRouteProviderAttribute
    {
        private HttpStatusCode _statusCode;
        private Is _kind;
        private string _httpMethod;


        public CollectionJsonRouteAttribute(Is kind)
            : base(string.Empty)
        {
            Construct(kind);
        }

        public CollectionJsonRouteAttribute(Is kind, string template)
            : base(template)
        {
            Construct(kind);
        }


        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"HttpMethod", new HttpMethodConstraint(new[] {_httpMethod})}
                       };
            }
        }

        public override HttpStatusCode StatusCode
        {
            get { return _statusCode; }
        }

        public override Is Kind
        {
            get { return _kind; }
        }


        private void Construct(Is kind)
        {
            _kind = kind;
            switch (Kind)
            {
                case Is.Create:
                    _httpMethod = "POST";
                    _statusCode = HttpStatusCode.Created;
                    break;
                case Is.Delete:
                    _httpMethod = "DELETE";
                    _statusCode = HttpStatusCode.NoContent;
                    break;
                case Is.Update:
                    _httpMethod = "PUT";
                    _statusCode = HttpStatusCode.OK;
                    break;
                default:
                    _httpMethod = "GET";
                    _statusCode = HttpStatusCode.OK;
                    break;
            }
        }
    }


    public enum Do
    {
        Query,
        Create,
        Update,
        Delete
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CollectionJsonTaskAttribute : CollectionJsonRouteProviderAttribute
    {
        
        private HttpStatusCode _statusCode;
        private Do _kind;
        private string _httpMethod;

        public CollectionJsonTaskAttribute(Do kind)
            : base(string.Empty)
        {
            
            Construct(kind);
        }
        public CollectionJsonTaskAttribute(Do kind, string template)
            : base(template)
        {
            Construct(kind);
        }

        public override RouteValueDictionary Constraints
        {
            get
            {
                return new RouteValueDictionary
                       {
                           {"HttpMethod", new HttpMethodConstraint(new[] {_httpMethod})}
                       };
            }
        }

        public override HttpStatusCode StatusCode
        {
            get { return _statusCode; }
        }

        public override Is Kind
        {
            get
            {
                switch (_kind)
                {
                    case Do.Create:
                        return Attributes.Is.Create;
                    case Do.Update:
                        return Attributes.Is.Update;
                    case Do.Delete:
                        return Attributes.Is.Delete;
                    default:
                        return Attributes.Is.Query;                        
                }
            }
        }

        private void Construct(Do kind)
        {
            _kind = kind;
            switch (Kind)
            {
                case Attributes.Is.Create:
                    _httpMethod = "POST";
                    _statusCode = HttpStatusCode.Created;
                    break;
                case Attributes.Is.Delete:
                    _httpMethod = "DELETE";
                    _statusCode = HttpStatusCode.NoContent;
                    break;
                case Attributes.Is.Update:
                    _httpMethod = "PUT";
                    _statusCode = HttpStatusCode.OK;
                    break;
                default:
                    _httpMethod = "GET";
                    _statusCode = HttpStatusCode.OK;
                    break;
            }
        }
    }

    public enum As
    {
        Base,
        Item,
        Query,
        Template
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CollectionJsonGetAttribute : CollectionJsonRouteProviderAttribute
    {
        public CollectionJsonGetAttribute(string template)
            : base(template)
        {
        }
    }

}