﻿using AtgDev.Voicemeeter.Types;
using System;
using System.Windows;
using System.Windows.Threading;
using VoicemeeterOsdProgram.Core.Types;
using VoicemeeterOsdProgram.Factories;
using VoicemeeterOsdProgram.Interop;
using VoicemeeterOsdProgram.Options;
using VoicemeeterOsdProgram.UiControls.OSD;
using static TopmostApp.Interop.NativeMethods;

namespace VoicemeeterOsdProgram.Core
{
    public static partial class OsdWindowManager
    {
        private const int WaitMsAfterVmStarted = 6000;
        private const int WaitMsAfetVmTypeChange = 11000;

        private static OsdControl m_wpfControl;
        private static OsdWindow m_window;

        private static DispatcherTimer m_displayDurationTimer = new(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(OptionsStorage.Osd.DurationMs)
        };
        private static DispatcherTimer m_WaitForVmStartedTimer = new(DispatcherPriority.Normal) 
        { 
            Interval = TimeSpan.FromMilliseconds(WaitMsAfterVmStarted) 
        };
        private static DispatcherTimer m_WaitForVmTypeTimer = new(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(WaitMsAfetVmTypeChange)
        };

        private static bool m_isMouseEntered;
        private static bool m_changingOsdContent;
        private static bool m_isVmStarting;
        private static bool m_isVmTypeChanging;
        private static VoicemeeterParameterBase[] m_vmParams = Array.Empty<VoicemeeterParameterBase>();

        static OsdWindowManager()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, _) => Exit();
            Application.Current.Exit += (_, _) => Exit();

            var options = OptionsStorage.Osd;
            OsdControl osd = new();
            m_wpfControl = osd;
            ApplyVisibilityToOsdElements(Visibility.Collapsed);

            var win = new OsdWindow()
            {
                Content = osd,
                Activatable = false,
                TopMost = true,
                IsClickThrough = true,
                ZBandID = GetTopMostZBandID()
            };
            win.CreateWindow();
            m_window = win;

            m_displayDurationTimer.Tick += TimerTick;
            m_WaitForVmStartedTimer.Tick += WaitForVmTimerTick;
            m_WaitForVmTypeTimer.Tick += WaitForVmTypeTimerTick;
            
            IsInteractable = options.IsInteractable;
            BgOpacity = options.BackgroundOpacity;
            IsEnabled = !OptionsStorage.Other.Paused;

            options.IsInteractableChanged += (_, val) => IsInteractable = val;
            options.DurationMsChanged += (_, val) => DurationMs = val;
            options.BackgroundOpacityChanged += (_, val) => BgOpacity = val;
            OptionsStorage.Other.PausedChanged += (_, val) => Application.Current.Dispatcher.Invoke(() => IsEnabled = !val);

            m_wpfControl.CloseBtn.Click += OnCloseButtonClick;
            m_wpfControl.MouseEnter += OnMouseEnter;
            m_wpfControl.MouseLeave += OnMouseLeave;
            
            VoicemeeterApiClient.Loaded += OnVoicemeeterLoad;
        }

        public static void Init() { }

        public static void Show()
        {
            if (!m_isMouseEntered)
            {
                ResetShowTimer();
            }
            if (!m_wpfControl.IsAnyVisibleChild()) return;

            IsShown = true;
            m_window.Show();
        }

        public static void ShowFull()
        {
            ApplyVisibilityToOsdElements(Visibility.Visible);
            Show();
        }

        public static void Hide()
        {
            IsShown = false;
            m_window.HideAnimated();
        }

        public static void Hide(uint fadeOutDuration)
        {
            IsShown = false;
            m_window.HideAnimated(fadeOutDuration);
        }

        public static void Clear()
        {
            foreach (var p in m_vmParams)
            {
                p.IsEnabled = false;
                p.ClearEvents();
            }
            m_vmParams = Array.Empty<VoicemeeterParameterBase>();
            m_wpfControl.MainContent.Children.Clear();
            m_wpfControl.UpdateLayout();
        }

        private static void TimerTick(object sender, EventArgs e)
        {
            m_displayDurationTimer.Stop();
            Hide();
        }

        private static void UpdateOsd()
        {
            if (m_changingOsdContent || m_isVmTypeChanging) return;

            if (m_isVmStarting)
            {
                UpdateVmParams(false);
                return;
            }

            bool isNotifyChanges = !IsIgnoreVmParameters;
            if (!IsShown)
            {
                ApplyVisibilityToOsdElements(Visibility.Collapsed);
            }
            UpdateVmParams(isNotifyChanges);
            if (!isNotifyChanges) return;

            UpdateOsdElementsVis();
            Show();
        }

        private static void UpdateVmParams(bool isNotifyChanges)
        {
            var len = m_vmParams.Length;
            for (int i = 0; i < len; i++)
            {
                m_vmParams[i].ReadIsNotifyChanges(isNotifyChanges);
            }
        }

        private static void RefillOsd(VoicemeeterType type)
        {
            if (type == VoicemeeterType.None) return;

            m_changingOsdContent = true;
            ApplyVisibilityToOsdElements(Visibility.Collapsed);

            Clear();
            OsdContentFactory.FillOsdWindow(ref m_wpfControl, ref m_vmParams, type);

            ApplyVisibilityToOsdElements(Visibility.Collapsed);
            m_changingOsdContent = false;
        }

        private static void ResetShowTimer()
        {
            if (DurationMs != 0)
            {
                m_displayDurationTimer.Stop();
                m_displayDurationTimer.Start();
            }
        }

        private static bool IsVoicemeeterWindowForeground()
        {
            const string WindowClass = "VBCABLE0Voicemeeter0MainWindow0";
            const string WindowText = "VoiceMeeter";

            IntPtr hWnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, WindowClass, WindowText);
            bool isFocused = GetForegroundWindow() == hWnd;
            return isFocused || !WindowObstructedHelper.IsObstructed(hWnd);
        }

        private static void Exit()
        {
            m_displayDurationTimer?.Stop();
            m_WaitForVmStartedTimer?.Stop();
        }
    }
}
