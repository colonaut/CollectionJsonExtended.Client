using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
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

    public class CollectionJsonResult<TEntity> : CollectionJsonResult where TEntity : class, new()
    {
        readonly TEntity _entity;
        readonly IEnumerable<TEntity> _entities;
        readonly Type _entityType = typeof(TEntity);


        /*Ctor*/
        public CollectionJsonResult(TEntity entity,
            CollectionJsonSerializerSettings serializerSettings = null) //TODO add my collection json formater here, inject? or what?
        {
            _entity = entity;
            SerializerSettings = serializerSettings ?? DefaultSerializerSettings;
            RouteInfoCollection = Singleton<UrlInfoCollection>.Instance.Find<RouteInfo>(_entityType);
                //UrlInfoBase.Find(_entityType) as IEnumerable<RouteInfo>;
        }

        public CollectionJsonResult(IEnumerable<TEntity> entities,
            CollectionJsonSerializerSettings serializerSettings = null) //TODO add my collection json formater here, inject? or what?
        {
            _entities = entities;
            SerializerSettings = serializerSettings ?? DefaultSerializerSettings;
            RouteInfoCollection = Singleton<UrlInfoCollection>.Instance.Find<RouteInfo>(_entityType);
        }


        /*public properties*/
        public readonly CollectionJsonSerializerSettings SerializerSettings;

        /* private properties */
        private IEnumerable<RouteInfo> RouteInfoCollection { get; set; }

        /* override methods*/
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

            //var controllerType = controllerContext.Controller.GetType();
            //var actionName = routeData.GetRequiredString("action");
            //var controllerName = routeData.GetRequiredString("controller");
            //var controllerDescriptor = new ReflectedControllerDescriptor(controllerType);

            var httpContext = controllerContext.HttpContext;
            var response = httpContext.Response;
            var routeData = controllerContext.RouteData;
            
            response.ClearHeaders();
            response.ClearContent();
            //validate url is given
            var requestUrl = httpContext.Request.Url;
            if (requestUrl == null)
            {
                CreateErrorResponse(response, HttpStatusCode.InternalServerError, "request url");
                return;
            }
            //validate routee name can be found in route data
            object requestRouteName;
            if (!routeData.DataTokens.TryGetValue("RouteName", out requestRouteName))
            {
                CreateErrorResponse(response, HttpStatusCode.InternalServerError, "request route name");
                return;
            }
            //validate route info for route of request exist
            var requestRouteInfo = RouteInfoCollection
                .SingleOrDefault(r => r.RouteName == requestRouteName as string);
            if (requestRouteInfo == null)
            {
                CreateErrorResponse(response, HttpStatusCode.InternalServerError, "request route info");
                return;
            }

            //TODO: CreateResponse, CreateTemplateResponse
            /* proceed to valid response */
            var responseStatusCode = requestRouteInfo.StatusCode;
            response.StatusCode = (int)responseStatusCode;

            //Add content to response
            switch (responseStatusCode)
            {
                case HttpStatusCode.Created:
                    //TODO Id property gedöns
                    response.AddHeader("Location", requestUrl.ToString() + _entity.GetType().GetProperty("Id").GetValue(_entity));
                    break;
                case HttpStatusCode.OK:
                    response.ContentType = "application/json"; //will be application/collection+json;
                    response.Write(GetWriter().Serialize());
                    break;
            }
        }

        /*private methods*/
        private CollectionJsonWriter<TEntity> GetWriter()
        {
            if (_entity != null)
                return new CollectionJsonWriter<TEntity>(_entity);
            
            if (_entities != null)
                return new CollectionJsonWriter<TEntity>(_entities);

            return null;
        }
       
        private void CreateErrorResponse(HttpResponseBase response, HttpStatusCode statusCode, string message)
        {
            response.StatusCode = (int)statusCode;
            response.TrySkipIisCustomErrors = true;
            response.StatusDescription = message;
            response.ContentType = "application/json"; //will be application/collection+json;
            response.Write(new CollectionJsonWriter<TEntity>(statusCode, message));
        }

    }
}
