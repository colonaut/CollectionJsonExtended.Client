using System;
using System.Collections.Generic;
using System.Net;
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
        /// <param name="context">The context in which the result is executed. 
        /// The context information includes the controller, HTTP content, 
        /// request context, and route data.</param>
        public override void ExecuteResult(ControllerContext context)
        {
            //TODO: How to ensure System.Web is in the nuget package, that is, it MUST be required!

            if (context == null)
                throw new ArgumentNullException("context");


            var httpMethod = context.HttpContext.Request.HttpMethod.ToUpperInvariant();

            var statusCode = GetStatusCode(httpMethod);

            var response = context.HttpContext.Response;

            var requestUri = context.HttpContext.Request.Url;

            //TODO we need BaseUri
            //"/some/url/id/" + _entity.GetType().GetProperty("Id").GetValue(_entity) goes to collection href with id for entity
            //"/some/url/id/" goes to collection href with id for entities


            response.ClearHeaders();
            response.ClearContent();

            response.StatusCode = (int)statusCode;

            if (response.StatusCode >= 400)
            {
                response.TrySkipIisCustomErrors = true;
                response.ContentType = "application/json"; //will be application/collection+json;
                response.Write(new CollectionJsonWriter<TEntity>(statusCode));
                return; //TODO: create an error response
            }

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
        HttpStatusCode GetStatusCode(string httpMethod)
        {
            switch (httpMethod)
            {
                case "GET":
                    if (_entity != null)
                        return HttpStatusCode.OK;
                    return HttpStatusCode.NotFound;
                case "UPDATE":
                    if (_entity != null)
                        return HttpStatusCode.OK;
                    return HttpStatusCode.NotFound;
                case "POST":
                    if (_entity != null)
                        return HttpStatusCode.Created;
                    return HttpStatusCode.NotFound;
                case "DELETE":
                    if (_entity != null)
                        return HttpStatusCode.NoContent;
                    return HttpStatusCode.NotFound;
                case "QUERY":
                    if (_entities != null)
                        return HttpStatusCode.OK;
                    return HttpStatusCode.NotFound;
                default:
                    return HttpStatusCode.MethodNotAllowed;
            }

            
        }

    }
}
