using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkflowEngine.Core
{
    public static class RecipeSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static string Serialize(InspectionRecipe recipe)
        {
            return JsonSerializer.Serialize(recipe, Options);
        }

        public static InspectionRecipe Deserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<InspectionRecipe>(json, Options);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static Task<InspectionRecipe> DeserializeFromFileAsync(string path)
        {
            return Task.Run(() =>
            {
                var json = File.ReadAllText(path);
                return Deserialize(json);
            });
        }

        public static Task SerializeToFileAsync(string path, InspectionRecipe recipe)
        {
            return Task.Run(() =>
            {
                var json = Serialize(recipe);
                File.WriteAllText(path, json);
            });
        }
    }
}