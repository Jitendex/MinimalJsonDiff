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
        var patch = new JsonPatchDocument();
        NodeDiff(nodeA, nodeB, patch, path: string.Empty);
        return JsonSerializer.Serialize(patch, SerializerOptions);
    }

    private static void NodeDiff(JsonNode? a, JsonNode? b, JsonPatchDocument patch, string path)
    {
        if (a is JsonObject objA && b is JsonObject objB)
        {
            ObjectDiff(objA, objB, patch, path);
        }
        else if (a is JsonArray arrA && b is JsonArray arrB)
        {
            ArrayDiff(arrA, arrB, patch, path);
        }
        else if (!JsonNode.DeepEquals(a, b))
        {
            patch.Test(path, a);
            patch.Replace(path, b);
        }
    }

    private static void ObjectDiff(JsonObject a, JsonObject b, JsonPatchDocument patch, string path)
    {
        static string Join(string path, string key) => $"{path}/{key}";

        foreach (var (key, nodeA) in a)
        {
            var keyPath = Join(path, key);
            if (b.TryGetPropertyValue(key, out var nodeB))
            {
                NodeDiff(nodeA, nodeB, patch, keyPath);
            }
            else
            {
                patch.Test(keyPath, nodeA);
                patch.Remove(keyPath);
            }
        }

        foreach (var (key, nodeB) in b)
        {
            if (!a.ContainsKey(key))
            {
                patch.Add(Join(path, key), nodeB);
            }
        }
    }

    private static void ArrayDiff(JsonArray a, JsonArray b, JsonPatchDocument patch, string path)
    {
        static string Join(string path, int i) => $"{path}/{i}";

        if (a.Count == 0 ^ b.Count == 0)
        {
            patch.Test(path, a);
            patch.Replace(path, b);
        }
        else if (a.Count <= b.Count)
        {
            for (int i = 0; i < b.Count; i++)
            {
                var indexPath = Join(path, i);
                if (i < a.Count)
                {
                    NodeDiff(a[i], b[i], patch, indexPath);
                }
                else
                {
                    patch.Add(indexPath, b[i]);
                }
            }
        }
        else
        {
            for (int i = a.Count - 1; i >= 0; i--)
            {
                var indexPath = Join(path, i);
                if (i < b.Count)
                {
                    NodeDiff(a[i], b[i], patch, indexPath);
                }
                else
                {
                    patch.Test(indexPath, a[i]);
                    patch.Remove(indexPath);
                }
            }
        }
    }
}
