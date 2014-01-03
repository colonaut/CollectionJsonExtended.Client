using System;
using System.Collections.Generic;
using System.Web.Configuration;

namespace CollectionJsonExtended.Client.Services
{
    public class TypeResolver
    {
        
        public static Type ToType(string simpleTypeName)
        {
            simpleTypeName = simpleTypeName.Trim().ToLower();

            bool isArray = false, isNullable = false;

            if (simpleTypeName.IndexOf("[]", System.StringComparison.Ordinal) != -1)
            {
                isArray = true;
                simpleTypeName = simpleTypeName.Remove(simpleTypeName.IndexOf("[]", System.StringComparison.Ordinal), 2);
            }

            if (simpleTypeName.IndexOf("?", System.StringComparison.Ordinal) != -1)
            {
                isNullable = true;
                simpleTypeName = simpleTypeName.Remove(simpleTypeName.IndexOf("?", System.StringComparison.Ordinal), 1);
            }

            string parsedTypeName = null;
            #region switchLookupSystemtypes
            switch (simpleTypeName)
            {
                case "bool":
                case "boolean":
                    parsedTypeName = "System.Boolean";
                    break;
                case "byte":
                    parsedTypeName = "System.Byte";
                    break;
                case "char":
                    parsedTypeName = "System.Char";
                    break;
                case "datetime":
                    parsedTypeName = "System.DateTime";
                    break;
                case "datetimeoffset":
                    parsedTypeName = "System.DateTimeOffset";
                    break;
                case "decimal":
                    parsedTypeName = "System.Decimal";
                    break;
                case "double":
                    parsedTypeName = "System.Double";
                    break;
                case "float":
                    parsedTypeName = "System.Single";
                    break;
                case "int16":
                case "short":
                    parsedTypeName = "System.Int16";
                    break;
                case "int32":
                case "int":
                    parsedTypeName = "System.Int32";
                    break;
                case "int64":
                case "long":
                    parsedTypeName = "System.Int64";
                    break;
                case "object":
                    parsedTypeName = "System.Object";
                    break;
                case "sbyte":
                    parsedTypeName = "System.SByte";
                    break;
                case "string":
                    parsedTypeName = "System.String";
                    break;
                case "timespan":
                    parsedTypeName = "System.TimeSpan";
                    break;
                case "uint16":
                case "ushort":
                    parsedTypeName = "System.UInt16";
                    break;
                case "uint32":
                case "uint":
                    parsedTypeName = "System.UInt32";
                    break;
                case "uint64":
                case "ulong":
                    parsedTypeName = "System.UInt64";
                    break;
            }
            #endregion

            if (parsedTypeName != null)
            {
                if (isArray)
                {
                    parsedTypeName = parsedTypeName + "[]";
                }

                if (isNullable)
                {
                    parsedTypeName = String.Concat("System.Nullable`1[", parsedTypeName, "]");
                }

                return Type.GetType(parsedTypeName, true, false);
            }

            return Type.GetType(simpleTypeName, true, true);
        }

        public static string ToFriendlyTypeName(Type type)
        {
            
            switch (type.Name)
            {
                case "System.Boolean":
                    return "bool";
                case "System.DateTime":
                    return "datetime";
                case "System.Int32":
                    return "int";
                case "System.Int64":
                    return "long";
                case "System.Double":
                case "System.Decimal":
                case "System.Float":
                case "System.Guid":
                    return type.Name.Substring(7).ToLowerInvariant();
            }
            return null;
        }
    }
}