namespace WildRune
{
    public enum RequestMethod
    {
        GET, POST, PATCH, DELETE, PUT
    }

    internal static class Utils
    {
        internal static readonly HttpClient insecureHttpClient = new HttpClient(new HttpClientHandler()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
        });

        internal static class Process
        {
            internal static bool IsRunning(string processName)
            {
                return System.Diagnostics.Process.GetProcessesByName(processName).Length > 0;
            }

            internal static bool IsRunning(int processId)
            {
                try
                {
                    System.Diagnostics.Process.GetProcessById(processId).ToString();
                    return true;
                }
                catch (System.ArgumentException)
                {
                    return false;
                }
            }

            internal static string? GetNameById(int processId) {
                try
                {
                    return System.Diagnostics.Process.GetProcessById(processId).ProcessName;
                }
                catch (System.ArgumentException)
                {
                    return null;
                }
            }

            public static Dictionary<string, string>? GetCmdArgs(string? processName)
            {
                if (string.IsNullOrWhiteSpace(processName)) return null;

                if (!processName.EndsWith(".exe")) processName += ".exe";

                try
                {
                    string query = $"SELECT CommandLine FROM Win32_Process WHERE Name = '{processName}'";
                    using (var searcher = new System.Management.ManagementObjectSearcher(query))
                    using (var results = searcher.Get())
                    {
                        foreach (var process in results)
                        {
                            string? commandLine = process["CommandLine"]?.ToString();
                            if (string.IsNullOrWhiteSpace(commandLine)) return null;

                            var args = new Dictionary<string, string>();
                            string argument = "";
                            bool inQuotes = false;
                            bool equalsFound = false;

                            foreach (char c in commandLine)
                            {
                                switch (c)
                                {
                                    case ' ':
                                        if (!inQuotes && !string.IsNullOrWhiteSpace(argument))
                                        {
                                            AddArgumentToDictionary(argument, args, ref equalsFound);
                                            argument = "";
                                        }
                                        else if (inQuotes)
                                        {
                                            argument += c;
                                        }
                                        break;

                                    case '=':
                                        equalsFound = true;
                                        argument += c;
                                        break;

                                    case '\"':
                                        inQuotes = !inQuotes;
                                        break;

                                    default:
                                        argument += c;
                                        break;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(argument))
                            {
                                AddArgumentToDictionary(argument, args, ref equalsFound);
                            }

                            return args;
                        }
                    }
                }
                catch
                {
                    return null;
                }

                return null;

                static void AddArgumentToDictionary(string argument, Dictionary<string, string> args, ref bool equalsFound)
                {
                    // Remove leading dashes from keys
                    argument = argument.TrimStart('-');

                    if (equalsFound)
                    {
                        string[] split = argument.Split(new[] { '=' }, 2);
                        args[split[0]] = split.Length > 1 ? split[1] : "";
                    }
                    else
                    {
                        args[argument] = "";
                    }
                    equalsFound = false;
                }
            }

        }

        internal static class Endpoint
        {
            internal static void CleanUp(ref string endpoint)
            {
                while (endpoint[0] == '/') endpoint = endpoint.Substring(1);

                endpoint = '/' + endpoint;

                while (endpoint.EndsWith('/')) endpoint = endpoint.Substring(0, endpoint.Length - 1);
            }

            internal static string GetEventFromEndpoint(string endpoint)
            {
                CleanUp(ref endpoint);
                return "OnJsonApiEvent" + endpoint.Replace("/", "_");
            }

            internal static string GetEndpointFromEvent(string eventName)
            {
                if (!eventName.StartsWith("OnJsonApiEvent")) throw new ArgumentException("Event name must start with 'OnJsonApiEvent'");
                return eventName.Replace("OnJsonApiEvent", "").Replace("_", "/");
            }
        }

        internal static class API
        {
            internal static bool ValidateKey(string apiKey)
            {
                if (apiKey.Length != 42 || !apiKey.StartsWith("RGAPI")) return false;

                short[] minusPositions = { 5, 14, 19, 24, 29 };

                foreach (short index in minusPositions)
                {
                    if (apiKey[index] != '-') return false;
                }

                return true;
            }
        }
    }
}
