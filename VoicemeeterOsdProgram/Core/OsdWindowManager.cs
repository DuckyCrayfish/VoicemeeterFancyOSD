﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using VoicemeeterOsdProgram.Core.Types;
using VoicemeeterOsdProgram.Factories;
using VoicemeeterOsdProgram.Types;
using VoicemeeterOsdProgram.UiControls.OSD;
using AtgDev.Voicemeeter.Types;
using VoicemeeterOsdProgram.Interop;
using static TopmostApp.Interop.NativeMethods;
using VoicemeeterOsdProgram.Options;

namespace VoicemeeterOsdProgram.Core
{
    public static partial class OsdWindowManager
    {
        private static OsdControl m_wpfControl;
        private static OsdWindow m_window;
        private static DispatcherTimer m_tickTimer;
        private static bool m_isMouseEntered;
        private static IVmParamReadable[] m_vmParams = Array.Empty<IVmParamReadable>();

        static OsdWindowManager()
        {
            OsdControl osd = new();
            m_wpfControl = osd;
            ApplyVisibilityToOsdElements(Visibility.Collapsed);

            var win = new OsdWindow()
            {
                WorkingAreaVertAlignment = VertAlignment.Top,
                WorkingAreaHorAlignment = HorAlignment.Right,
                Content = osd,
                Activatable = false,
                TopMost = true,
                IsClickThrough = true,
                ZBandID = GetTopMostZBandID()
            };
            win.CreateWindow();
            m_window = win;

            m_tickTimer = new(DispatcherPriority.Normal);
            m_tickTimer.Interval = TimeSpan.FromMilliseconds(OptionsStorage.Osd.DurationMs);
            m_tickTimer.Tick += TimerTick;

            var options = OptionsStorage.Osd;
            IsInteractable = options.IsInteractable;
            BgOpacity = options.BackgroundOpacity;

            options.IsInteractableChanged += (_, val) => IsInteractable = val;
            options.DurationMsChanged += (_, val) => DurationMs = val;
            options.BackgroundOpacityChanged += (_, val) => BgOpacity = val;

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
            if (IsShown) return;

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

        private static void TimerTick(object sender, EventArgs e)
        {
            m_tickTimer.Stop();
            Hide();
        }

        private static void UpdateOsd()
        {
            bool isIgnoreParams = IsIgnoreVmParameters;
            if (!IsShown)
            {
                ApplyVisibilityToOsdElements(Visibility.Collapsed);
            }
            UpdateVmParams(isIgnoreParams);
            if (isIgnoreParams) return;

            UpdateOsdElementsVis();
            Show();
        }

        private static void UpdateVmParams(bool isSkipEvents)
        {
            var len = m_vmParams.Length;
            for (int i = 0; i < len; i++)
            {
                m_vmParams[i].ReadIsIgnoreEvent(isSkipEvents);
            }
        }

        private static void RefillOsd(VoicemeeterType type)
        {
            if (type == VoicemeeterType.None) return;

            m_wpfControl.MainContent.Children.Clear();
            m_vmParams = Array.Empty<IVmParamReadable>();
            OsdContentFactory.FillOsdWindow(ref m_wpfControl, ref m_vmParams, type);
            ApplyVisibilityToOsdElements(Visibility.Collapsed);
        }

        private static void ResetShowTimer()
        {
            if (DurationMs != 0)
            {
                m_tickTimer.Stop();
                m_tickTimer.Start();
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

        private static void OnNewVoicemeeterParams(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(UpdateOsd);
        }

        private static void OnVoicemeeterTypeChange(object sender, VoicemeeterType t)
        {
            Application.Current.Dispatcher.Invoke(() => RefillOsd(t));
        }

        private static void OnVoicemeeterLoad(object sender, EventArgs e)
        {
            VoicemeeterApiClient.ProgramTypeChange += OnVoicemeeterTypeChange;
            VoicemeeterApiClient.NewParameters += OnNewVoicemeeterParams;
            Application.Current.Dispatcher.Invoke(() => RefillOsd(VoicemeeterApiClient.ProgramType));
        }

        private static void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            m_isMouseEntered = true;
            m_tickTimer.Stop();
        }

        private static void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            m_isMouseEntered = false;
            if (IsShown)
            {
                ResetShowTimer();
            }
        }

        private static void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            if (!IsShown) return;

            Hide(75);
        }
    }
}
