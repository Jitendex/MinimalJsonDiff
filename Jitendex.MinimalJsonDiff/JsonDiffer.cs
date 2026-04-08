/*
JsonDiffer.cs
Copyright (c) 2025-2026 Stephen Kraus
SPDX-License-Identifier: Apache-2.0

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

namespace Jitendex.MinimalJsonDiff;

public static class JsonDiffer
{
    public static string Diff<T>(T a, T b, JsonSerializerOptions? options = null) where T : class
    {
        var nodeA = JsonSerializer.SerializeToNode(a);
        var nodeB = JsonSerializer.SerializeToNode(b);
        return Diff(nodeA, nodeB, options);
    }

    public static byte[] DiffToUtf8Bytes<T>(T a, T b, JsonSerializerOptions? options = null) where T : class
    {
        var nodeA = JsonSerializer.SerializeToNode(a);
        var nodeB = JsonSerializer.SerializeToNode(b);
        return DiffToUtf8Bytes(nodeA, nodeB, options);
    }

    /// <remarks>
    /// Note that this method will mutate and effectively destroy the input JsonNodes.
    /// </remarks>
    public static string Diff(JsonNode? a, JsonNode? b, JsonSerializerOptions? options = null)
    {
        var document = new JsonPatchDocument();
        NodeDiff(a, b, document, path: string.Empty);
        return document.Serialize(options);
    }

    /// <remarks>
    /// Note that this method will mutate and effectively destroy the input JsonNodes.
    /// </remarks>
    public static byte[] DiffToUtf8Bytes(JsonNode? a, JsonNode? b, JsonSerializerOptions? options = null)
    {
        var document = new JsonPatchDocument();
        NodeDiff(a, b, document, path: string.Empty);
        return document.SerializeToUtf8Bytes(options);
    }

    private static void NodeDiff(JsonNode? a, JsonNode? b, JsonPatchDocument document, string path)
    {
        if (a is JsonObject objA && b is JsonObject objB)
        {
            ObjectDiff(objA, objB, document, path);
        }
        else if (a is JsonArray arrA && b is JsonArray arrB)
        {
            ArrayDiff(arrA, arrB, document, path);
        }
        else if (!JsonNode.DeepEquals(a, b))
        {
            document.Test(path, a);
            document.Replace(path, b);
        }
    }

    private static string Join<T>(this string path, T key) => $"{path}/{key}";

    private static void ObjectDiff(JsonObject a, JsonObject b, JsonPatchDocument document, string path)
    {
        var dictionaryA = JsonObjectToDictionary(a);
        var dictionaryB = JsonObjectToDictionary(b);

        foreach (var (key, nodeA) in dictionaryA)
        {
            var keyPath = path.Join(key);
            if (dictionaryB.TryGetValue(key, out var nodeB))
            {
                NodeDiff(nodeA, nodeB, document, keyPath);
            }
            else
            {
                document.Test(keyPath, nodeA);
                document.Remove(keyPath);
            }
        }

        foreach (var (key, nodeB) in dictionaryB)
        {
            if (!dictionaryA.ContainsKey(key))
            {
                document.Add(path.Join(key), nodeB);
            }
        }
    }

    /// <summary>
    /// Remove the nodes from the object to avoid the "node already has a parent"
    /// error when moving a node to the patch document.
    /// </summary>
    private static Dictionary<string, JsonNode?> JsonObjectToDictionary(JsonObject obj)
    {
        var dictionary = new Dictionary<string, JsonNode?>(obj.Count);
        foreach (var (key, node) in obj)
        {
            dictionary[key] = node;
        }
        foreach (var key in dictionary.Keys)
        {
            obj.Remove(key);
        }
        return dictionary;
    }

    private static void ArrayDiff(JsonArray a, JsonArray b, JsonPatchDocument document, string path)
    {
        // One array is empty, but the other is not empty.
        if (a.Count == 0 ^ b.Count == 0)
        {
            document.Test(path, a);
            document.Replace(path, b);
            return;
        }

        var arrayA = JsonArrayToNodeArray(a);
        var arrayB = JsonArrayToNodeArray(b);

        // Arrays are equal length, or array B is larger.
        if (arrayA.Length <= arrayB.Length)
        {
            for (int i = 0; i < arrayB.Length; i++)
            {
                var indexPath = path.Join(i);
                if (i < arrayA.Length)
                {
                    NodeDiff(arrayA[i], arrayB[i], document, indexPath);
                }
                else
                {
                    document.Add(indexPath, arrayB[i]);
                }
            }
        }

        // Array A is larger than array B.
        else
        {
            // Loop backwards because Remove operations cause array lengths to shrink.
            for (int i = arrayA.Length - 1; i >= 0; i--)
            {
                var indexPath = path.Join(i);
                if (i < arrayB.Length)
                {
                    NodeDiff(arrayA[i], arrayB[i], document, indexPath);
                }
                else
                {
                    document.Test(indexPath, arrayA[i]);
                    document.Remove(indexPath);
                }
            }
        }
    }

    /// <summary>
    /// Remove the nodes from the array to avoid the "node already has a parent"
    /// error when moving a node to the patch document.
    /// </summary>
    private static JsonNode?[] JsonArrayToNodeArray(JsonArray arr)
    {
        var nodes = new JsonNode?[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            nodes[i] = arr[i];
        }
        arr.Clear();
        return nodes;
    }
}
