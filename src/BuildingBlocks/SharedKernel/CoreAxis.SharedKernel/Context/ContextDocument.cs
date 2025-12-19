using System.Text.Json;
using System.Text.Json.Nodes;

namespace CoreAxis.SharedKernel.Context;

public class ContextDocument
{
    private JsonObject _root;

    public ContextDocument(string json = "{}")
    {
        try 
        {
            var node = JsonNode.Parse(json);
            if (node is JsonObject obj)
            {
                _root = obj;
            }
            else
            {
                _root = new JsonObject();
            }
        }
        catch
        {
             _root = new JsonObject();
        }
    }
    
    public ContextDocument(JsonObject root)
    {
        _root = root ?? new JsonObject();
    }

    public T? Get<T>(string path)
    {
        var node = GetNode(path);
        if (node == null) return default;
        return node.Deserialize<T>();
    }
    
    public JsonNode? GetNode(string path)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonNode? current = _root;
        
        foreach (var segment in segments)
        {
            if (current is JsonObject obj && obj.TryGetPropertyValue(segment, out var next))
            {
                current = next;
            }
            else
            {
                return null;
            }
        }
        
        return current;
    }

    public void Set<T>(string path, T value)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return;

        JsonObject current = _root;
        
        for (int i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (current.TryGetPropertyValue(segment, out var next) && next is JsonObject nextObj)
            {
                current = nextObj;
            }
            else
            {
                var newObj = new JsonObject();
                current[segment] = newObj;
                current = newObj;
            }
        }

        var lastSegment = segments[^1];
        current[lastSegment] = JsonSerializer.SerializeToNode(value);
    }
    
    public void Merge(ContextDocument other)
    {
        MergeObjects(_root, other._root);
    }
    
    private void MergeObjects(JsonObject target, JsonObject source)
    {
        foreach (var kvp in source)
        {
            if (kvp.Value is JsonObject sourceObj)
            {
                if (target.TryGetPropertyValue(kvp.Key, out var targetNode) && targetNode is JsonObject targetObj)
                {
                    MergeObjects(targetObj, sourceObj);
                }
                else
                {
                    target[kvp.Key] = sourceObj.DeepClone();
                }
            }
            else
            {
                target[kvp.Key] = kvp.Value?.DeepClone();
            }
        }
    }

    public string ToJson()
    {
        return _root.ToJsonString();
    }
}
