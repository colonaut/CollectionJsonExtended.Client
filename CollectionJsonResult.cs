using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using CollectionJsonExtended.Client.Attributes;
using CollectionJsonExtended.Client.Extensions;
using CollectionJsonExtended.Core;
using Newtonsoft.Json;

namespace CollectionJsonExtended.Client
{

    public abstract class CollectionJsonResult : ActionResult
    {
        protected static readonly CollectionJsonSerializerSettings DefaultSerializerSettings;

        static CollectionJsonResult()
        {
            DefaultSerializerSettings = new CollectionJsonSerializerSettings
                                            {
                                                ConversionMethod = ConversionMethod.Entity,
                                                DataPropertyCasing = DataPropertyCasing.CamelCase,
                                                Formatting = Formatting.Indented
                                            };
        }

    }


    //CollectionJsonQueryResult

    //CollectionJsonTemplateResult

    //CollectionJsonErrorResult

    //working approach (via result) but not really nice. we cannot really find the routes easily.
    public class CollectionJsonEntityResult<TEntity> : CollectionJsonResult where TEntity : class, new()
    {
        readonly TEntity _entity;

        public CollectionJsonEntityResult(TEntity entity,
            CollectionJsonSerializerSettings serializerSettings = null) //TODO add my collection json formater here, inject? or what?
        {
            SerializerSettings = serializerSettings ?? DefaultSerializerSettings;
            _entity = entity;
        }


        public readonly CollectionJsonSerializerSettings SerializerSettings;

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var httpContext = context.HttpContext;
            var response = httpContext.Response;
            var httpMethod = httpContext.Request.HttpMethod.ToUpperInvariant();

            if (_entity != null && httpMethod == "POST")
            {
                var requestUri = httpContext.Request.Url;
                var writer = new CollectionJsonWriter<TEntity>(_entity, requestUri, SerializerSettings);
                
                response.StatusCode = (int) HttpStatusCode.OK;
                response.ContentType = "application/json"; //will be application/collection+json;
                response.Write(writer.Serialize());

                return;
            }
            
            response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }


    public class CollectionJsonResult<TEntity> : CollectionJsonResult where TEntity : class, new()
    {
        readonly TEntity _entity;
        readonly IEnumerable<TEntity> _entities; 

        /*Ctor*/
        public CollectionJsonResult(TEntity entity,
            CollectionJsonSerializerSettings serializerSettings = null) //TODO add my collection json formater here, inject? or what?
        {
            SerializerSettings = serializerSettings ?? DefaultSerializerSettings;
            _entity = entity;
        }

        public CollectionJsonResult(IEnumerable<TEntity> entities,
            CollectionJsonSerializerSettings serializerSettings = null) //TODO add my collection json formater here, inject? or what?
        {
            SerializerSettings = serializerSettings ?? DefaultSerializerSettings;
            _entities = entities;
        }


        /*Properties*/
        public readonly CollectionJsonSerializerSettings SerializerSettings;

        public CollectionJsonWriter<TEntity> Writer
        {
            get
            {
                if (_entity != null)
                    return new CollectionJsonWriter<TEntity>(_entity);

                if (_entities != null)
                    return new CollectionJsonWriter<TEntity>(_entities);

                return null;
            }
        }

        
        /*Override*/
        /// <summary>
        /// Enables processing of the result of an action method by a custom 
        /// type that inherits from the 
        /// <see cref="T:System.Web.Mvc.ActionResult"/> class.
        /// </summary>
        /// <param name="controllerContext">The controllerContext in which the result is executed. 
        /// The controllerContext information includes the controller, HTTP content, 
        /// request controllerContext, and route data.</param>
        public override void ExecuteResult(ControllerContext controllerContext)
        {
            //TODO: How to ensure System.Web is in the nuget package, that is, it MUST be required!

            if (controllerContext == null)
                throw new ArgumentNullException("controllerContext");

            

            ////TODO versuche sction conroller area schon zu parsen (new Uri?) sollte aber eher im ececute gemacht werden
            //Uri result;
            //Uri.TryCreate(routePrefixTemplate, UriKind.RelativeOrAbsolute, out result);



            return;

            //var myRouteData = controllerContext.RouteData;
            //var controllerName = myRouteData.Values["controller"];
            //foreach (var route in RouteTable.Routes.Where(r =>r.GetType() == typeof(Route)))
            //{
            //    var routeData = route.GetRouteData(controllerContext.HttpContext);
            //    if (routeData != null
            //        && routeData.Values["controller"] == controllerName)

            //        routeData.DataTokens["Foo"] = "Bar";


            //    //Add route and method name to Dictionary

            //}

            var httpContext = controllerContext.HttpContext;
            var httpMethod = httpContext.Request.HttpMethod.ToUpperInvariant();
            var response = httpContext.Response;
            
            var controllerType = controllerContext.Controller.GetType();
            var controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
            var actionDescriptor = controllerDescriptor.FindAction(controllerContext,
                controllerContext.RouteData.GetRequiredString("action"));
            
            var statusCode = HttpStatusCode.NotFound; //This is initially 404. If NO ActionSelector (and route in it in Mvc5) is set, it will always return 404 not found. So you HAVE to set the attribute in the controller.

            response.ClearHeaders();
            response.ClearContent();

            response.StatusCode = (int) HttpStatusCode.NotImplemented; //501 for now. test if our match stuff works

            return;
            
            /* Check if action selector attribute can be invoked (http method check in RouteCollectionJson attributes) */
            foreach (var actionSelector in  actionDescriptor.GetSelectors())
            {
                if (actionSelector.Invoke(controllerContext))
                {
                    statusCode = GetStatusCode(httpMethod);
                    break;
                }
            }
            response.StatusCode = (int)statusCode;

            if (response.StatusCode >= 400)
            {
                response.TrySkipIisCustomErrors = true;
                response.ContentType = "application/json"; //will be application/collection+json;
                response.Write(new CollectionJsonWriter<TEntity>(statusCode));
                return; //TODO: create an error response
            }

            //ResolveRoutes(controllerContext);
            var requestUri = httpContext.Request.Url;
            switch (statusCode)
            {
                case HttpStatusCode.Created:
                    response.AddHeader("Location", requestUri.ToString() + _entity.GetType().GetProperty("Id").GetValue(_entity)); //TODO (possible null pointer or completely change)
                    break;
                case HttpStatusCode.OK:
                    response.ContentType = "application/json"; //will be application/collection+json;
                    response.Write(Writer.Serialize());
                    break;
            }
        }

        
        /*Methods*/

        [Obsolete]
        void ResolveRoutes(ControllerContext context)
        {
            var controllerType = context.Controller.GetType();
            var controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
            

            foreach (var action in controllerDescriptor.GetCanonicalActions())
            {

                var actionName = action.ActionName;
                var actionDescriptor = controllerDescriptor.FindAction(context, actionName);
                

                // Get any attributes (filters) on the action
                //var attributes = action.GetCustomAttributes(typeof(CollectionJsonRouteAttribute), true); //this is the abstract which is currently not exisiting. we might use an interface...
                //.SingleOrDefault(a => a.GetType().GetInterface("ICollectionJsonRoute", true) != null);



              
                //var selectors = action.GetSelectors();


                //foreach (var actionSelector in selectors)
                //{
                //    actionSelector.Invoke(context);
                //}

                //var routeInfo = attributes;


            }
        }

        HttpStatusCode GetStatusCode(string httpMethod)
        {
            switch (httpMethod)
            {
                case "GET": //read
                    if (_entity != null)
                        return HttpStatusCode.OK;
                    return HttpStatusCode.NotFound;
                case "PUT": //update
                    if (_entity != null)
                        return HttpStatusCode.OK;
                    return HttpStatusCode.NotFound;
                case "POST": //create
                    if (_entity != null)
                        return HttpStatusCode.Created;
                    return HttpStatusCode.NotFound;
                case "DELETE":
                    if (_entity != null)
                        return HttpStatusCode.NoContent;
                    return HttpStatusCode.NotFound;
                case "QUERY": //read multiple
                    if (_entities != null)
                        return HttpStatusCode.OK;
                    return HttpStatusCode.NotFound;
                default:
                    return HttpStatusCode.MethodNotAllowed;
            }

            
        }

    }
}
