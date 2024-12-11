namespace WildRune
{
    public class RiotAPI
    {
        private string apiKey;

        public RiotAPI(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public void UpdateApiKey(string newApiKey)
        {
            if (!Utils.API.ValidateKey(newApiKey)) throw new ArgumentException("Invalid API key");
            else apiKey = newApiKey;
        }
    }
}
