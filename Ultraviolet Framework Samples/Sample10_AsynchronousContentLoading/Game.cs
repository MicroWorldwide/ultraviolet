﻿using System;
using TwistedLogik.Nucleus;
using TwistedLogik.Ultraviolet;
using TwistedLogik.Ultraviolet.Content;
using TwistedLogik.Ultraviolet.OpenGL;
using UltravioletSample.Sample10_AsynchronousContentLoading.Assets;
using UltravioletSample.Sample10_AsynchronousContentLoading.Input;
using UltravioletSample.Sample10_AsynchronousContentLoading.UI;
using UltravioletSample.Sample10_AsynchronousContentLoading.UI.Screens;

namespace UltravioletSample.Sample10_AsynchronousContentLoading
{
#if ANDROID
    [Android.App.Activity(Label = "Sample 10 - Asynchronous Content Loading", MainLauncher = true, ConfigurationChanges = 
        Android.Content.PM.ConfigChanges.Orientation | 
        Android.Content.PM.ConfigChanges.ScreenSize | 
        Android.Content.PM.ConfigChanges.KeyboardHidden)]
#endif
    public class Game : SampleApplicationBase2
    {
        public Game()
            : base("TwistedLogik", "Sample 10 - Asynchronous Content Loading", uv => uv.GetInput().GetActions())
        {
#if IOS
            EnsureAssemblyIsLinked<TwistedLogik.Ultraviolet.BASS.BASSUltravioletAudio>();
#endif
        }

        public static void Main(String[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }

        protected override UltravioletContext OnCreatingUltravioletContext()
        {
            return new OpenGLUltravioletContext(this);
        }

        protected override void OnLoadingContent()
        {
            this.content = ContentManager.Create("Content");
            LoadContentManifests(content);
            LoadLocalizationDatabases(content);

            var screenService = new UIScreenService(content);
            var screen = screenService.Get<LoadingScreen>();

            screen.SetContentManager(this.content);
            screen.AddLoadingStep("Loading, please wait...");
            screen.AddLoadingStep(Ultraviolet.GetContent().Manifests["Global"]);
            screen.AddLoadingDelay(2500);
            screen.AddLoadingStep("Loading interface...");
            screen.AddLoadingStep(screenService.Load);
            screen.AddLoadingDelay(2500);
            screen.AddLoadingStep("Reticulating splines...");
            screen.AddLoadingDelay(2500);

            Ultraviolet.GetUI().GetScreens().Open(screen, TimeSpan.Zero);

            base.OnLoadingContent();
        }

        protected override void OnUpdating(UltravioletTime time)
        {
            if (Ultraviolet.GetInput().GetActions().ExitApplication.IsPressed())
            {
                Exit();
            }
            base.OnUpdating(time);
        }

        protected override void OnDrawing(UltravioletTime time)
        {
            base.OnDrawing(time);
        }

        protected override void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                SafeDispose.Dispose(content);
            }
            base.Dispose(disposing);
        }
        
        protected override void LoadContentManifests(ContentManager content)
        {
            base.LoadContentManifests(content);

            Ultraviolet.GetContent().Manifests["Global"]["Fonts"].PopulateAssetLibrary(typeof(GlobalFontID));
            Ultraviolet.GetContent().Manifests["Global"]["Textures"].PopulateAssetLibrary(typeof(GlobalTextureID));
        }

        private ContentManager content;
    }
}
