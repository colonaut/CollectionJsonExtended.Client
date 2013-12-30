using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Routing;
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
    [Obsolete]
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
        readonly Type _entityType = typeof(TEntity);

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

            var httpContext = controllerContext.HttpContext;
            var response = httpContext.Response;
            var routeData = controllerContext.RouteData;
            
            response.ClearHeaders();
            response.ClearContent();

            //var controllerType = controllerContext.Controller.GetType();
            //var actionName = routeData.GetRequiredString("action");
            //var controllerName = routeData.GetRequiredString("controller");
            //var controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
            
            object requestRouteName;
            if (!routeData.DataTokens.TryGetValue("RouteName", out requestRouteName))
            {
                response.StatusCode = (int) HttpStatusCode.InternalServerError;
                return;
            }

            var routeInfos = RouteInfo.GetPublishedRouteInfos(_entityType);
            var requestUri = httpContext.Request.Url;
            var requestRouteInfo = routeInfos
                .SingleOrDefault(r => r.RouteName == requestRouteName as string);

            if (requestRouteName == null)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

            var statusCode = requestRouteInfo.SuccesHttpStatusCode;


            //generate the urls
            foreach (var routeInfo in routeInfos)
            {
                var url =
                    UrlHelper.GenerateUrl(routeInfo.RouteName, //TODO cannot create routename....
                        routeInfo.ActionDescriptor.ActionName,
                        routeInfo.ActionDescriptor.ControllerDescriptor.ControllerName,
                        new RouteValueDictionary {{"id", 1}}, //TODO other param name? how?
                        RouteTable.Routes,
                        controllerContext.RequestContext,
                        true);
            }

            if (response.StatusCode >= 400)
            {
                response.TrySkipIisCustomErrors = true;
                response.ContentType = "application/json"; //will be application/collection+json;
                response.Write(new CollectionJsonWriter<TEntity>(statusCode));
                return; //TODO: create an error response
            }

            //Add content to response
            switch (statusCode)
            {
                case HttpStatusCode.Created:
                    //TODO Id property gedöns
                    response.AddHeader("Location", requestUri.ToString() + _entity.GetType().GetProperty("Id").GetValue(_entity)); //TODO (possible null pointer or completely change)
                    break;
                case HttpStatusCode.OK:
                    response.ContentType = "application/json"; //will be application/collection+json;
                    response.Write(Writer.Serialize());
                    break;
            }
        }

   

    }
}
