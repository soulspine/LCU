namespace WildRune
{
    public static class RiotAPI
    {
        private static string apiKey = "";

        public static void SetApiKey(string newApiKey)
        {
            if (!Utils.API.ValidateKey(newApiKey)) throw new ArgumentException("Invalid API key");
            else apiKey = newApiKey;
        }
    }
}
