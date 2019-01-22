﻿using System;
using SpiderEye.Configuration;
using SpiderEye.UI.Mac.Interop;
using SpiderEye.UI.Mac.Native;

namespace SpiderEye.UI.Mac
{
    internal class CocoaWindow : IWindow
    {
        public string Title { get; set; }

        public readonly IntPtr Handle;

        private readonly AppConfiguration config;
        private readonly CocoaWebview webview;

        public CocoaWindow(AppConfiguration config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            Handle = AppKit.Call("NSWindow", "alloc");

            var style = NSWindowStyleMask.Titled | NSWindowStyleMask.Closable | NSWindowStyleMask.Miniaturizable;
            if (config.Window.CanResize) { style |= NSWindowStyleMask.Resizable; }

            ObjC.SendMessage(
                Handle,
                ObjC.RegisterName("initWithContentRect:styleMask:backing:defer:"),
                new CGRect(0, 0, config.Window.Width, config.Window.Height),
                (int)style,
                2,
                0);

            SetTitle(config.Window.Title);

            IntPtr bgColor = NSColor.FromHex(config.Window.BackgroundColor);
            ObjC.Call(Handle, "setBackgroundColor:", bgColor);

            webview = new CocoaWebview(config);
            ObjC.Call(Handle, "setContentView:", webview.Handle);

            if (config.Window.UseBrowserTitle)
            {
                webview.TitleChanged += Webview_TitleChanged;
                if (config.EnableScriptInterface) { webview.ScriptHandler.TitleChanged += Webview_TitleChanged; }
            }
        }

        public void Show()
        {
            ObjC.Call(Handle, "center");
            ObjC.Call(Handle, "makeKeyAndOrderFront:", IntPtr.Zero);
        }

        public void Close()
        {
            ObjC.Call(Handle, "close", IntPtr.Zero);
        }

        public void LoadUrl(string url)
        {
            webview.NavigateToFile(url);
        }

        public void SetWindowState(WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    // TODO: restore window state when maximized
                    ObjC.Call(Handle, "deminiaturize", IntPtr.Zero);
                    break;

                case WindowState.Maximized:
                    // TODO: maximize window
                    // [window setFrame:[[window screen] frame] display YES]
                    break;

                case WindowState.Minimized:
                    ObjC.Call(Handle, "miniaturize", IntPtr.Zero);
                    break;

                default:
                    throw new ArgumentException($"Invalid window state of \"{state}\"", nameof(state));
            }
        }

        public void Dispose()
        {
            // will be released automatically
        }

        private void Webview_TitleChanged(object sender, string title)
        {
            if (title != null) { SetTitle(title); }
        }

        private void SetTitle(string title)
        {
            NSString.Use(title, nsTitle => ObjC.Call(Handle, "setTitle:", nsTitle));
        }
    }
}
