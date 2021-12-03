﻿using System;
using System.ComponentModel;
using VoicemeeterOsdProgram.Types;

namespace VoicemeeterOsdProgram.Options
{
    public class OsdOptions : OptionsBase
    {
        private uint m_displayIndex;
        private bool m_dontShowIfVoicemeeterVisible = true;
        private bool m_isInteractable = false;
        private uint m_durationMs = 2000;
        private double m_backgroundOpacity = 0.9;
        private HorAlignment m_horizontalAlignment = HorAlignment.Right;
        private VertAlignment m_verticalAlignment = VertAlignment.Top;

        [Description("0 - is a primary display, 1 - is a secondary, etc")]
        public uint DisplayIndex
        {
            get => m_displayIndex;
            set => HandlePropertyChange(ref m_displayIndex, ref value, DisplayIndexChanged);
        }

        [Description("Dont show OSD if Voicemeeter's window is visible (and not obstructed) or is active window")]
        public bool DontShowIfVoicemeeterVisible 
        {
            get => m_dontShowIfVoicemeeterVisible;
            set => HandlePropertyChange(ref m_dontShowIfVoicemeeterVisible, ref value, DontShowIfVoicemeeterVisibleChanged);
        }

        [Description("User can interact with UI elements in OSD (Gain Fader, Mute, Mono, etc)")]
        public bool IsInteractable 
        {
            get => m_isInteractable;
            set => HandlePropertyChange(ref m_isInteractable, ref value, IsInteractableChanged);
        }

        [Description("How long OSD is displayed (in milliseconds)")]
        public uint DurationMs
        {
            get => m_durationMs;
            set => HandlePropertyChange(ref m_durationMs, ref value, DurationMsChanged);
        }

        [Description("From 0.0 to 1.0. The recommended is 0.8 - 0.95")]
        public double BackgroundOpacity
        {
            get => m_backgroundOpacity;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                else if (value > 1)
                {
                    value = 1;
                }
                HandlePropertyChange(ref m_backgroundOpacity, ref value, BackgroundOpacityChanged);
            }
        }

        public HorAlignment HorizontalAlignment
        {
            get => m_horizontalAlignment;
            set => HandlePropertyChange(ref m_horizontalAlignment, ref value, HorizontalAlignmentChanged);
        }

        public VertAlignment VerticalAlignment
        {
            get => m_verticalAlignment;
            set => HandlePropertyChange(ref m_verticalAlignment, ref value, VerticalAlignmentChanged);
        }

        public event EventHandler<uint> DisplayIndexChanged;
        public event EventHandler<bool> DontShowIfVoicemeeterVisibleChanged;
        public event EventHandler<bool> IsInteractableChanged;
        public event EventHandler<uint> DurationMsChanged;
        public event EventHandler<double> BackgroundOpacityChanged;
        public event EventHandler<HorAlignment> HorizontalAlignmentChanged;
        public event EventHandler<VertAlignment> VerticalAlignmentChanged;
    }
}
