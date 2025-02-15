﻿using AtgDev.Voicemeeter.Types;
using System.Collections.Generic;
using VoicemeeterOsdProgram.Core;
using VoicemeeterOsdProgram.Core.Types;
using VoicemeeterOsdProgram.UiControls.OSD;
using VoicemeeterOsdProgram.UiControls.OSD.Strip;

namespace VoicemeeterOsdProgram.Factories
{
    public static partial class OsdContentFactory
    {
        private static List<VoicemeeterParameterBase> m_vmParams;
        private static VoicemeeterProperties m_vmProperties;
        private static List<ButtonContainer> m_selButtons;

        public static void FillOsdWindow(ref OsdControl osd, ref VoicemeeterParameterBase[] vmParams, VoicemeeterType type)
        {
            m_vmProperties = new VoicemeeterProperties(type);
            m_vmParams = new();
            m_selButtons = new();
            var api = VoicemeeterApiClient.Api;

            osd.AllowAutoUpdateSeparators = false;

            // adding Hardware Inputs
            for (int i = 0; i < m_vmProperties.hardInputs; i++)
            {
                var strip = GetHardwareInputStrip(i);

                MakeLabelParam(strip, i, $"HardIn{i + 1}", StripType.Input);
                MakeFaderParam(strip, i, StripType.Input);

                osd.MainContent.Children.Add(strip);
            }

            // adding Virtual Inputs
            for (int i = 0; i < m_vmProperties.virtInputs; i++)
            {
                var stripIndex = m_vmProperties.hardInputs + i;

                var strip = GetVirtualInputStrip(stripIndex);

                MakeLabelParam(strip, stripIndex, $"VirtIn{i + 1}", StripType.Input);
                MakeFaderParam(strip, stripIndex, StripType.Input);

                osd.MainContent.Children.Add(strip);
            }

            // adding Physical Outputs
            for (int i = 0; i < m_vmProperties.hardOutputs; i++)
            {
                var strip = GetOutputStrip(i);
                var name = (m_vmProperties.hardOutputs == 1) ? "A" : $"A{i + 1}";
                strip.StripLabel.Text = name;

                MakeLabelParam(strip, i, name, StripType.Output);
                MakeFaderParam(strip, i, StripType.Output);

                osd.MainContent.Children.Add(strip);
            }

            // adding Virtual Outputs
            for (int i = 0; i < m_vmProperties.virtOutputs; i++)
            {
                var stripIndex = m_vmProperties.hardOutputs + i;

                var strip = GetOutputStrip(stripIndex);
                var name = (m_vmProperties.virtOutputs == 1) ? "B" : $"B{i + 1}";
                strip.StripLabel.Text = name;

                MakeFaderParam(strip, stripIndex, StripType.Output);

                osd.MainContent.Children.Add(strip);
            }

            osd.UpdateSeparators();
            osd.AllowAutoUpdateSeparators = true;

            // read first time to initialize values
            foreach (var p in m_vmParams)
            {
                p.Read();
            }

            vmParams = m_vmParams.ToArray();
            m_vmParams.Clear();
            m_vmParams = null;
            m_selButtons = null;
        }

        private static StripControl GetOutputStrip(int stripIndex)
        {
            var strip = new StripControl();

            var btn = StripButtonFactory.GetMonoWithReverse();
            MakeButtonParam(BtnType.Mono, StripType.Output, btn, stripIndex);
            strip.ControlBtnsContainer.Children.Add(btn);

            btn = StripButtonFactory.GetMute();
            MakeButtonParam(BtnType.Mute, StripType.Output, btn, stripIndex);
            strip.ControlBtnsContainer.Children.Add(btn);

            switch (m_vmProperties.type)
            {
                case VoicemeeterType.Potato:
                case VoicemeeterType.Potato64:
                    btn = StripButtonFactory.GetSel();
                    MakeButtonParam(BtnType.Sel, StripType.Output, btn, stripIndex);
                    strip.AdditionalControlBtns.Children.Add(btn);
                    break;
            }

            return strip;
        }

        private static StripControl GetInput(int stripIndex)
        {
            var strip = new StripControl();

            var btn = StripButtonFactory.GetSolo();
            MakeButtonParam(BtnType.Solo, StripType.Input, btn, stripIndex);
            strip.ControlBtnsContainer.Children.Add(btn);

            btn = StripButtonFactory.GetMute();
            MakeButtonParam(BtnType.Mute, StripType.Input, btn, stripIndex);
            strip.ControlBtnsContainer.Children.Add(btn);

            // adding A1, A2, ... buttons
            for (int i = 0; i < m_vmProperties.hardOutputs; i++)
            {
                var btnCont = StripButtonFactory.GetBusSelect();
                var busIndex = i + 1;
                var name = (m_vmProperties.hardOutputs == 1) ? $"A" : $"A{busIndex}";
                btnCont.Btn.Content = name;
                MakeButtonParam(BtnType.A, StripType.Input, btnCont, stripIndex, busIndex);

                strip.BusBtnsContainer.Children.Add(btnCont);
            }

            // adding B1, B2, ... buttons
            for (int i = 0; i < m_vmProperties.virtOutputs; i++)
            {
                var btnCont = StripButtonFactory.GetBusSelect();
                var busIndex = i + 1;
                var name = (m_vmProperties.virtOutputs == 1) ? $"B" : $"B{busIndex}";
                btnCont.Btn.Content = name;
                MakeButtonParam(BtnType.B, StripType.Input, btnCont, stripIndex, busIndex);

                strip.BusBtnsContainer.Children.Add(btnCont);
            }

            return strip;
        }

        private static StripControl GetVirtualInputStrip(int stripIndex)
        {
            return GetInput(stripIndex);
        }

        private static StripControl GetHardwareInputStrip(int stripIndex)
        {
            var strip = GetInput(stripIndex);
            var btn = StripButtonFactory.GetMono();
            MakeButtonParam(BtnType.Mono, StripType.Input, btn, stripIndex);
            strip.ControlBtnsContainer.Children.Insert(0, btn);
            return strip;
        }
    }
}
