﻿using IniParser.Model;
using IniParser.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VoicemeeterOsdProgram.Options
{
    public static class OptionsStorage
    {
        // field name will be used as INI file Section name
        public static readonly OsdOptions Osd = new();
        public static readonly OsdAlternative AltOsdOptionsFullscreenApps = new();
        public static readonly UpdaterOptions Updater = new();
        public static readonly OtherOptions Other = new();

        private static readonly string m_path = @$"{AppDomain.CurrentDomain.BaseDirectory}config\config.ini";

        private static readonly IniDataParser m_parser = new();
        private static IniData m_data = new();
        private static FileSystemWatcher m_watcher = new();
        private static DispatcherTimer m_timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(1000)};
        private static bool m_isWatcherEnabled;
        private static bool m_isWatcherPaused;
        private static bool m_isInit = false;

        static OptionsStorage()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, _) => Exit();
            System.Windows.Application.Current.Exit += (_, _) => Exit();
        }

        public static string ConfigFilePath => m_path;

        public static async Task InitAsync()
        {
            if (m_isInit) return;

            await ValidateConfigFileAsync();

            m_timer.Tick += OnTimerTick;

            m_watcher.Path = Path.GetDirectoryName(m_path);
            m_watcher.Filter = Path.GetFileName(m_path);
            m_watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            m_watcher.Changed += OnConfigFileChanged;
            IsWatcherEnabled = true;

            m_isInit = true;
        }

        public static bool IsWatcherEnabled
        {
            get => m_isWatcherEnabled;
            set
            {
                m_isWatcherEnabled = value;
                if (value)
                {
                    m_watcher.EnableRaisingEvents = !IsWatcherPaused;
                    if (IsWatcherPaused)
                    {
                        m_timer.Stop();
                    }
                }
                else
                {
                    m_watcher.EnableRaisingEvents = false;
                    m_timer.Stop();
                }
            }
        }

        public static bool IsWatcherPaused
        {
            get => m_isWatcherPaused;
            set
            {
                m_isWatcherPaused = value;
                if (value)
                {
                    m_watcher.EnableRaisingEvents = false;
                    m_timer.Stop();
                }
                else
                {
                    m_watcher.EnableRaisingEvents = IsWatcherEnabled;
                    if (!IsWatcherEnabled)
                    {
                        m_timer.Stop();
                    }
                }
            }
        }

        public static async Task<bool> TrySaveAsync()
        {
            bool result = false;
            IsWatcherPaused = true;

            try
            {
                var directoryPath = Path.GetDirectoryName(m_path);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                ToIniData(Osd);
                ToIniData(AltOsdOptionsFullscreenApps);
                ToIniData(Updater);
                await using (StreamWriter sw = new(m_path))
                {
                    await sw.WriteAsync(m_data.ToString());
                }

                result = true;
            }
            catch { }

            IsWatcherPaused = false;
            return result;
        }

        public static async Task<bool> TryReadAsync()
        {
            bool result = false;
            IsWatcherPaused = false;

            try
            {
                const long MB = 1024 * 1024;
                if (new FileInfo(m_path).Length > 100 * MB) throw new InvalidOperationException("Config file size is too large");

                using StreamReader sr = new(m_path);
                string fileData = await sr.ReadToEndAsync();

                m_data = m_parser.Parse(fileData);
                FromIniData(Osd);
                FromIniData(Updater);
                FromIniData(AltOsdOptionsFullscreenApps);

                m_data = new();
                result = true;
            }
            catch { }

            IsWatcherPaused = true;
            return result;
        }

        private static void ToIniData<T>(T optionsObj)
        {
            var sectionName = GetSectionName(optionsObj);
            var properties = optionsObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                m_data[sectionName][prop.Name] = prop.GetValue(optionsObj).ToString();
                var comments = GetPropComments(prop);
                if (comments.Count > 0)
                {
                    m_data[sectionName].GetKeyData(prop.Name).Comments = comments;
                }
            }
        }

        private static List<string> GetPropComments(PropertyInfo prop)
        {
            List<string> comments = new();
            if (prop.GetCustomAttribute(typeof(DescriptionAttribute)) is DescriptionAttribute att)
            {
                comments.Add(' ' + att.Description);
            }
            if (prop.PropertyType.IsEnum)
            {
                var values = string.Join(", ", prop.PropertyType.GetEnumNames());
                comments.Add($" Possible values: {values}");
            }
            return comments;
        }

        private static void FromIniData<T>(T optionsObj)
        {
            var properties = optionsObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                ValidateOption(optionsObj, prop);
            }
        }

        private static void ValidateOption<T>(T optionsObj, PropertyInfo optionProp)
        {
            var sectionName = GetSectionName(optionsObj);
            var name = optionProp.Name;
            var field = m_data[sectionName][name];
            try
            {
                if (!string.IsNullOrEmpty(field))
                {
                    object result = optionProp.PropertyType.IsEnum ? 
                        Enum.Parse(optionProp.PropertyType, field) :
                        Convert.ChangeType(field, optionProp.PropertyType);
                    if (result is not null)
                    {
                        optionProp.SetValue(optionsObj, result);
                    }
                }
            }
            catch { }
            m_data[sectionName][name] = optionProp.GetValue(optionsObj).ToString();
        }

        private static string GetSectionName<T>(T optionsObj)
        {
            var members = typeof(OptionsStorage).GetFields(BindingFlags.Static | BindingFlags.Public);
            return members.First(m => m.FieldType.Name == optionsObj.GetType().Name).Name;
        }

        private static async Task ValidateConfigFileAsync()
        {
            bool readRes = await TryReadAsync();
            bool writeRes = await TrySaveAsync();
            m_isInit = true;
        }

        private static void Exit()
        {
            m_timer?.Stop();
        }

        private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            m_timer.Stop();
            m_timer.Start();
        }

        private static void OnTimerTick(object sender, EventArgs e)
        {
            m_timer.Stop();
            _ = ValidateConfigFileAsync();
        }
    }
}
