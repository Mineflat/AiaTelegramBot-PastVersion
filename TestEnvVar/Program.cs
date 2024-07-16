using System.Diagnostics;

namespace TestEnvVar
{
    internal class Program
    {
        protected static async void RunScript(string location, string? args = null)
        {
            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = location,
                        Arguments = $"{args}",
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true
                    }
                };
                process.EnableRaisingEvents = false;
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        Console.ForegroundColor= ConsoleColor.Green;
                        Console.Write($"[SCRIPT]: ");
                        Console.ResetColor();
                        Console.WriteLine($"{args.Data}");
                    }
                };
                Console.WriteLine($"Запуск процесса: {location} ${args}");
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                Console.WriteLine($"Процесс {location} завершился успешно");
                process.CancelOutputRead();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
        }
        static void Main(string[] args)
        {
            string[] envs =
            {
                "host",
                "port",
                "ip",
                "net",
                "mask"
            };
            string[] setEnvsList =
            {
                "set1",
                "set2",
                "set3",
                "set4",
                "set5"
            };
            Console.WriteLine("\tПеременные окружения (до запуска):");
            foreach (var item in envs)
            {
                Console.WriteLine($"\t\t{item}={Environment.GetEnvironmentVariable(item)}");
            }
            RunScript("./env");
            RunScript("./set_env.sh");
            
            Console.WriteLine("\tПеременные окружения (после запуска):");
            foreach (var item in envs)
            {
                Console.WriteLine($"\t\t{item}={Environment.GetEnvironmentVariable(item)}");
            }
            foreach (var item in setEnvsList)
            {
                Console.WriteLine($"Setting {item}={item}_success");
                Environment.SetEnvironmentVariable(item, $"{item}_success");
            }
            RunScript("./get_env.sh");
        }
    }
}