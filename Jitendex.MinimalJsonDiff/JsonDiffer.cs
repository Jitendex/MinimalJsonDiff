/*
JsonDiffer.cs
Copyright (c) 2025 Stephen Kraus
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

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

namespace Jitendex.MinimalJsonDiff;

public static class JsonDiffer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static string Diff<T>(T a, T b) where T : class
    {
        var nodeA = JsonSerializer.SerializeToNode(a);
        var nodeB = JsonSerializer.SerializeToNode(b);
        return Diff(nodeA, nodeB);
    }

    public static string Diff(JsonNode? a, JsonNode? b)
    {
        var document = new JsonPatchDocument();
        NodeDiff(a, b, document, path: string.Empty);

        // JsonPatchDocument serialization is broken in .NET 10
        // return JsonSerializer.Serialize(document);

        return SerializeDocument(document);
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
        foreach (var (key, nodeA) in a)
        {
            var keyPath = path.Join(key);
            if (b.TryGetPropertyValue(key, out var nodeB))
            {
                NodeDiff(nodeA, nodeB, document, keyPath);
            }
            else
            {
                document.Test(keyPath, nodeA);
                document.Remove(keyPath);
            }
        }

        foreach (var (key, nodeB) in b)
        {
            if (!a.ContainsKey(key))
            {
                document.Add(path.Join(key), nodeB);
            }
        }
    }

    private static void ArrayDiff(JsonArray a, JsonArray b, JsonPatchDocument document, string path)
    {
        // One array is empty, but the other is not empty.
        if (a.Count == 0 ^ b.Count == 0)
        {
            document.Test(path, a);
            document.Replace(path, b);
        }

        // Arrays are equal length, or array B is larger.
        else if (a.Count <= b.Count)
        {
            for (int i = 0; i < b.Count; i++)
            {
                var indexPath = path.Join(i);
                if (i < a.Count)
                {
                    NodeDiff(a[i], b[i], document, indexPath);
                }
                else
                {
                    document.Add(indexPath, b[i]);
                }
            }
        }

        // Array A is larger than array B.
        else
        {
            // Loop backwards because Remove operations cause array lengths to shrink.
            for (int i = a.Count - 1; i >= 0; i--)
            {
                var indexPath = path.Join(i);
                if (i < b.Count)
                {
                    NodeDiff(a[i], b[i], document, indexPath);
                }
                else
                {
                    document.Test(indexPath, a[i]);
                    document.Remove(indexPath);
                }
            }
        }
    }

    /// <summary>
    /// This method can be removed when JsonPatchDocument serialization is fixed in the dotnet runtime.
    /// See https://github.com/Jitendex/MinimalJsonDiff/issues/1
    /// </summary>
    private static string SerializeDocument(JsonPatchDocument document)
    {
        var node = JsonSerializer.SerializeToNode(document);
        if (node is not JsonArray array)
        {
            throw new Exception("Expected document to be an array");
        }
        foreach (var element in array)
        {
            if (element is not JsonObject obj)
            {
                throw new Exception("Expected all elements of document array to be objects");
            }

            // Since we're only using "Add", "Remove", "Replace", and "Test"
            // operations, the "from" property is never necessary.
            obj.Remove("from");

            // For "Remove" operations, the "Value" property is never necessary.
            if (!obj.TryGetPropertyValue("op", out var opNode))
            {
                continue;
            }
            if (opNode?.GetValue<string?>() is not string opValue)
            {
                continue;
            }
            if (string.Equals(opValue, "remove", StringComparison.Ordinal))
            {
                obj.Remove("value");
            }
        }
        return JsonSerializer.Serialize(node);
    }
}
