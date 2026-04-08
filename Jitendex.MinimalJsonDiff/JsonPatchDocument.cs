/*
JsonPatchDocument.cs
Copyright (c) 2026 Stephen Kraus
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

internal sealed class JsonPatchDocument
{
    private readonly JsonArray _document = [];

    public string Serialize(JsonSerializerOptions? options)
        => JsonSerializer.Serialize(_document, options);

    public byte[] SerializeToUtf8Bytes(JsonSerializerOptions? options)
        => JsonSerializer.SerializeToUtf8Bytes(_document, options);

    private const string Op = "op";
    private const string Path = "path";
    private const string Value = "value";

    public void Test(string path, JsonNode? value)
        => _document.Add(new JsonObject()
        {
            [Op] = "test",
            [Path] = path,
            [Value] = value,
        });

    public void Add(string path, JsonNode? value)
        => _document.Add(new JsonObject()
        {
            [Op] = "add",
            [Path] = path,
            [Value] = value,
        });

    public void Replace(string path, JsonNode? value)
        => _document.Add(new JsonObject()
        {
            [Op] = "replace",
            [Path] = path,
            [Value] = value,
        });

    public void Remove(string path)
        => _document.Add(new JsonObject()
        {
            [Op] = "remove",
            [Path] = path,
        });
}
