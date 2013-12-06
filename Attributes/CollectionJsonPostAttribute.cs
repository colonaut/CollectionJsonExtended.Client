using System;
using CollectionJsonExtended.Client.Attributes;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(CollectionJsonPostAttribute), "Start")]

namespace CollectionJsonExtended.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class CollectionJsonPostAttribute : Attribute
    {

        public static void Start()
        {
            
        }
        
        public CollectionJsonPostAttribute(string baseUri)
        {

        }

        
    }
}