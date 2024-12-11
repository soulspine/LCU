using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.WebSockets;

namespace LCUcore
{
    internal static class Utils
    {
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

            internal static Dictionary<string, string>? GetCmdArgs(string? processName)
            {
                if ((processName == null) || (!IsRunning(processName))) return null;

                if (!processName.EndsWith(".exe")) processName += ".exe";

                string command = $"wmic process where \"caption='{processName}'\" get CommandLine";
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.Arguments = "/c " + command;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                string argument = "";
                bool inQuotes = false;
                bool equalsFound = false;

                Dictionary<string, string> outputDict = new Dictionary<string, string>();

                foreach (char c in output)
                {
                    switch (c)
                    {
                        // skip
                        case ' ':
                            {
                                break;
                            }

                        // equals found, output will have a value
                        case '=':
                            {
                                equalsFound = true;
                                argument += c;
                                break;
                            }

                        // argument boundary
                        case '\"':
                            {
                                // at the start
                                if (!inQuotes)
                                {
                                    inQuotes = true;
                                }
                                // at the end
                                else
                                {
                                    argument = argument.Replace("--", "");
                                    if (equalsFound)
                                    {
                                        string[] split;
                                        split = argument.Split('=');
                                        outputDict.Add(split[0], split[1]);
                                    }
                                    else
                                    {
                                        outputDict.Add(argument, "");
                                    }

                                    inQuotes = false;
                                    equalsFound = false;
                                    argument = "";
                                }


                                break;
                            }
                        default:
                            {
                                if (inQuotes) argument += c;
                                break;
                            }
                    }
                }

                // this is here to destroy past outputDict object, specifically when league was launching, it would skyrocket memory usage if this was not here
                GC.Collect();

                return outputDict;
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
                if (!endpoint.StartsWith("/")) endpoint = "/" + endpoint;
                return "OnJsonApiEvent" + endpoint.Replace("/", "_");
            }

            internal static string GetEndpointFromEvent(string eventName)
            {
                if (!eventName.StartsWith("OnJsonApiEvent")) throw new ArgumentException("Event name must start with 'OnJsonApiEvent'");
                return eventName.Replace("OnJsonApiEvent", "").Replace("_", "/");
            }
        }
    }
}
