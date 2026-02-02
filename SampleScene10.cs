using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// 最初のシーン
    /// </summary>
    public class SampleScene10 : IScene
    {
        private int filterState = 0;
        private const int MAX_FILTER_COUNT = 16;

        // アニメーション状態保持用
        private TonAnimState anim = new TonAnimState
        {
            x1 = 0,
            y1 = 0,
            width = 64,
            height = 64,
            FrameCount = 6,
            FrameDuration = 125,
            IsLoop = true
        };

        // アニメーション状態保持用2
        private TonAnimState anim2 = new TonAnimState
        {
            x1 = 0,
            y1 = 64,
            width = 64,
            height = 64,
            FrameCount = 6,
            FrameDuration = 180,
            IsLoop = false
        };
        
        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // 永続的に使用しないリソースはここでロード(LoadTexture()など)してください
            Ton.Gra.CreateRenderTarget("SubLayer", Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);

            // 初期化処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Initialized.");
        }

        public void Terminate()
        {
            // 終了処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminating.");

            // 永続的に使用しないリソースはここでアンロード(UnloadTexture()など)してください(自動解放を待ちたくない場合)

            // 終了処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        public void Update(GameTime gameTime)
        {
            if(Ton.Input.IsJustPressed("Right"))
            {
                filterState++;
                if(filterState == MAX_FILTER_COUNT)
                {
                    filterState = 0;
                }
            }
            else if (Ton.Input.IsJustPressed("Left"))
            {
                filterState--;
                if (filterState < 0)
                {
                    filterState = MAX_FILTER_COUNT - 1;
                }
            }

            if(Ton.Input.GetPressedDuration("A") > 1.0f)
            {
                Ton.Scene.Change(new SampleScene11(), 0.5f, 0.2f, Color.White);
            }
        }

        public void Draw()
        {
            TonFilterParam filter = null;
            string strFilter = "";

            double filterTime = Ton.Game.TotalGameTime.TotalSeconds;

            // フィルタ処理
            switch (filterState)
            {
                case 0:
                    {
                        // Flash greyscale
                        filter = new TonFilterParam(ScreenFilterType.Greyscale, (float)((Math.Sin(filterTime * 4.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.Greyscale.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 1:
                    {
                        // Flash sepia
                        filter = new TonFilterParam(ScreenFilterType.Sepia, (float)((Math.Sin(filterTime * 4.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.Sepia.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 2:
                    {
                        // Flash scanline
                        filter = new TonFilterParam(ScreenFilterType.ScanLine, (float)((Math.Sin(filterTime * 4.0) * 0.2) + 0.5));
                        strFilter = ScreenFilterType.ScanLine.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 3:
                    {
                        // Flash mosaic
                        filter = new TonFilterParam(ScreenFilterType.Mosaic, (float)((Math.Sin(filterTime * 4.0) * 10.0) + 12.0));
                        strFilter = ScreenFilterType.Mosaic.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 4:
                    {
                        // Flash blur
                        filter = new TonFilterParam(ScreenFilterType.Blur, (float)((Math.Sin(filterTime * 4.0) * 4.0) + 5.0));
                        strFilter = ScreenFilterType.Blur.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 5:
                    {
                        // Flash chromatic aberration
                        filter = new TonFilterParam(ScreenFilterType.ChromaticAberration, (float)((Math.Sin(filterTime * 4.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.ChromaticAberration.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 6:
                    {
                        // Flash vignette
                        filter = new TonFilterParam(ScreenFilterType.Vignette, (float)((Math.Sin(filterTime * 4.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.Vignette.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 7:
                    {
                         // Flash invert
                        filter = new TonFilterParam(ScreenFilterType.Invert, (float)((Math.Sin(filterTime * 4.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.Invert.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 8:
                    {
                        // Distortion (Wave) - Amount controls amplitude (0.0 to 1.0 maps to 0% to 5% distortion in shader)
                        filter = new TonFilterParam(ScreenFilterType.Distortion, (float)((Math.Sin(filterTime * 2.0) * 5.0) + 5.0));
                        strFilter = ScreenFilterType.Distortion.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 9:
                    {
                        // Noise - Amount controls intensity
                        filter = new TonFilterParam(ScreenFilterType.Noise, (float)((Math.Sin(filterTime * 4.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.Noise.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 10:
                    {
                        // Edge Detect - Amount blends between original and edge view
                        filter = new TonFilterParam(ScreenFilterType.EdgeDetect, (float)((Math.Sin(filterTime * 2.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.EdgeDetect.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 11:
                    {
                        // Radial Blur
                        filter = new TonFilterParam(ScreenFilterType.RadialBlur, (float)((Math.Sin(filterTime * 2.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.RadialBlur.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 12:
                    {
                        // Posterize
                        filter = new TonFilterParam(ScreenFilterType.Posterize, (float)((Math.Sin(filterTime * 1.0) * 0.2) + 0.8));
                        strFilter = ScreenFilterType.Posterize.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 13:
                    {
                        // Fish Eye
                        filter = new TonFilterParam(ScreenFilterType.FishEye, (float)((Math.Sin(filterTime * 2.0) * 0.5) + 0.5));
                        strFilter = ScreenFilterType.FishEye.ToString();
                        Ton.Gra.SetScreenFilter(null, filter);
                    }
                    break;
                case 14:
                    {
                        // Multi Filter (Sepia + ScanLine + Vignette)
                        Ton.Gra.ClearScreenFilter();
                        
                        var f1 = new TonFilterParam(ScreenFilterType.Sepia, 1.0f);
                        Ton.Gra.AddScreenFilter(null, f1);

                        var f2 = new TonFilterParam(ScreenFilterType.ScanLine, 0.5f);
                        Ton.Gra.AddScreenFilter(null, f2);
                        
                        var f3 = new TonFilterParam(ScreenFilterType.Vignette, 0.6f);
                        Ton.Gra.AddScreenFilter(null, f3);

                        strFilter = "Multi: Sepia+ScanLine+Vignette";
                    }
                    break;
                case 15:
                    strFilter = "Render Target Filter";
                    break;
                default:
                    break;
            }

            if (filter != null)
            {
                // フィルタを適用する
                Ton.Gra.SetScreenFilter(null, filter);
            }else if (filterState != 14)
            {
                // フィルタをクリアする (Case 14は個別に追加しているのでクリアしない)
                Ton.Gra.ClearScreenFilter();
            }

            // 描画処理
            DrawSub(strFilter);

            if (filterState == 15)
            {
                
                // レンダーターゲットを切り替える
                Ton.Gra.SetRenderTarget("SubLayer");

                // フィルタを適用する
                TonFilterParam paramfl = new TonFilterParam(ScreenFilterType.Greyscale, 1.0f);
                Ton.Gra.SetScreenFilter("SubLayer", paramfl);

                // 描画する
                DrawSub(strFilter);

                // レンダーターゲットを戻す
                Ton.Gra.SetRenderTarget();

                // 縮小描き込みする
                TonDrawParamEx param = new TonDrawParamEx(0.5f);
                Ton.Gra.DrawEx("SubLayer", Ton.Game.VirtualWidth / 2, Ton.Game.VirtualHeight / 2, 0, 0, Ton.Gra.GetTextureWidth("SubLayer"), Ton.Gra.GetTextureHeight("SubLayer"), param);

            }
        }

        public void DrawSub(string strFilter)
        {
            // 背景を表示する
            Ton.Gra.DrawBackground("landscape");

            // 背景は先頭で描画済み

            // テキスト表示
            Ton.Gra.DrawText("Filter Test", 10, 10);
            Ton.Gra.DrawText("Left <> Right Select Filter", 10, 60, 0.7f);
            Ton.Gra.DrawText(strFilter, 10, 90, Color.Black, 0.7f);

            // 通常描画
            Ton.Gra.Draw("coin_animation", 320, 330, 0, 0, 64, 64);

            // 加算合成
            Ton.Gra.SetBlendState(TonBlendState.Additive);
            Ton.Gra.Draw("coin_animation", 390, 330, 0, 256, 64, 64);

            // 不透明合成
            Ton.Gra.SetBlendState(TonBlendState.NonPremultiplied);
            Ton.Gra.Draw("coin_animation", 460, 330, 0, 64, 64, 64);

            // ブレンドモードを戻す
            Ton.Gra.SetBlendState(TonBlendState.AlphaBlend);

            // 左右反転
            TonDrawParam param = new TonDrawParam();
            param.FlipH = true;
            Ton.Gra.Draw("coin_animation", 530, 330, 128, 256, 64, 64, param);

            // 上下反転
            param.FlipH = false;
            param.FlipV = true;
            Ton.Gra.Draw("coin_animation", 600, 330, 128, 256, 64, 64, param);

            // 上下左右反転
            param.FlipH = true;
            Ton.Gra.Draw("coin_animation", 670, 330, 128, 256, 64, 64, param);

            // アルファ値の計算(Ton.Game.TotalGameTimeはDraw()内でも使用できる経過時間変数です)
            float fAlpha = (float)((Math.Sin(Ton.Game.TotalGameTime.TotalSeconds * 4.0) * 0.5) + 0.5);

            // アルファ値変更
            param.FlipH = false;
            param.Alpha = fAlpha;
            Ton.Gra.Draw("coin_animation", 730, 330, 192, 192, 64, 64, param);

            // カラー変更
            param.Alpha = 1.0f;
            param.Color = new Color(fAlpha, fAlpha, 1.0f);
            Ton.Gra.Draw("coin_animation", 800, 330, 192, 192, 64, 64, param);

            // 拡大表示
            TonDrawParamEx paramex = new TonDrawParamEx();
            paramex.ScaleX = (float)((Math.Sin(Ton.Game.TotalGameTime.TotalSeconds * 4.0) * 1.0) + 1.5);
            paramex.ScaleY = (float)((Math.Sin(Ton.Game.TotalGameTime.TotalSeconds * 6.0) * 1.0) + 1.5);
            Ton.Gra.DrawEx("coin_animation", 400.0f, 480.0f, 0, 0, 64, 64, paramex);

            // 拡大回転表示
            paramex.Angle = (float)(Ton.Game.TotalGameTime.TotalSeconds * 4.0);
            paramex.FlipH = true;
            Ton.Gra.DrawEx("coin_animation", 550.0f, 480.0f, 0, 0, 64, 64, paramex);

            // 拡大回転アルファ値変更
            paramex.Alpha = fAlpha;
            paramex.Angle = -paramex.Angle;
            Ton.Gra.DrawEx("coin_animation", 700.0f, 480.0f, 0, 0, 64, 64, paramex);

            // 拡大回転カラー変更
            paramex.Alpha = 1.0f;
            paramex.Angle = -paramex.Angle;
            paramex.Color = Color.Gray;
            Ton.Gra.DrawEx("coin_animation", 850.0f, 480.0f, 0, 0, 64, 64, paramex);

            // アニメーション(動きの更新はUpdate()でTonAnimState.Update()を実行)
            Ton.Gra.DrawAnim("coin_animation", 320, 560, anim);
            Ton.Gra.DrawAnim("coin_animation", 400, 560, anim2);

            // アニメーション(TonAnimState.Update()不要で簡易ループアニメする場合はCreateLoop()を使用)
            TonAnimState anim3 = TonAnimState.CreateLoop(0, 0, 64, 64, 6, 50, Ton.Game.TotalGameTime.TotalSeconds);
            Ton.Gra.DrawAnim("coin_animation", 480, 560, anim3);

            // アニメーション拡大回転半透明表示
            paramex.Angle = (float)(Ton.Game.TotalGameTime.TotalSeconds * 2.0);
            paramex.FlipH = false;
            paramex.Alpha = fAlpha;
            paramex.Color = Color.White;
            Ton.Gra.DrawAnimEx("coin_animation", 640.0f, 600.0f, anim3, paramex);

            // FPS情報を表示
            String str = String.Format("FPS: (Update {0}, Draw {1}) FullScreen ({2}) Virtual Resolution ({3},{4})"
                , Math.Round(Ton.Game.UpdateFPS, MidpointRounding.AwayFromZero)
                , Math.Round(Ton.Game.DrawFPS, MidpointRounding.AwayFromZero)
                , Ton.Game.IsFullScreen, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
            Ton.Gra.DrawText(str, 10, Ton.Game.VirtualHeight - 30, 0.6f);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));
        }
    }
}
