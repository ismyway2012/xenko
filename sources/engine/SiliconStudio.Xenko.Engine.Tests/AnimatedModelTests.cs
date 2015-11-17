﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Animations;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Input;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Rendering.Lights;
using SiliconStudio.Xenko.Rendering.ProceduralModels;
using SiliconStudio.Xenko.Rendering.Tessellation;

namespace SiliconStudio.Xenko.Engine.Tests
{
    public class AnimatedModelTests : EngineTestBase
    {
        private Entity knight;
        private AnimationClip megalodonClip;
        private AnimationClip knightOptimizedClip;
        private TestCamera camera;

        public AnimatedModelTests()
        {
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_0 };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var knightModel = Asset.Load<Model>("knight Model");
            knight = new Entity { new ModelComponent { Model = knightModel } };
            knight.Transform.Position = new Vector3(0, 0f, 0f);
            var animationComponent = knight.GetOrCreate<AnimationComponent>();
            animationComponent.Animations.Add("Run", Asset.Load<AnimationClip>("knight Run"));
            animationComponent.Animations.Add("Idle", Asset.Load<AnimationClip>("knight Idle"));

            // We will test both non-optimized and optimized clips
            megalodonClip = CreateModelChangeAnimation(new ProceduralModelDescriptor(new CubeProceduralModel { Size = Vector3.One, MaterialInstance = { Material = knightModel.Materials[0].Material } }).GenerateModel(Services));
            knightOptimizedClip = CreateModelChangeAnimation(Asset.Load<Model>("knight Model"));
            knightOptimizedClip.Optimize();

            animationComponent.Animations.Add("ChangeModel1", megalodonClip);
            animationComponent.Animations.Add("ChangeModel2", knightOptimizedClip);

            Scene.Entities.Add(knight);

            camera = new TestCamera();
            CameraComponent = camera.Camera;
            Script.Add(camera);

            LightingKeys.EnableFixedAmbientLight(GraphicsDevice.Parameters, true);
            GraphicsDevice.Parameters.Set(EnvironmentLightKeys.GetParameterKey(LightSimpleAmbientKeys.AmbientLight, 0), (Color3)Color.White);

            camera.Position = new Vector3(6.0f, 2.5f, 1.5f);
            camera.SetTarget(knight, true);
        }

        private AnimationClip CreateModelChangeAnimation(Model model)
        {
            var changeMegalodonAnimClip = new AnimationClip();
            var modelCurve = new AnimationCurve<object>();
            modelCurve.KeyFrames.Add(new KeyFrameData<object>(CompressedTimeSpan.Zero, model));
            changeMegalodonAnimClip.AddCurve("[ModelComponent.Key].Model", modelCurve);

            return changeMegalodonAnimClip;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            // Initial frame (no anim
            FrameGameSystem.Draw(() => { }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // T = 0
                var playingAnimation = knight.Get<AnimationComponent>().Play("Run");
                playingAnimation.Enabled = false;
            }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // T = 0.5sec
                var playingAnimation = knight.Get<AnimationComponent>().PlayingAnimations.First();
                playingAnimation.CurrentTime = TimeSpan.FromSeconds(0.5f);
            }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // Blend with Idle (both weighted 1.0f)
                var playingAnimation = knight.Get<AnimationComponent>().Blend("Idle", 1.0f, TimeSpan.Zero);
                playingAnimation.Enabled = false;
            }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // Update the model itself
                knight.Get<AnimationComponent>().Play("ChangeModel1");
            }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // Update the model itself (blend it at 2 vs 1 to force it to be active directly)
                var playingAnimation = knight.Get<AnimationComponent>().Blend("ChangeModel2", 2.0f, TimeSpan.Zero);
                playingAnimation.Enabled = false;
            }).TakeScreenshot();
        }

        [Test]
        public void RunTestGame()
        {
            RunGameTest(new AnimatedModelTests());
        }

        static public void Main()
        {
            using (var game = new AnimatedModelTests())
            {
                game.Run();
            }
        }
    }
}