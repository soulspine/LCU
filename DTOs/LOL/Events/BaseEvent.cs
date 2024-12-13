using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WildRune.DTOs.LOL.Events
{
    public abstract class BaseEvent
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public float EventTime { get; set; }

    }

    public class EventConverter : JsonConverter<BaseEvent>
    {
        public override void WriteJson(JsonWriter writer, BaseEvent? value, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.FromObject(value);
            jsonObject.WriteTo(writer);
        }

        public override BaseEvent? ReadJson(JsonReader reader, Type objectType, BaseEvent? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var eventName = jsonObject["EventName"]?.ToString();

            BaseEvent result = CreateEvent(eventName!);

            serializer.Populate(jsonObject.CreateReader(), result);
            return result;
        }

        public BaseEvent CreateEvent(string eventName)
        {
            // Get all types in the current assembly that inherit from BaseEventDTO
            var eventType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.IsClass
                    && !t.IsAbstract
                    && typeof(BaseEvent).IsAssignableFrom(t)
                    && t.Name.Equals(eventName + "Event", StringComparison.OrdinalIgnoreCase));

            if (eventType == null)
            {
                throw new InvalidOperationException($"Unknown event type: {eventName}");
            }

            // Create an instance of the found type
            return (BaseEvent)Activator.CreateInstance(eventType)!;
        }
    }
}
