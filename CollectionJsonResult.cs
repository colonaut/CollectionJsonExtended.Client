using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
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
            CollectionJsonSerializerSettings serializerSettings = null)
        {
            _entity = entity;
            SerializerSettings = serializerSettings ?? DefaultSerializerSettings;
            RouteInfoCollection = SingletonFactory<UrlInfoCollection>.Instance
                .Find<RouteInfo>(_entityType);
        }

        public CollectionJsonResult(IEnumerable<TEntity> entities,
            CollectionJsonSerializerSettings serializerSettings = null)
        {
            _entities = entities;
            SerializerSettings = serializerSettings ?? DefaultSerializerSettings;
            RouteInfoCollection = SingletonFactory<UrlInfoCollection>.Instance
                .Find<RouteInfo>(_entityType);
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
            //validate route name can be found in route data
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

            CreateResponse(response, requestRouteInfo);
        }

        /*private methods*/
        private void CreateResponse(HttpResponseBase response, RouteInfo routeInfo)
        {
            switch (routeInfo.StatusCode)
            {
                case HttpStatusCode.OK:
                    if (_entity == null && _entities == null)
                    {
                        CreateErrorResponse(response, HttpStatusCode.InternalServerError, "Writer has no data");
                        return;
                    }
                    var writer = _entity != null
                        ? new CollectionJsonWriter<TEntity>(_entity)
                        : new CollectionJsonWriter<TEntity>(_entities);
                    response.ContentType = "application/json"; //will be application/collection+json;
                    response.Write(writer.Serialize());
                    break;

                case HttpStatusCode.Created:
                    if (_entity == null)
                    {
                        CreateErrorResponse(response, HttpStatusCode.InternalServerError, "Entity is null");
                        return;
                    }
                    var itemRouteInfo = RouteInfoCollection.SingleOrDefault(r => r.Kind == Is.Item);
                    if (itemRouteInfo == null)
                    {
                        CreateErrorResponse(response, HttpStatusCode.InternalServerError, "item route info");
                        return;
                    }
                    var primaryKey = itemRouteInfo.PrimaryKeyProperty.GetValue(_entity).ToString();
                    response.AddHeader("Location",
                        itemRouteInfo.VirtualPath.Replace(itemRouteInfo.PrimaryKeyTemplate, primaryKey));
                    break;

                case HttpStatusCode.NoContent:
                    break;

                default:
                    CreateErrorResponse(response,
                        HttpStatusCode.InternalServerError,
                        "Status code not supported: " + routeInfo.StatusCode);
                    return;
            }

            response.StatusCode = (int) routeInfo.StatusCode;
        }

        private void CreateErrorResponse(HttpResponseBase response, HttpStatusCode statusCode, string message)
        {
            //TODO check this all with spec. these could be real errors (http). error code could be used with content, etc...
            response.StatusCode = (int)HttpStatusCode.OK;
            response.TrySkipIisCustomErrors = true;
            //response.StatusDescription = message;
            response.ContentType = "application/json"; //will be application/collection+json;
            response.Write(new CollectionJsonWriter<TEntity>(statusCode, message));
        }
    }
}
