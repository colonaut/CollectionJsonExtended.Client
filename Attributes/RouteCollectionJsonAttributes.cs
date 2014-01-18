using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Routing;
using System.Web.Routing;
using CollectionJsonExtended.Core;
using CollectionJsonExtended.Core.Attributes;

namespace CollectionJsonExtended.Client.Attributes
{
    
    //https://aspnetwebstack.codeplex.com/wikipage?title=Attribute%20Routing%20in%20MVC%20and%20Web%20API
    //search for IDirectRoute

    //the abstract
    public abstract class CollectionJsonRouteProviderAttribute : Attribute, IDirectRouteProvider
    {
        static readonly Dictionary<string, int> GeneratedRoutNames =
            new Dictionary<string, int>();

        /* property defaults */
        int _routeOrder = -1;
        string _version = "1.0";

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

            var actionDescriptor = context.Actions.Single(); //throws if nor unique exists
            ValidateTemplate(actionDescriptor);
            ValidateRelation(actionDescriptor);
            ValidateRouteName(actionDescriptor);

            var builder = context.CreateBuilder(Template);
            Contract.Assert(builder != null);

            builder.Name = RouteName;
            builder.Order = RouteOrder;

            if (builder.Constraints == null)
                builder.Constraints = new RouteValueDictionary();
            
            foreach (var constraint in Constraints)
                builder.Constraints.Add(constraint.Key, constraint.Value);
            
            if (builder.DataTokens == null)
                builder.DataTokens = new RouteValueDictionary();
            
            foreach (var dataToken in DataTokens)
                builder.DataTokens.Add(dataToken.Key, dataToken.Value);
            

            if (DataTokens.ContainsKey("RouteName"))
                DataTokens.Remove("RouteName");
            builder.DataTokens.Add("RouteName", RouteName);

            CreateRouteInfo(builder).Publish();

            return builder.Build();
        }
        
        public RouteInfo CreateRouteInfo(DirectRouteBuilder builder)
        {
            var actionDescriptor = builder.Actions.Single();
            var methodInfo = ((ReflectedActionDescriptor)actionDescriptor).MethodInfo;
            var entityType = methodInfo.ReturnType.GetGenericArguments().Single(); //this will break, if not exactly one generic argument (the entity type) is given
            
            var routeInfo = new RouteInfo(entityType)
            {
                //Params = methodInfo.GetParameters(),
                VirtualPath = builder.Template,
                Kind = Kind,
                Relation = Relation,
                StatusCode = StatusCode,
                RouteName = RouteName
            };

            object httpMethodConstraint;
            if (Constraints.TryGetValue("HttpMethod", out httpMethodConstraint))
            {
                var casted = httpMethodConstraint as HttpMethodConstraint;
                if (casted != null)
                    routeInfo.AllowedMethods = casted.AllowedMethods;
            }

            if (Kind == Is.Item || Kind == Is.Delete)
            {
                routeInfo.PrimaryKeyProperty = GetValidatedPrimaryKeyProperty(actionDescriptor,
                    methodInfo,
                    entityType);

                routeInfo.PrimaryKeyTemplate =
                    string.Format("{{{0}}}",
                        Regex.Match(builder.Template, @"\{([^)]*)\}").Groups[1].Value);
            }

            if (Kind == Is.Query)
                routeInfo.QueryParams = methodInfo.GetParameters();
        
            return routeInfo;
        }


        /*private methods*/
        PropertyInfo GetPrimaryKeyProperty(Type entityType)
        {
            var primaryKeyProperty = entityType
                .GetProperty("Id", BindingFlags.Instance
                                   | BindingFlags.IgnoreCase
                                   | BindingFlags.Public)
            ?? entityType.GetProperties()
                    .SingleOrDefault(p =>
                    {
                        var a = p.GetCustomAttribute<CollectionJsonPropertyAttribute>();
                        return a != null && a.IsPrimaryKey;
                    });

            if (primaryKeyProperty == null
                || !primaryKeyProperty.CanRead) // TODO check for possible types also
                throw new NullReferenceException(string.Format(
                    "The entity {0} does not have an unique identifier." +
                    " Either create a public get Id property" +
                    " or set CollectionJsonPropertyAttribute[IsPrimaryKey = true]" +
                    " on exactly 1 public get property",
                    entityType.FullName));

            return primaryKeyProperty;
        }

        PropertyInfo GetValidatedPrimaryKeyProperty(ActionDescriptor actionDescriptor, MethodBase methodInfo, Type entityType)
        {
            var primaryKeyProperty = GetPrimaryKeyProperty(entityType);

            var identifierParam = methodInfo.GetParameters().SingleOrDefault();

            if (identifierParam == null)
                throw new ArgumentNullException(string.Format(
                    "Method {0}.{1} should have exactly 1 param pointing on the entity's primary key",
                    ((methodInfo.DeclaringType != null) ? methodInfo.DeclaringType.Name : ""),
                    methodInfo.Name));

            if (identifierParam.ParameterType.FullName != primaryKeyProperty.PropertyType.FullName)
                throw new TypeAccessException(string.Format(
                    "Type of method param ({0})" +
                    " does not match type of entity's primary key ({1})",
                    identifierParam.ParameterType.FullName,
                    primaryKeyProperty.PropertyType.FullName));

            return primaryKeyProperty;
        }

        void ValidateTemplate(ActionDescriptor actionDescriptor)
        {
            //TODO we must throw, if we find more than one entity. but this should is done in core
            if ((Kind != Is.Item && Kind != Is.Delete)
                || !string.IsNullOrWhiteSpace(Template))
                return;

            //create Template (primary key only), if it doesn't exist (should be alway true, when Is.Create)
            var templates =
                actionDescriptor.GetParameters().Select(parameterDescriptor =>
                    "{" + parameterDescriptor.ParameterName + "}");
            Template =
                string.Join("/", templates);

        }

        void ValidateRelation(ActionDescriptor actionDescriptor)
        {
            if ((Kind != Is.Query && Kind != Is.Delete && Kind != Is.Create)
                || !string.IsNullOrEmpty(Relation))
                return;

            Relation = Kind.ToString().ToLowerInvariant()
                + "." + actionDescriptor.ActionName.ToLowerInvariant();
        }

        void ValidateRouteName(ActionDescriptor actionDescriptor)
        {
            if (RouteName != null)
                return;

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

    }

    //approach 1 (Is.xyz)
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
                //case Is.Query:
                //    _httpMethod = "QUERY"; //we do not use query but standard implementations of HTTP and query via get
                //    _statusCode = HttpStatusCode.OK;
                //    break;
                default:
                    _httpMethod = "GET";
                    _statusCode = HttpStatusCode.OK;
                    break;
            }
        }
    }

    //approach 2 (Do.xyz, As.xyz) both work, offer both or skip one. Is is used in Core now. Which is good :)
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
                        return Is.Create;
                    case Do.Update:
                        return Is.Update;
                    case Do.Delete:
                        return Is.Delete;
                    default:
                        return Is.Query;                        
                }
            }
        }

        private void Construct(Do kind)
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