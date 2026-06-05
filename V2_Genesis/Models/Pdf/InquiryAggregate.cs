using System.Collections.Generic;

namespace V2_Genesis.Models
{
    /// <summary>
    /// Represents a fully hydrated inquiry consisting of the main record
    /// and all section records required to populate a form.
    /// </summary>
    public class InquiryAggregate
    {
        public object? Main { get; set; }
        public Dictionary<string, object?> Sections { get; } = new();
        public T? GetSection<T>(string key) where T : class
        {
            if (Sections.TryGetValue(key, out var value))
            {
                return value as T;
            }
            return null;
        }
    }
}