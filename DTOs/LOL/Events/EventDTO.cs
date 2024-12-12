using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WildRune.DTOs.LOL.Events
{
    public abstract class BaseEventDTO
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public float EventTime { get; set; }

    }

    public class EventConverter : JsonConverter<BaseEventDTO>
    {
        public override void WriteJson(JsonWriter writer, BaseEventDTO? value, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.FromObject(value);
            jsonObject.WriteTo(writer);
        }

        public override BaseEventDTO? ReadJson(JsonReader reader, Type objectType, BaseEventDTO? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var eventName = jsonObject["EventName"]?.ToString();

            BaseEventDTO result = CreateEvent(eventName!);

            serializer.Populate(jsonObject.CreateReader(), result);
            return result;
        }

        public BaseEventDTO CreateEvent(string eventName)
        {
            // Get all types in the current assembly that inherit from BaseEventDTO
            var eventType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.IsClass
                    && !t.IsAbstract
                    && typeof(BaseEventDTO).IsAssignableFrom(t)
                    && t.Name.Equals(eventName + "EventDTO", StringComparison.OrdinalIgnoreCase));

            if (eventType == null)
            {
                throw new InvalidOperationException($"Unknown event type: {eventName}");
            }

            // Create an instance of the found type
            return (BaseEventDTO)Activator.CreateInstance(eventType)!;
        }
    }
}
