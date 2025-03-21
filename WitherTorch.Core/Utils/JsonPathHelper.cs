﻿using System;
using System.Text.Json.Nodes;

namespace WitherTorch.Core.Utils
{
    internal static class JsonPathHelper
    {
        /*
         * Path grammar:
         * x.y = select y in x
         * x.y[0] = select index 0 of y in x
         */
        public static JsonNode? GetNodeFromPath(JsonObject? obj, string? path)
        {
            if (obj is null || obj.Count <= 0)
                return null;
            if (string.IsNullOrEmpty(path))
                return obj;
            ReadOnlySpan<char> pathSpan = path.AsSpan();
            JsonNode? currentNode = obj;
            do
            {
                int indexOf = pathSpan.IndexOf('.');
                if (indexOf < 0)
                    return DecodeSubPath(pathSpan, currentNode);
                currentNode = DecodeSubPath(pathSpan.Slice(0, indexOf), currentNode);
                if (currentNode is null)
                    return null;
                pathSpan = pathSpan.Slice(indexOf + 1);
            }
            while (!pathSpan.IsEmpty);
            return null;
        }

        public static void SetNodeFromPath(JsonObject? obj, string? path, JsonNode value)
        {
            if (obj is null || string.IsNullOrEmpty(path))
                return;
            ReadOnlySpan<char> pathSpan = path.AsSpan();
            JsonNode? currentNode = obj;
            do
            {
                int indexOf = pathSpan.IndexOf('.');
                if (indexOf < 0)
                {
                    DecodeSubPathAndCreateOrSet(pathSpan, currentNode, Either.Right<Func<JsonNode>, JsonNode>(value));
                    return;
                }
                currentNode = DecodeSubPathAndCreateOrSet(pathSpan.Slice(0, indexOf), currentNode,
                    Either.Left<Func<JsonNode>, JsonNode>(() => new JsonObject()));
                if (currentNode is null)
                    return;
                pathSpan = pathSpan.Slice(indexOf + 1);
            }
            while (!pathSpan.IsEmpty);
        }

        private static JsonNode? DecodeSubPath(ReadOnlySpan<char> pathSpan, JsonNode? node)
        {
            if (node is not JsonObject obj || obj.Count <= 0)
                return null;
            int bracketIndex = pathSpan.IndexOf('[');
            if (bracketIndex < 0) //Is not an array
                return obj[pathSpan.ToString()];
            node = obj[pathSpan.Slice(0, bracketIndex).ToString()];
            if (node is not JsonArray array) //Invalid input
                return null;
            int rightBracketIndex = pathSpan.IndexOf(']');
            if (rightBracketIndex < bracketIndex ||
                !int.TryParse(pathSpan.Slice(bracketIndex + 1, rightBracketIndex - bracketIndex - 1).ToString(), out int index))
                return null; //Invalid input
            if (index < 0) //Use reverse index
                return array[array.Count + index];
            return array[index]; //Use forward index
        }

        private static JsonNode? DecodeSubPathAndCreateOrSet(ReadOnlySpan<char> pathSpan, JsonNode? node, EitherStruct<Func<JsonNode>, JsonNode> newNode)
        {
            if (node is not JsonObject obj)
                return null;
            int bracketIndex = pathSpan.IndexOf('[');
            string path;
            if (bracketIndex < 0) //Is not an array
            {
                path = pathSpan.ToString();
                JsonNode? result;
                if (newNode.IsRight)
                {
                    result = newNode.Right;
                    obj[path] = result;
                    return result;
                }
                result = obj[path];
                if (result is null)
                {
                    result = newNode.Left.Invoke();
                    obj[path] = result;
                }
                return result;
            }
            int rightBracketIndex = pathSpan.IndexOf(']');
            if (rightBracketIndex < bracketIndex ||
                !int.TryParse(pathSpan.Slice(bracketIndex + 1, rightBracketIndex - bracketIndex - 1).ToString(), out int index))
                return null; //Invalid input
            path = pathSpan.Slice(0, bracketIndex).ToString();
            int count;
            if (obj[path] is JsonArray array)
            {
                count = array.Count;
            }
            else //Invalid input
            {
                array = new JsonArray();
                obj[path] = array;
                count = 0;
            }
            if (index < 0) //Use reverse index
            {
                index = count + index;
                if (index < 0)
                {
                    if (newNode.IsLeft)
                        node = newNode.Left.Invoke();
                    else
                        node = newNode.Right;
                    array.Insert(0, node);
                    return node;
                }
                if (newNode.IsRight)
                {
                    node = newNode.Right;
                    array[index] = node;
                    return node;
                }
                return array[index];
            }
            //Use forward index
            if (index < count)
            {
                if (newNode.IsRight)
                {
                    node = newNode.Right;
                    array[index] = node;
                    return node;
                }
                return array[index];
            }
            if (newNode.IsLeft)
                node = newNode.Left.Invoke();
            else
                node = newNode.Right;
            array.Add(node);
            return node;
        }
    }
}
