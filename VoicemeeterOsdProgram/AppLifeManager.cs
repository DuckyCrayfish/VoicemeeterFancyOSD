﻿using AtgDev.Utils.ProcessExtensions;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Threading;

namespace VoicemeeterOsdProgram
{
    public static class AppLifeManager
    {
        private static Mutex m_mutex = new(true, Program.UniqueName);
        private static bool? m_isLareadyRunning;
        private static Dispatcher m_dispatcher;

        public static bool IsAlreadyRunning
        {
            get
            {
                if (m_isLareadyRunning is null)
                {
                    m_isLareadyRunning = !m_mutex.WaitOne(0, false);
                }
                return m_isLareadyRunning.Value;
            }
        }

        public static void Start(string[] args, Action action)
        {
            if (IsAlreadyRunning)
            {
                SendArgsToFirstInstance(args);
                Environment.Exit(0);
            }

            m_dispatcher = Dispatcher.CurrentDispatcher;

            ArgsHandler.Handle(args);
            Thread pipeServerThread = new(CreatePipeServer)
            {
                IsBackground = true,
            };
            pipeServerThread.SetApartmentState(ApartmentState.STA);
            pipeServerThread.Start();

            action();
        }

        public static void CloseDuplicates()
        {
            DirectoryInfo programDir = new(AppDomain.CurrentDomain.BaseDirectory);
            // iterating over all exe files because program can be launched by multiple executables
            foreach (var exeFile in programDir.GetFiles("*.exe"))
            {
                string programName = Path.GetFileNameWithoutExtension(exeFile.Name);
                var procs = Process.GetProcessesByName(programName);
                RequestKillDuplicateProcesses(procs);
            }
        }

        private static void SendArgsToFirstInstance(string[] args)
        {
            if (args.Length == 0) return;

            var rawArgs = string.Join(' ', args);
            if (string.IsNullOrWhiteSpace(rawArgs)) return;

            using NamedPipeClientStream client = new(".", Program.UniqueName, PipeDirection.Out); // "." is for Local Computer
            try
            {
                client.Connect(1000);
                using StreamWriter writer = new StreamWriter(client);
                writer.Write(rawArgs);
            }
            catch { }
        }

        private static void RequestKillDuplicateProcesses(Process[] procs)
        {
            foreach (var p in procs)
            {
                if (Environment.ProcessId == p.Id) continue;

                p.RequestKill();
                p.WaitForExit(1000);
            }
        }

        private static void CreatePipeServer()
        {
            using NamedPipeServerStream server = new(Program.UniqueName, PipeDirection.In);
            using StreamReader reader = new(server);
            while (true)
            {
                server.WaitForConnection();
                try
                {
                    string rawArgs = reader.ReadToEnd();
                    m_dispatcher.Invoke(() => ArgsHandler.Handle(rawArgs));
                }
                catch { }
                server.Disconnect();
            }
        }
    }
}
