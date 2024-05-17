using Microsoft.Win32;
using System.Runtime.CompilerServices;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System.Collections.Frozen;
using System.Collections.Generic;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Leayal.Sunshine.SteamGameLauncher
{
    internal class Program
    {
        private static bool isStarted;
        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PrintErrorSteamNotFound() => Console.Error.WriteLine("Cannot find Steam on the machine. Please install Steam client, or run Steam client at least once before using this tool.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PrintErrorSteamFoundUninit() => Console.Error.WriteLine("Steam client found, but has never been initialized. Please run Steam client at least once before using this tool.");

        static async Task Main(string[] args)
        {
            isStarted = false;
            Console.CancelKeyPress += Console_CancelKeyPress;

            string path_steamInstallationDir = string.Empty;

            using (var reg_root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (var reg_hklm = reg_root.OpenSubKey(Path.Join("SOFTWARE", "Valve", "Steam"), false))
            {
                if (reg_hklm == null)
                {
                    PrintErrorSteamNotFound();
                    return;
                }

                var obj_steamPath = reg_hklm.GetValue("InstallPath");
                if (obj_steamPath == null)
                {
                    PrintErrorSteamNotFound();
                    return;
                }

                var kind = reg_hklm.GetValueKind("InstallPath");
                switch (reg_hklm.GetValueKind("InstallPath"))
                {
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                        path_steamInstallationDir = (string)obj_steamPath;
                        break;
                    default:
                        PrintErrorSteamNotFound();
                        return;
                }
            }

            var path_SteamClientBinary = Path.Join(path_steamInstallationDir, "steam.exe");
            if (!File.Exists(path_SteamClientBinary))
            {
                PrintErrorSteamNotFound();
                return;
            }

            var path_SteamLibraryDefinition = Path.Join(path_steamInstallationDir, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(path_SteamLibraryDefinition))
            {
                PrintErrorSteamFoundUninit();
                return;
            }

            var steamGameIdListing = new Dictionary<long, string>();

            var steamLibraryDefinition = VdfConvert.Deserialize(File.ReadAllText(path_SteamLibraryDefinition));
            foreach (var steamLibDirDefinition in steamLibraryDefinition.Value.Children<VProperty>())
            {
                var libDirData = steamLibDirDefinition.Value;
                var vtoken_libPath = libDirData.Value<string>("path");
                var vprop_apps = libDirData.Value<VObject>("apps");

                foreach (var gameDefinition in vprop_apps.Children<VProperty>())
                {
                    if (long.TryParse(gameDefinition.Key, out var gameAppId))
                    {
                        steamGameIdListing.Add(gameAppId, vtoken_libPath);
                    }
                }
            }

            if (args.Length == 0)
            {
                Console.Error.WriteLine("Please specify an AppId as the first launch argument of this executable.");
                return;
            }
            
            if (!long.TryParse(args[0], out var steamAppId))
            {
                Console.Error.WriteLine("You did not specify a valid AppId, AppId should be number only. Please specify an AppId as the first launch argument of this executable.");
                return;
            }

            var perfDictionary = FrozenDictionary.ToFrozenDictionary(steamGameIdListing);
            if (!perfDictionary.TryGetValue(steamAppId, out var steamLibPath))
            {
                Console.Error.WriteLine("The game has not been installed on your computer. Please install the game through Steam Client.");
                return;
            }

            var path_gameManifest = Path.Join(steamLibPath, "steamapps", $"appmanifest_{steamAppId}.acf");

            var path_gameManifestDefinition = VdfConvert.Deserialize(File.ReadAllText(path_gameManifest));
            var gameManifestDefinition = path_gameManifestDefinition.Value;
            var gameDirectoryName = gameManifestDefinition.Value<string>("installdir");
            var gameName = gameManifestDefinition.Value<string>("name");
            var path_gameDirectory = Path.GetFullPath(Path.Join("steamapps", "common", gameDirectoryName), steamLibPath);

            string executableName = string.Empty;
            if (args.Length > 1)
            {
                executableName = args[1];
            }
            else
            {
                foreach (var binaryPath in Directory.EnumerateFiles(path_gameDirectory, "*.exe", new EnumerationOptions() { MaxRecursionDepth = 30, ReturnSpecialDirectories = false, RecurseSubdirectories = true, IgnoreInaccessible = true }))
                {
                    executableName = binaryPath;
                    break;
                }
            }

            if (string.IsNullOrEmpty(executableName))
            {
                Console.Error.WriteLine("This tool cannot determine the main executable of the game. Please specify the name of the executable file as the second launch argument.");
                return;
            }

            Console.WriteLine("Launching game: " + gameName);

            // Launch the game via Steam
            Process.Start(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), $"steam://launch/{steamAppId}/dialog")?.Dispose();

            isStarted = true;

            var processName = new string(Path.GetFileNameWithoutExtension(executableName.AsSpan()));
            Console.WriteLine("Finding game process: " + processName);
            var gracePeriod = Task.Delay(TimeSpan.FromSeconds(10));
            var cancelToken = cancellationTokenSource.Token;
            while (!cancelToken.IsCancellationRequested)
            {
                var proccesses = Process.GetProcessesByName(processName);
                if (proccesses.Length == 0)
                {
                    if (gracePeriod.IsCompleted) break;
                }
                foreach (var proc in proccesses)
                {
                    if (!cancelToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"Found game process: {Path.GetFileName(executableName.AsSpan())} (Id: {proc.Id}). Waiting for the process to exit...");
                        await proc.WaitForExitAsync(cancelToken);
                        Console.WriteLine("Process exited.");
                    }
                    proc.Dispose();
                    // break;
                }

                if (!cancelToken.IsCancellationRequested) await Task.Delay(50);
                // Retry again after 50ms
            }

            Console.WriteLine("Exiting steam game launcher to stop Sunshine session...");
        }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                if (isStarted)
                {
                    cancellationTokenSource.Token.Register(ForceExit);
                    cancellationTokenSource.Cancel(false);
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        }

        private static void ForceExit() => Environment.Exit(1);
    }
}
