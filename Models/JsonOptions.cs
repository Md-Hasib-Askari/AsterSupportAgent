using System.Text.Json;

namespace AsterSupportAgent.Models;

/// <summary>
/// Cached JsonSerializerOptions instances, reused across the app instead of
/// constructing a new instance per serialize/deserialize call. JsonSerializerOptions
/// is expensive to build (it compiles and caches type metadata internally) and is
/// thread-safe to share once configured, so a single static instance per
/// configuration shape is the correct pattern here.
/// </summary>
public static class JsonOptions
{
    /// <summary>
    /// Case-insensitive property matching, used wherever we deserialize JSON
    /// produced outside our own serializer (data files on disk, LLM tool-call output).
    /// </summary>
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
