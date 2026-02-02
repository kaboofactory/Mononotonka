using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Mononotonka
{
    /// <summary>
    /// グラフィックス管理クラスです。
    /// テクスチャの読み込み、描画(SpriteBatch)、画面効果(シェイク、フィルター)などを担当します。
    /// </summary>
    public class TonGraphics
    {
        private Game _game;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _defaultFont;
        private bool _useAA = false;
        private TonBlendState _currentBlendState;

        // リソース管理クラス
        private class ResourceInfo
        {
            public Texture2D Texture;
            public string ContentId; // 所属するContentGroup ID
            public long Size; // バイト数
            public bool IsManual; // 手動生成リソースかどうか（ContentManager管理外）
        }

        // コンテンツグループ管理クラス
        private class ContentGroup
        {
            public ContentManager Manager;
            public double LastUsed;
            public List<string> ResourceNames = new List<string>(); // このグループに属するリソース名のリスト

            public ContentGroup(IServiceProvider serviceProvider, string rootDirectory)
            {
                Manager = new ContentManager(serviceProvider, rootDirectory);
            }
        }

        // メモリ使用量（推定）
        private long _totalTextureMemory = 0;
        private long _maxTextureMemory = 0;

        /// <summary>
        /// 現在のテクスチャメモリ使用量（バイト）を取得します。
        /// </summary>
        public long TotalTextureMemory => _totalTextureMemory;

        /// <summary>
        /// 最大テクスチャメモリ使用量（バイト）を取得します。
        /// </summary>
        public long MaxTextureMemory => _maxTextureMemory;

        private long GetTextureSize(Texture2D tex)
        {
             if (tex == null) return 0;
             // 簡易計算: 幅 * 高さ * 4バイト (RGBA32相当)
             return (long)tex.Width * tex.Height * 4;
        }

        private string FormatBytes(long bytes)
        {
            double mb = bytes / (1024.0 * 1024.0);
            return $"{mb:F2} MB";
        }

        // リソースキャッシュ
        // リソースキャッシュ
        private Dictionary<string, ResourceInfo> _resources = new Dictionary<string, ResourceInfo>(StringComparer.OrdinalIgnoreCase);
        
        // コンテンツグループ管理
        private Dictionary<string, ContentGroup> _contentGroups = new Dictionary<string, ContentGroup>(StringComparer.OrdinalIgnoreCase);

        private double _cacheTimeout = 300.0; // 秒（未使用リソースの破棄基準）

        // レンダーターゲット（仮想画面など）
        private RenderTarget2D _virtualScreen;
        private RenderTarget2D _tempScreen; // multiple filter ping-pong buff
        private Dictionary<string, RenderTarget2D> _renderTargets = new Dictionary<string, RenderTarget2D>(StringComparer.OrdinalIgnoreCase);
        private string _currentTargetName = null;

        private Effect _mainEffect;
        private List<TonFilterParam> _currentFilters = new List<TonFilterParam>();
        private Dictionary<string, List<TonFilterParam>> _targetFilters = new Dictionary<string, List<TonFilterParam>>(StringComparer.OrdinalIgnoreCase);

        // 画面シェイク
        private float _shakeTime = 0;
        private float _shakeRatioX = 0;
        private float _shakeRatioY = 0;
        private Vector2 _shakeOffset = Vector2.Zero;
        private float _shakeFrequency = 20.0f; // 振動周波数（回/秒）
        private float _shakeTimer = 0;         // 更新用タイマー

        // 塗りつぶし用白テクスチャ
        // 塗りつぶし用白テクスチャ
        private Texture2D _pixel;
        // フォールバック用テクスチャ（共有）
        private Texture2D _fallbackTexture;

        // 現在のフレーム時刻（リソース管理用）
        private double _currentFrameTime = 0;
        private double _lastCleanupTime = 0;

        /// <summary>
        /// 初期化処理です。
        /// </summary>
        /// <param name="game">Gameインスタンス</param>
        /// <param name="graphics">GraphicsDeviceManagerインスタンス</param>
        public void Initialize(Game game, GraphicsDeviceManager graphics)
        {
            _game = game;
            _graphics = graphics;
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            // 塗りつぶし用ピクセルの生成
            _pixel = new Texture2D(game.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // 仮想画面の生成
            _virtualScreen = new RenderTarget2D(game.GraphicsDevice, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempScreen = new RenderTarget2D(game.GraphicsDevice, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            
            // "Default" グループの作成
            GetOrCreateContentGroup("Default");

            // シェーダーの読み込み（呼び出し側で用意されていると仮定）
            // コンテンツパイプライン経由で "shader/Filter" がある場合など

            // アンチエイリアスの初期設定（デフォルト有効）
            SetAntiAliasing(true);
        }

        // ヘルパー: グループ取得・作成
        private ContentGroup GetOrCreateContentGroup(string contentId)
        {
            if (!_contentGroups.ContainsKey(contentId))
            {
                var group = new ContentGroup(_game.Services, "Content");
                group.LastUsed = 0; 
                _contentGroups[contentId] = group;
                Ton.Log.Info($"[Graphics] Created ContentGroup: {contentId}");
            }
            return _contentGroups[contentId];
        }

        // ヘルパー: LastUsed更新
        private void UpdateLastUsed(string contentId, double totalSeconds)
        {
             if (_contentGroups.ContainsKey(contentId))
             {
                 _contentGroups[contentId].LastUsed = totalSeconds;
             }
        }

        /// <summary>
        /// 終了処理。テクスチャなどのリソースを解放します。
        /// </summary>
        public void Terminate()
        {
            // グループごとの解放
            foreach (var group in _contentGroups.Values)
            {
                group.Manager.Unload();
                group.Manager.Dispose();
            }
            _contentGroups.Clear();

            // 手動管理リソースの解放
            foreach (var res in _resources.Values)
            {
                if (res.IsManual && res.Texture != null && !res.Texture.IsDisposed)
                {
                    res.Texture.Dispose();
                }
            }
            _resources.Clear();
            
            foreach (var rt in _renderTargets.Values) rt.Dispose();
            _renderTargets.Clear();
            
            _virtualScreen?.Dispose();
            _tempScreen?.Dispose();
            _pixel.Dispose();
            _fallbackTexture?.Dispose();
            _spriteBatch.Dispose();
        }

        /// <summary>
        /// 未使用リソースの破棄時間を設定します。
        /// デフォルトは300秒(5分)です。
        /// </summary>
        /// <param name="seconds">秒数</param>
        public void SetCacheTimeout(double seconds)
        {
            _cacheTimeout = seconds;
        }

        /// <summary>
        /// 更新処理。定期的なリソース管理（未使用テクスチャの解放など）やシェイク更新を行います。
        /// </summary>
        public void Update(GameTime gameTime)
        {
            double now = gameTime.TotalGameTime.TotalSeconds;
            _currentFrameTime = now;

            // 10秒ごとにチェック
            if (now - _lastCleanupTime >= 10.0) 
            {
               _lastCleanupTime = now;
               
               // グループごとのクリーンアップ
               List<string> removeGroups = new List<string>();
               foreach (var kvp in _contentGroups)
               {
                   if (kvp.Key == "Default") continue; // Defaultは除外
                   
                   if (now - kvp.Value.LastUsed > _cacheTimeout)
                   {
                       removeGroups.Add(kvp.Key);
                   }
               }
               
               foreach (string gid in removeGroups)
               {
                   Unload(gid);
                   Ton.Log.Info($"[Graphics] Auto-released ContentGroup: {gid}");
               }
            }

            // シェイク更新
            if (_shakeTime > 0)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                _shakeTime -= dt;

                if (_shakeTime <= 0)
                {
                    _shakeTime = 0;
                    _shakeOffset = Vector2.Zero;
                }
                else
                {
                    _shakeTimer -= dt;
                    if (_shakeTimer <= 0)
                    {
                        // 次の更新までの時間をセット
                        float interval = 1.0f / Math.Max(_shakeFrequency, 1.0f); // 0除算防止
                        _shakeTimer += interval;

                        float ox = (Ton.Math.RandF(-1f, 1f)) * _shakeRatioX * Ton.Game.VirtualWidth;
                        float oy = (Ton.Math.RandF(-1f, 1f)) * _shakeRatioY * Ton.Game.VirtualHeight;
                        _shakeOffset = new Vector2(ox, oy);
                    }
                }
            }
        }

        /// <summary>
        /// テクスチャを読み込みます。すでに読み込み済みの場合はキャッシュから返します。
        /// </summary>
        /// <param name="path">読み込む画像ファイルのパス</param>
        /// <param name="name">登録名（キー）</param>
        /// <param name="isPermanent">永続リソースかどうか</param>
        /// <returns>読み込んだテクスチャ</returns>
        /// <summary>
        /// テクスチャを読み込みます。すでに読み込み済みの場合はキャッシュから返します。
        /// </summary>
        /// <param name="path">読み込む画像ファイルのパス</param>
        /// <param name="name">登録名（キー）</param>
        /// <param name="contentId">所属するコンテンツグループID</param>
        /// <returns>読み込んだテクスチャ</returns>
        public Texture2D LoadTexture(string path, string name, string contentId = "Default")
        {
            if (_resources.ContainsKey(name))
            {
                var res = _resources[name];
                UpdateLastUsed(res.ContentId, _currentFrameTime);
                return res.Texture;
            }

            var group = GetOrCreateContentGroup(contentId);

            // ContentManager経由で読み込み
            try
            {
                Texture2D tex = group.Manager.Load<Texture2D>(path);
                long size = GetTextureSize(tex);
                var info = new ResourceInfo
                {
                    Texture = tex,
                    ContentId = contentId,
                    IsManual = false,
                    Size = size
                };
                _resources[name] = info;
                group.ResourceNames.Add(name);

                // Usage更新
                UpdateLastUsed(contentId, _currentFrameTime);
                
                _totalTextureMemory += size;
                if (_totalTextureMemory > _maxTextureMemory) _maxTextureMemory = _totalTextureMemory;
                Ton.Log.Info($"[MEM] Loaded Texture: {name} (Size: {FormatBytes(size)}). (Group: {contentId})");
                
                return tex;
            }
            catch(Exception ex)
            {
                Ton.Log.Error($"Failed to load texture {path}: {ex.Message}");

                // エラー時のフォールバック（共有）
                MakeFallbackTexture(name, contentId);

                // フォールバックを使った場合もグループには登録せず、リソース辞書のみ
                // UpdateLastUsedはしないでおく（中身が無いので）か、あるいはDefault扱いにしても良いが、
                // フォールバック自体はGlobalなものなので管理外とする手もある
                
                return _fallbackTexture;
            }
        }

        /// <summary>
        /// 共用のエラーテクスチャを生成する
        /// </summary>
        private void MakeFallbackTexture(string name, string contentId = "Default")
        {
            if (_fallbackTexture == null)
            {
                int w = 10;
                int h = 10;
                _fallbackTexture = new Texture2D(_game.GraphicsDevice, w, h);
                Color[] data = new Color[w * h];
                for (int i = 0; i < data.Length; ++i) data[i] = Color.Magenta;
                _fallbackTexture.SetData(data);
            }

            var info = new ResourceInfo
            {
                Texture = _fallbackTexture,
                ContentId = contentId,
                IsManual = true, // フォールバックは一応Manual扱いにしておくが、DisposeはTerminateでのみ行う
                Size = 10 * 10 * 4 // 10x10 * 4byte
            };

            _resources[name] = info;
        }

        /// <summary>
        /// 指定したテクスチャをメモリから解放します。
        /// </summary>
        /// <param name="name">登録名</param>
        /// <summary>
        /// 指定したコンテンツグループのリソースをすべて解放します。
        /// </summary>
        public void Unload(string contentId)
        {
            if (contentId == "Default")
            {
                Ton.Log.Warning("Cannot unload 'Default' content group.");
                return;
            }

            if (_contentGroups.ContainsKey(contentId))
            {
                var group = _contentGroups[contentId];
                
                // リソース辞書からの削除
                foreach (var resName in group.ResourceNames)
                {
                    if (_resources.ContainsKey(resName))
                    {
                        var res = _resources[resName];
                        _totalTextureMemory -= res.Size;
                        
                        // IsManualなものは個別にDisposeが必要だが、通常のTextureはManagerとともに消える
                        // ただし、もしIsManualなものがグループに入っている場合はここでDispose
                        if (res.IsManual && res.Texture != null && !res.Texture.IsDisposed && res.Texture != _fallbackTexture)
                        {
                            res.Texture.Dispose();
                        }
                        
                        _resources.Remove(resName);
                    }
                }

                // ContentManagerのアンロードと破棄
                group.Manager.Unload();
                group.Manager.Dispose();
                
                _contentGroups.Remove(contentId);
                Ton.Log.Info($"[Graphics] Unloaded ContentGroup: {contentId}");
            }
        }
        
        // 内部ヘルパー：テクスチャを取得
        private Texture2D GetTexture(string name)
        {
            if (_resources.ContainsKey(name))
            {
                 var res = _resources[name];
                 UpdateLastUsed(res.ContentId, _currentFrameTime);
                 return res.Texture;
            }

            // レンダーターゲットも検索対象にする
            if (_renderTargets.ContainsKey(name))
            {
                return _renderTargets[name];
            }

            // 見つからない場合は共有のマゼンタテクスチャを返す
            MakeFallbackTexture(name);

            return _fallbackTexture;
        }

        /// <summary>
        /// 指定した画像（またはレンダーターゲット）の幅を取得します。
        /// 見つからない場合は0を返します。
        /// </summary>
        public int GetTextureWidth(string name)
        {
            var tex = GetTexture(name);
            return tex?.Width ?? 0;
        }

        /// <summary>
        /// 指定した画像（またはレンダーターゲット）の高さを取得します。
        /// 見つからない場合は0を返します。
        /// </summary>
        public int GetTextureHeight(string name)
        {
            var tex = GetTexture(name);
            return tex?.Height ?? 0;
        }

        /// <summary>
        /// 描画先（レンダーターゲット）を設定します。
        /// </summary>
        /// <param name="targetName">ターゲット名（nullの場合は仮想画面）</param>
        public void SetRenderTarget(string targetName = null)
        {
            _spriteBatch.End(); // 前の描画をフラッシュ
            _currentTargetName = targetName;
            
            if (targetName == null)
            {
                _game.GraphicsDevice.SetRenderTarget(_virtualScreen);
            }
            else if (_renderTargets.ContainsKey(targetName))
            {
                _game.GraphicsDevice.SetRenderTarget(_renderTargets[targetName]);
            }
            
            Begin(); // 描画再開
        }

        /// <summary>
        /// 新しいレンダーターゲットを作成します。
        /// </summary>
        /// <param name="targetName">登録名</param>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        public void CreateRenderTarget(string targetName, int width, int height)
        {
             var rt = new RenderTarget2D(_game.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
             _renderTargets[targetName] = rt;

             // レンダーターゲットもメモリとしてカウント（管理リストに入れないのでリーク注意だが、ここでは生成時のみログ）
             long size = GetTextureSize(rt);
             _totalTextureMemory += size;
             if (_totalTextureMemory > _maxTextureMemory) _maxTextureMemory = _totalTextureMemory;
             Ton.Log.Info($"[MEM] Created RenderTarget: {targetName} ({FormatBytes(size)}). Total: {FormatBytes(_totalTextureMemory)}");
        }

        /// <summary>
        /// 指定したレンダーターゲットを解放します。
        /// </summary>
        /// <param name="targetName">登録名</param>
        /// <summary>
        /// 指定したレンダーターゲットを解放します。
        /// </summary>
        /// <param name="targetName">登録名</param>
        public void ReleaseRenderTarget(string targetName)
        {
            if (_renderTargets.ContainsKey(targetName))
            {
                var rt = _renderTargets[targetName];
                long size = GetTextureSize(rt);
                
                if (rt != null && !rt.IsDisposed)
                    rt.Dispose();
                
                _renderTargets.Remove(targetName);
                
                _totalTextureMemory -= size;
                Ton.Log.Info($"[MEM] Released RenderTarget: {targetName} ({FormatBytes(size)}). Total: {FormatBytes(_totalTextureMemory)}");
            }
        }

        /// <summary>
        /// アンチエイリアス（線形補間サンプラー）の使用切り替えを設定します。
        /// 次の Begin() 呼び出しから適用されます。
        /// </summary>
        /// <param name="enabled">trueなら有効、falseなら無効（ドット絵向け）</param>
        public void SetAntiAliasing(bool enabled)
        {
            _useAA = enabled;
        }

        /// <summary>
        /// 現在設定されているブレンドステート（合成モード）を取得します。
        /// </summary>
        /// <returns>現在のブレンドステート</returns>
        public TonBlendState GetBlendState()
        {
            return _currentBlendState;
        }

        /// <summary>
        /// 画面を単色でクリアします。
        /// </summary>
        /// <param name="color">クリアする色（省略時は黒）</param>
        public void Clear(Color? color = null)
        {
            _game.GraphicsDevice.Clear(color ?? Color.Black);
        }

        /// <summary>
        /// 描画の開始を行います。通常は Ton.Draw から呼び出されます。
        /// デフォルトで AlphaBlend が適用されます。
        /// </summary>
        public void Begin()
        {
            // ターゲットが設定されていない（デフォルト）の場合、毎フレーム仮想画面をセットする必要がある
            // (End()でバックバッファに戻されているため)
            if (_currentTargetName == null)
            {
                 _game.GraphicsDevice.SetRenderTarget(_virtualScreen);
            }

            var sampler = _useAA ? SamplerState.LinearClamp : SamplerState.PointClamp;
            
            // 画面シェイク用の行列適用（メイン画面描画時のみ）
            Matrix transform = Matrix.Identity;
            if (_currentTargetName == null)
            {
                 transform = Matrix.CreateTranslation(_shakeOffset.X, _shakeOffset.Y, 0);
            }

            _currentBlendState = TonBlendState.AlphaBlend; // デフォルト設定
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, sampler, null, null, null, transform);
        }

        /// <summary>
        /// ブレンドステート（合成モード）を変更します。
        /// 一度現在のバッチを終了(End)し、新しい設定で開始(Begin)します。
        /// </summary>
        /// <param name="blend">適用するブレンドステート</param>
        public void SetBlendState(TonBlendState blend)
        {
            _spriteBatch.End();
            var sampler = _useAA ? SamplerState.LinearClamp : SamplerState.PointClamp;
            Matrix transform = Matrix.Identity;
            if (_currentTargetName == null)
            {
                 transform = Matrix.CreateTranslation(_shakeOffset.X, _shakeOffset.Y, 0);
            }
            
            _currentBlendState = blend;
            _spriteBatch.Begin(SpriteSortMode.Deferred, blend.State, sampler, null, null, null, transform);
        }

        /// <summary>
        /// 描画の終了を行います。
        /// 仮想画面への描画を行っていた場合、ここで実際の画面への転送とフィルター適用が行われます。
        /// </summary>

        public void End()
        {
            _spriteBatch.End();
            
            // 仮想画面への描画が終わった場合、それをバックバッファ（実画面）へ描画する
            if (_currentTargetName == null)
            {
                // Force Clear (Ensure RT is null)
                _game.GraphicsDevice.SetRenderTargets(null);
                _game.GraphicsDevice.Clear(Color.Black);

                // フィルターなしの場合
                if (_currentFilters.Count == 0)
                {
                    // 画面サイズに合わせてスケーリングして描画
                    var destRect = Ton.Game.GetScreenDestinationRect();
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null);
                    _spriteBatch.Draw(_virtualScreen, destRect, Color.White);
                    _spriteBatch.End();
                }
                // フィルターが1つの場合
                else if (_currentFilters.Count == 1)
                {
                    // フィルター適用のためのエフェクト設定
                    Effect effect = PrepareFilterEffect(_currentFilters[0], Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
                    // 画面サイズに合わせてスケーリングして描画
                    var destRect = Ton.Game.GetScreenDestinationRect();
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, effect);
                    _spriteBatch.Draw(_virtualScreen, destRect, Color.White); 
                    _spriteBatch.End();
                }
                // フィルターが複数の場合（ピンポンレンダリング）
                else
                {


                    
                    // パス1: VirtualScreen -> TempScreen (Filter 0)
                    _game.GraphicsDevice.SetRenderTarget(_tempScreen);
                    _game.GraphicsDevice.Clear(Color.Transparent);
                    Effect effect = PrepareFilterEffect(_currentFilters[0], Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, null, effect);
                    _spriteBatch.Draw(_virtualScreen, new Rectangle(0, 0, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight), Color.White);
                    _spriteBatch.End();

                    RenderTarget2D source = _tempScreen;
                    RenderTarget2D dest = _virtualScreen;

                    // パス2以降: Source -> Dest (Filter i)
                    for (int i = 1; i < _currentFilters.Count - 1; i++)
                    {
                        // Ton.Log.Info($"[DEBUG] Pass {i} Start.");
                        
                        _game.GraphicsDevice.SetRenderTarget(dest);
                        _game.GraphicsDevice.Clear(Color.Transparent);
                        Effect e = PrepareFilterEffect(_currentFilters[i], Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
                        
                        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, null, e);
                        _spriteBatch.Draw(source, new Rectangle(0, 0, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight), Color.White);
                        _spriteBatch.End();
                        
                        // Swap targets for next pass
                        var temp = source; source = dest; dest = temp;
                    }

                    // 最終パス: Current Source -> BackBuffer (Filter N-1)
                    _game.GraphicsDevice.SetRenderTarget(null);
                    _game.GraphicsDevice.Clear(Color.Black);
                    Effect finalEffect = PrepareFilterEffect(_currentFilters[_currentFilters.Count - 1], Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
                    
                    var destRect = Ton.Game.GetScreenDestinationRect();
                    _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, finalEffect);
                    _spriteBatch.Draw(source, destRect, Color.White);
                    _spriteBatch.End();
                }
            }
            else
            {
            }
        }

        /// <summary>
        /// SpriteBatchを一時中断します。
        /// Primitive描画など、SpriteBatch以外で描画する前に呼び出します。
        /// End()と異なり、画面への転送やフィルター処理は行いません。
        /// </summary>
        public void SuspendBatch()
        {
            _spriteBatch.End();
        }

        /// <summary>
        /// 中断したSpriteBatchを再開します。
        /// 直前のブレンドステートやTransformを維持して開始します。
        /// </summary>
        public void ResumeBatch()
        {
            var sampler = _useAA ? SamplerState.LinearClamp : SamplerState.PointClamp;
            
            Matrix transform = Matrix.Identity;
            if (_currentTargetName == null)
            {
                 transform = Matrix.CreateTranslation(_shakeOffset.X, _shakeOffset.Y, 0);
            }

            // _currentBlendStateはSuspend前のものが残っている前提
            if (_currentBlendState == null) _currentBlendState = TonBlendState.AlphaBlend;

            _spriteBatch.Begin(SpriteSortMode.Deferred, _currentBlendState.State, sampler, null, null, null, transform);
        }

        /// <summary>
        /// フィルターエフェクトを準備し、適用するEffectを返します。
        /// </summary>
        private Effect PrepareFilterEffect(TonFilterParam filter, int width, int height)
        {
            if (filter == null || filter.Type == ScreenFilterType.None) return null;

            // エフェクトの遅延読み込み
            if (_mainEffect == null)
            {
                try { _mainEffect = _game.Content.Load<Effect>("shader/Filter"); } 
                catch (Exception ex)
                {
                    Ton.Log.Error($"Failed to load shader/Filter: {ex.Message}");
                    return null;
                }
            }

            if (_mainEffect != null)
            {
                _mainEffect.Parameters["FilterType"]?.SetValue((float)filter.Type);
                _mainEffect.Parameters["Amount"]?.SetValue(filter.Amount);
                _mainEffect.Parameters["Resolution"]?.SetValue(new Vector2(width, height));
                _mainEffect.Parameters["Time"]?.SetValue((float)Ton.Game.TotalGameTime.TotalSeconds);
                return _mainEffect;
            }

            return null;
        }

        /// <summary>
        /// 複数フィルタ適用のためのヘルパーメソッド。
        /// フィルタ処理を行い、最終描画に使用するテクスチャとエフェクトを返します。
        /// </summary>
        private Texture2D ApplyMultiPassFilters(Texture2D source, Rectangle sourceRect, List<TonFilterParam> filters, out List<RenderTarget2D> garbage, out Effect finalEffect)
        {
            garbage = new List<RenderTarget2D>();
            finalEffect = null;

            if (filters == null || filters.Count == 0) return source;

            int w = sourceRect.Width;
            int h = sourceRect.Height;

            // 1つだけの場合は、最終描画時にEffectを適用するため、ここでは何もしない
            if (filters.Count == 1)
            {
                finalEffect = PrepareFilterEffect(filters[0], w, h);
                return source;
            }

            // 複数パス処理
            // Pass 1: Source(sourceRect) -> TempRT(0,0,w,h)
            
            // 現在のレンダーターゲット保存
            var oldRenderTargets = _game.GraphicsDevice.GetRenderTargets();

            // Pass 1
            _game.GraphicsDevice.SetRenderTarget(_tempScreen);
            _game.GraphicsDevice.Clear(Color.Transparent);
            Effect e0 = PrepareFilterEffect(filters[0], w, h);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, null, e0);
            _spriteBatch.Draw(source, new Rectangle(0, 0, w, h), sourceRect, Color.White);
            _spriteBatch.End();

            RenderTarget2D currentIn = _tempScreen;

            // Pass 2..N-1
            for (int i = 1; i < filters.Count - 1; i++)
            {
                var newRt = new RenderTarget2D(_game.GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None);
                garbage.Add(newRt);

                _game.GraphicsDevice.SetRenderTarget(newRt);
                _game.GraphicsDevice.Clear(Color.Transparent);
                Effect ei = PrepareFilterEffect(filters[i], w, h);

                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, null, ei);
                _spriteBatch.Draw(currentIn, new Rectangle(0, 0, w, h), new Rectangle(0, 0, w, h), Color.White);
                _spriteBatch.End();

                currentIn = newRt;
            }

            // Restore original targets
            _game.GraphicsDevice.SetRenderTargets(oldRenderTargets);

            // Prepare final effect (Last filter)
            finalEffect = PrepareFilterEffect(filters[filters.Count - 1], w, h);
            
            return currentIn;
        }

        /// <summary>
        /// 指定された画像のフィルタ一覧を取得します。
        /// </summary>
        private List<TonFilterParam> GetTargetFilters(string imageName)
        {
            if (_targetFilters.TryGetValue(imageName, out var fList) && fList != null && fList.Count > 0)
            {
                return fList;
            }
            return null;
        }

        /// <summary>
        /// フィルタ適用描画の共通実行メソッド。
        /// </summary>
        private void ExecuteDrawWithFilter(Texture2D tex, Rectangle src, List<TonFilterParam> filters, Action<Texture2D, Rectangle> drawAction)
        {
             _spriteBatch.End();

             List<RenderTarget2D> garbage;
             Effect finalEffect;
             Texture2D drawTex = ApplyMultiPassFilters(tex, src, filters, out garbage, out finalEffect);
             
             // 描画元矩形の調整（フィルタ済みテクスチャは(0,0,w,h)にトリミングされているため）
             Rectangle drawSrc = (drawTex == tex) ? src : new Rectangle(0, 0, src.Width, src.Height);

             // 現在のステートで開始
             var sampler = _useAA ? SamplerState.LinearClamp : SamplerState.PointClamp;
             Matrix transform = Matrix.Identity;
             if (_currentTargetName == null) transform = Matrix.CreateTranslation(_shakeOffset.X, _shakeOffset.Y, 0);

             _spriteBatch.Begin(SpriteSortMode.Deferred, _currentBlendState.State, sampler, null, null, finalEffect, transform);

             // 描画アクション実行
             drawAction(drawTex, drawSrc);

             _spriteBatch.End();

             // ステート復元
             _spriteBatch.Begin(SpriteSortMode.Deferred, _currentBlendState.State, sampler, null, null, null, transform);
             
             foreach(var g in garbage) g.Dispose();
        }

        /// <summary>
        /// 画像全体をそのまま描画します。
        /// </summary>
        /// <param name="imageName">画像名</param>
        /// <param name="toX">描画先X座標</param>
        /// <param name="toY">描画先Y座標</param>
        /// <param name="param">描画パラメータ（省略可）</param>
        public void Draw(string imageName, int toX, int toY, TonDrawParam param = null)
        {
            var tex = GetTexture(imageName);
            // 内部Drawを呼び出す
            // (GetTextureで取得したテクスチャのサイズを使用)
            Draw(imageName, toX, toY, 0, 0, tex.Width, tex.Height, param);
        }

        /// <summary>
        /// 通常の画像描画を行います（矩形指定）。
        /// </summary>
        /// <param name="imageName">画像名（ファイルパス）</param>
        /// <param name="toX">描画先X座標（画面上の位置）</param>
        /// <param name="toY">描画先Y座標（画面上の位置）</param>
        /// <param name="fromX">転送元X座標（テクスチャ上の位置）</param>
        /// <param name="fromY">転送元Y座標（テクスチャ上の位置）</param>
        /// <param name="w">幅</param>
        /// <param name="h">高さ</param>
        /// <param name="param">描画パラメータ（色、透明度、反転）</param>
        public void Draw(string imageName, int toX, int toY, int fromX, int fromY, int w, int h, TonDrawParam param = null)
        {
            var tex = GetTexture(imageName);
            if (param == null) param = new TonDrawParam();

            // クリッピング処理: 画面外なら描画しない
            if (toX + w <= 0 || toX >= Ton.Game.VirtualWidth || 
                toY + h <= 0 || toY >= Ton.Game.VirtualHeight)
            {
                return;
            }
            
            SpriteEffects effects = SpriteEffects.None;
            if (param.FlipH) effects |= SpriteEffects.FlipHorizontally;
            if (param.FlipV) effects |= SpriteEffects.FlipVertically;

            Rectangle src = new Rectangle(fromX, fromY, w, h);
            var filters = GetTargetFilters(imageName);

            if (filters != null)
            {
                ExecuteDrawWithFilter(tex, src, filters, (drawTex, drawSrc) => 
                {
                    _spriteBatch.Draw(drawTex, new Vector2(toX, toY), drawSrc, param.Color * param.Alpha, 0f, Vector2.Zero, 1.0f, effects, 0f);
                });
            }
            else
            {
                _spriteBatch.Draw(tex, new Vector2(toX, toY), src, param.Color * param.Alpha, 0f, Vector2.Zero, 1.0f, effects, 0f);
            }
        }

        /// <summary>
        /// 画像をアスペクト比を維持して画面いっぱいに描画します（背景用）。
        /// 画面サイズに合わせて自動的にスケーリングし、中央に配置します。
        /// 隙間ができないように計算上少しだけ大きめに描画します。
        /// </summary>
        /// <param name="imageName">画像名</param>
        /// <param name="param">描画パラメータ（省略可）</param>
        public void DrawBackground(string imageName, TonDrawParam param = null)
        {
            var tex = GetTexture(imageName);
            if (param == null) param = new TonDrawParam();

            int vw = Ton.Game.VirtualWidth;
            int vh = Ton.Game.VirtualHeight;

            // スケール計算: 画面を覆う最小のスケールを採用 (Math.Max)
            float scaleX = (float)vw / tex.Width;
            float scaleY = (float)vh / tex.Height;
            float scale = Math.Max(scaleX, scaleY);
            
            // ずれ防止で少し大きめに
            // scaleの精度問題での隙間回避のため、ピクセル単位での調整も含めるが
            // DrawExはScaleベースなので、scale自体をほんの少し大きくするか、
            // あるいは DrawEx に投げる際の scale を工夫する
            // ここではシンプルに scale を使用する（DrawEx側で中心描画されるため、わずかな隙間は周辺に出るが、アスペクト比維持Fillなら問題ないはず）
            // 念のため 0.1% 増やす
            scale *= 1.001f;

            // DrawEx に委譲
            // DrawEx は中心基準で描画する
            TonDrawParamEx paramEx = new TonDrawParamEx
            {
                Color = param.Color,
                Alpha = param.Alpha,
                FlipH = param.FlipH,
                FlipV = param.FlipV,
                ScaleX = scale,
                ScaleY = scale,
                Angle = 0f
            };

            DrawEx(imageName, vw / 2.0f, vh / 2.0f, 0, 0, tex.Width, tex.Height, paramEx);
        }

        /// <summary>
        /// 拡張パラメータを用いた画像描画を行います（回転・拡大縮小対応）。
        /// </summary>
        /// <param name="imageName">画像名</param>
        /// <param name="toX">描画先X座標</param>
        /// <param name="toY">描画先Y座標</param>
        /// <param name="fromX">転送元X座標</param>
        /// <param name="fromY">転送元Y座標</param>
        /// <param name="w">幅</param>
        /// <param name="h">高さ</param>
        /// <param name="param">拡張描画パラメータ</param>
        public void DrawEx(string imageName, float toX, float toY, int fromX, int fromY, int w, int h, TonDrawParamEx param)
        {
             var tex = GetTexture(imageName);
             if (param == null) param = new TonDrawParamEx();

            SpriteEffects effects = SpriteEffects.None;
            if (param.FlipH) effects |= SpriteEffects.FlipHorizontally;
            if (param.FlipV) effects |= SpriteEffects.FlipVertically;

            Rectangle src = new Rectangle(fromX, fromY, w, h);
            Vector2 origin = new Vector2(w / 2f, h / 2f); // 原点を中心に設定
            Vector2 scale = new Vector2(param.ScaleX, param.ScaleY);

            var filters = GetTargetFilters(imageName);

            if (filters != null)
            {
                ExecuteDrawWithFilter(tex, src, filters, (drawTex, drawSrc) =>
                {
                    _spriteBatch.Draw(drawTex, new Vector2(toX, toY), drawSrc, param.Color * param.Alpha, param.Angle, origin, scale, effects, 0f);
                });
            }
            else
            {
                _spriteBatch.Draw(tex, new Vector2(toX, toY), src, param.Color * param.Alpha, param.Angle, origin, scale, effects, 0f);
            }
        }

        /// <summary>
        /// アニメーション状態を使用した描画を行います。
        /// </summary>
        /// <param name="imageName">画像名</param>
        /// <param name="x">描画先X座標</param>
        /// <param name="y">描画先Y座標</param>
        /// <param name="anim">アニメーション状態</param>
        /// <param name="param">描画パラメータ</param>
        public void DrawAnim(string imageName, int x, int y, TonAnimState anim, TonDrawParam param = null)
        {
             var tex = GetTexture(imageName);
             if (param == null) param = new TonDrawParam();
             
             // クリッピング処理
             Rectangle src = anim.GetSourceRect();
             if (x + src.Width <= 0 || x >= Ton.Game.VirtualWidth || 
                 y + src.Height <= 0 || y >= Ton.Game.VirtualHeight)
             {
                 return;
             }

             SpriteEffects effects = SpriteEffects.None;
             if (param.FlipH) effects |= SpriteEffects.FlipHorizontally;
             if (param.FlipV) effects |= SpriteEffects.FlipVertically;

             // アニメーション状態からソース矩形を取得して描画
             var filters = GetTargetFilters(imageName);

             if (filters != null)
             {
                 ExecuteDrawWithFilter(tex, src, filters, (drawTex, drawSrc) =>
                 {
                     _spriteBatch.Draw(drawTex, new Vector2(x, y), drawSrc, param.Color * param.Alpha, 0f, Vector2.Zero, 1.0f, effects, 0f);
                 });
             }
             else
             {
                 _spriteBatch.Draw(tex, new Vector2(x, y), src, param.Color * param.Alpha, 0f, Vector2.Zero, 1.0f, effects, 0f);
             }
        }

        /// <summary>
        /// アニメーション状態を使用した拡張描画を行います。
        /// </summary>
        /// <param name="imageName">画像名</param>
        /// <param name="x">描画先X座標</param>
        /// <param name="y">描画先Y座標</param>
        /// <param name="anim">アニメーション状態</param>
        /// <param name="param">拡張描画パラメータ</param>
        public void DrawAnimEx(string imageName, float x, float y, TonAnimState anim, TonDrawParamEx param)
        {
            var tex = GetTexture(imageName);
             if (param == null) param = new TonDrawParamEx();
             
             SpriteEffects effects = SpriteEffects.None;
             if (param.FlipH) effects |= SpriteEffects.FlipHorizontally;
             if (param.FlipV) effects |= SpriteEffects.FlipVertically;

             Rectangle src = anim.GetSourceRect();
             Vector2 origin = new Vector2(src.Width / 2f, src.Height / 2f);
             Vector2 scale = new Vector2(param.ScaleX, param.ScaleY);

             var filters = GetTargetFilters(imageName);

             if (filters != null)
             {
                 ExecuteDrawWithFilter(tex, src, filters, (drawTex, drawSrc) =>
                 {
                     _spriteBatch.Draw(drawTex, new Vector2(x, y), drawSrc, param.Color * param.Alpha, param.Angle, origin, scale, effects, 0f);
                 });
             }
             else
             {
                 _spriteBatch.Draw(tex, new Vector2(x, y), src, param.Color * param.Alpha, param.Angle, origin, scale, effects, 0f);
             }
        }

        // フォント管理
        private Dictionary<string, SpriteFont> _fonts = new Dictionary<string, SpriteFont>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// フォントを読み込みます。
        /// </summary>
        /// <param name="path">コンテンツパス（例: "font/default"）</param>
        /// <param name="name">登録ID（デフォルトフォントは"default"を使用してください）</param>
        public void LoadFont(string path, string name)
        {
            try
            {
                if (_fonts.ContainsKey(name)) return;

                var font = _game.Content.Load<SpriteFont>(path);
                _fonts[name] = font;
                
                // フォントテクスチャのサイズ加算
                if (font.Texture != null)
                {
                    long size = GetTextureSize(font.Texture);
                    _totalTextureMemory += size;
                    if (_totalTextureMemory > _maxTextureMemory) _maxTextureMemory = _totalTextureMemory;
                    Ton.Log.Info($"[MEM] Loaded Font: {name} (Texture: {FormatBytes(size)}). Total: {FormatBytes(_totalTextureMemory)}");
                }
                
                // "default"という名前で登録された場合、またはまだデフォルトフォントがない場合、これをデフォルトとする
                if (name == "default" || _defaultFont == null)
                {
                    _defaultFont = font;
                }
            }
            catch (Exception ex)
            {
                Ton.Log.Error($"Failed to load font '{path}': {ex.Message}");
            }
        }

        /// <summary>
        /// 指定したフォントで文字列のサイズを計測します。
        /// </summary>
        /// <param name="text">計測する文字列</param>
        /// <param name="fontId">フォントID（nullの場合はデフォルトフォント）</param>
        /// <returns>文字列のサイズ(Vector2)</returns>
        public Vector2 MeasureString(string text, string fontId = null)
        {
            SpriteFont font = _defaultFont;
            if (!string.IsNullOrEmpty(fontId) && _fonts.ContainsKey(fontId))
            {
                font = _fonts[fontId];
            }

            if (font == null || string.IsNullOrEmpty(text))
            {
                return Vector2.Zero;
            }

            return font.MeasureString(text);
        }

        /// <summary>
        /// 指定したフォントがロードされているか確認します。
        /// </summary>
        public bool HasFont(string name)
        {
            return _fonts.ContainsKey(name);
        }

        /// <summary>
        /// テキストを描画します（全パラメータ指定版）。
        /// </summary>
        /// <param name="text">表示する文字列</param>
        /// <param name="x">描画先X座標</param>
        /// <param name="y">描画先Y座標</param>
        /// <param name="color">色（nullの場合はWhite）</param>
        /// <param name="scale">サイズ（標準=1.0）</param>
        /// <param name="fontId">フォントID（nullの場合はデフォルトフォント）</param>
        public void DrawText(string text, int x, int y, Color? color = null, float scale = 1.0f, string fontId = null)
        {
            // デフォルトフォントがロードされていない場合の安全策（本来はLoadFontでロードすべき）
            if (_defaultFont == null && !_fonts.ContainsKey("default"))
            {
                // 旧仕様の自動ロード（互換性のため残すが、ログ警告を出す）
                // Ton.Log.Warning("Default font not loaded via LoadFont. Attempting auto-load.");
                LoadFont("font/default", "default");
            }

            SpriteFont font = _defaultFont;
            if (!string.IsNullOrEmpty(fontId) && _fonts.ContainsKey(fontId))
            {
                font = _fonts[fontId];
            }
            
            Color drawColor = color ?? Color.White;

            if (font != null && text != null)
                _spriteBatch.DrawString(font, text, new Vector2(x, y), drawColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // --- 便利オーバーロード ---

        /// <summary>
        /// テキストを描画します（シンプル）。
        /// </summary>
        public void DrawText(string text, int x, int y)
        {
            DrawText(text, x, y, Color.White, 1.0f, null);
        }

        /// <summary>
        /// テキストを描画します（フォント指定）。
        /// </summary>
        public void DrawText(string text, int x, int y, string fontId)
        {
            DrawText(text, x, y, Color.White, 1.0f, fontId);
        }

        /// <summary>
        /// テキストを描画します（スケール指定）。
        /// </summary>
        public void DrawText(string text, int x, int y, float scale)
        {
            DrawText(text, x, y, Color.White, scale, null);
        }

        /// <summary>
        /// テキストを描画します（フォントとスケール指定）。
        /// </summary>
        public void DrawText(string text, int x, int y, string fontId, float scale)
        {
            DrawText(text, x, y, Color.White, scale, fontId);
        }

        /// <summary>
        /// テキストを描画します（フォントと色指定）。
        /// </summary>
        public void DrawText(string text, int x, int y, string fontId, Color color)
        {
            DrawText(text, x, y, color, 1.0f, fontId);
        }

        /// <summary>
        /// テキストを描画します（回転対応版）。
        /// 回転の中心は文字の中央になります。
        /// </summary>
        /// <param name="text">表示する文字列</param>
        /// <param name="x">描画先X座標（中心）</param>
        /// <param name="y">描画先Y座標（中心）</param>
        /// <param name="color">色</param>
        /// <param name="scale">サイズ</param>
        /// <param name="rotation">回転角度(ラジアン)</param>
        /// <param name="fontId">フォントID</param>
        public void DrawTextEx(string text, float x, float y, Color? color = null, float scale = 1.0f, float rotation = 0f, string fontId = null)
        {
            // デフォルトフォントがロードされていない場合の安全策
            if (_defaultFont == null && !_fonts.ContainsKey("default"))
            {
                LoadFont("font/default", "default");
            }

            SpriteFont font = _defaultFont;
            if (!string.IsNullOrEmpty(fontId) && _fonts.ContainsKey(fontId))
            {
                font = _fonts[fontId];
            }
            
            Color drawColor = color ?? Color.White;

            if (font != null && text != null)
            {
                // 回転中心を文字の中央にする
                Vector2 size = font.MeasureString(text);
                Vector2 origin = size / 2f;
                
                // 位置調整（左上が指定座標ではなく、中心が指定座標になるように描画されるため、
                // LayoutEngine側で考慮が必要だが、回転する場合は中心座標指定が一般的。
                // ただし既存のDrawTextは左上座標指定。
                // ここでは引数x,yは「描画したい文字の中心座標」ではなく「左上座標」として受け取り、
                // 内部で中心計算をしてからDrawStringに渡すか、あるいは呼び出し元が調整するか。
                // LayoutEngineは左上座標(CursorX, CursorY)を管理している。
                // つまり、(x + w/2, y + h/2) をPositionとし、Originを(w/2, h/2)とすれば、
                // 見かけ上は左上(x,y)にありつつ、その中心で回転する。
                
                Vector2 position = new Vector2(x + size.X * scale / 2f, y + size.Y * scale / 2f);

                _spriteBatch.DrawString(font, text, position, drawColor, rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// デバッグ用：指定した登録名のフォントテクスチャをPNGとして保存します。
        /// 名前を指定しない場合はデフォルトフォントを使用します。
        /// </summary>
        /// <param name="savePath">保存先のパス（相対パス可）</param>
        /// <param name="id">フォント登録ID（nullならデフォルト）</param>
        public void DebugSaveFontTexture(string savePath, string id = null)
        {
            SpriteFont targetFont = _defaultFont;
            
            if (!string.IsNullOrEmpty(id))
            {
                if (_fonts.ContainsKey(id)) targetFont = _fonts[id];
                else
                {
                    Ton.Log.Error($"DebugSaveFontTexture: Font '{id}' not found.");
                    return;
                }
            }
            
            // まだロードされていない場合の救済
            if (targetFont == null)
            {
                 LoadFont("font/default", "default");
                 targetFont = _defaultFont;
            }

            if (targetFont != null && targetFont.Texture != null)
            {
                // 情報ログ
                Ton.Log.Info($"DebugSaveFontTexture: Attempting to save '{id}' (Size: {targetFont.Texture.Width}x{targetFont.Texture.Height}, Format: {targetFont.Texture.Format})");

                try 
                {
                    // 圧縮フォーマット等で直接SaveAsPngできない場合があるため、RenderTargetを経由する
                    int w = targetFont.Texture.Width;
                    int h = targetFont.Texture.Height;

                    // 現在のステートを退避（念のため）
                    var previousTargets = _game.GraphicsDevice.GetRenderTargets();

                    using (var rt = new RenderTarget2D(_game.GraphicsDevice, w, h))
                    {
                        _game.GraphicsDevice.SetRenderTarget(rt);
                        _game.GraphicsDevice.Clear(Color.Transparent);

                        // テクスチャをそのまま描画
                        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                        _spriteBatch.Draw(targetFont.Texture, new Rectangle(0, 0, w, h), Color.White);
                        _spriteBatch.End();

                        // 描画先を戻す
                        _game.GraphicsDevice.SetRenderTargets(previousTargets);

                        // PNG保存
                        using (var stream = File.Create(savePath))
                        {
                            rt.SaveAsPng(stream, w, h);
                            stream.Flush(); 
                            Ton.Log.Info($"Font texture saved successfully to {Path.GetFullPath(savePath)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                     Ton.Log.Error($"Failed to save font texture: {ex.Message}");
                     Ton.Log.Error($"Stack Trace: {ex.StackTrace}");
                }
            }
            else
            {
                Ton.Log.Error($"DebugSaveFontTexture: Target font or texture is null for id '{id}'.");
            }
        }

        /// <summary>
        /// 矩形（四角形）を塗りつぶし描画します。
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="w">幅</param>
        /// <param name="h">高さ</param>
        /// <param name="color">色</param>
        public void FillRect(int x, int y, int w, int h, Color color)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);
        }

        /// <summary>
        /// 矩形の枠線を描画します。
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="w">幅</param>
        /// <param name="h">高さ</param>
        /// <param name="color">色</param>
        /// <param name="thickness">線の太さ（デフォルト: 1）</param>
        public void DrawRect(int x, int y, int w, int h, Color color, int thickness = 1)
        {
            FillRect(x, y, w, thickness, color); // 上
            FillRect(x, y + h - thickness, w, thickness, color); // 下
            FillRect(x, y, thickness, h, color); // 左
            FillRect(x + w - thickness, y, thickness, h, color); // 右
        }

        /// <summary>
        /// 直線を描画します。
        /// </summary>
        /// <param name="start">開始点</param>
        /// <param name="end">終了点</param>
        /// <param name="color">色</param>
        /// <param name="thickness">線の太さ</param>
        public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();

            _spriteBatch.Draw(_pixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0);
        }

        /// <summary>
        /// 直線を描画します（座標指定版）。
        /// </summary>
        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f)
        {
            DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), color, thickness);
        }

        /// <summary>
        /// 角丸矩形を描画します（9スライス処理）。
        /// </summary>
        /// <param name="imageName">9スライス用の画像名</param>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="w">全体の幅</param>
        /// <param name="h">全体の高さ</param>
        /// <param name="pw">角（パーツ）の幅</param>
        /// <param name="ph">角（パーツ）の高さ</param>
        public void FillRoundedRect(string imageName, int x, int y, int w, int h, int pw, int ph)
        {
            var tex = GetTexture(imageName);
            // 9スライス実装
            // 画像を9分割して、四隅はそのまま、上下左右は引き伸ばし、中央を引き伸ばして描画
            
            int x1 = x;
            int x2 = x + pw;
            int x3 = x + w - pw;
            
            int y1 = y;
            int y2 = y + ph;
            int y3 = y + h - ph;
            
            int cw = w - 2*pw;
            int ch = h - 2*ph;

            // 9箇所描画
            // 左上
            _spriteBatch.Draw(tex, new Rectangle(x1, y1, pw, ph), new Rectangle(0, 0, pw, ph), Color.White);
            // 上
            _spriteBatch.Draw(tex, new Rectangle(x2, y1, cw, ph), new Rectangle(pw, 0, pw, ph), Color.White);
            // 右上
            _spriteBatch.Draw(tex, new Rectangle(x3, y1, pw, ph), new Rectangle(2*pw, 0, pw, ph), Color.White);
            
            // 左中
            _spriteBatch.Draw(tex, new Rectangle(x1, y2, pw, ch), new Rectangle(0, ph, pw, ph), Color.White);
            // 中央
            _spriteBatch.Draw(tex, new Rectangle(x2, y2, cw, ch), new Rectangle(pw, ph, pw, ph), Color.White);
            // 右中
            _spriteBatch.Draw(tex, new Rectangle(x3, y2, pw, ch), new Rectangle(2*pw, ph, pw, ph), Color.White);
            
            // 左下
            _spriteBatch.Draw(tex, new Rectangle(x1, y3, pw, ph), new Rectangle(0, 2*ph, pw, ph), Color.White);
            // 下
            _spriteBatch.Draw(tex, new Rectangle(x2, y3, cw, ph), new Rectangle(pw, 2*ph, pw, ph), Color.White);
            // 右下
            _spriteBatch.Draw(tex, new Rectangle(x3, y3, pw, ph), new Rectangle(2*pw, 2*ph, pw, ph), Color.White);
        }

        /// <summary>
        /// 画面全体を揺らします（シェイク）。
        /// </summary>
        /// <param name="seconds">揺らす時間（秒）</param>
        /// <param name="ratioX">横方向の揺れ強度(0.0-1.0)</param>
        /// <param name="ratioY">縦方向の揺れ強度(0.0-1.0)</param>
        /// <param name="frequency">振動数（回/秒）。デフォルトは20回。</param>
        public void ShakeScreen(float seconds, float ratioX, float ratioY, float frequency = 20.0f)
        {
            _shakeTime = seconds;
            _shakeRatioX = ratioX;
            _shakeRatioY = ratioY;
            _shakeFrequency = frequency;
            _shakeTimer = 0; // 即時更新されるようにリセット
        }

        /// <summary>
        /// 画面フィルターを設定します。
        /// 既存のフィルターはクリアされ、新しいフィルターが設定されます。
        /// </summary>
        /// <param name="targetName">ターゲット名（nullならメイン画面）</param>
        /// <param name="param">フィルターパラメータ</param>
        public void SetScreenFilter(TonFilterParam param)
        {
            SetScreenFilter(null, param);
        }
        public void SetScreenFilter(string targetName, TonFilterParam param)
        {
            ClearScreenFilter(targetName);
            AddScreenFilter(targetName, param);
        }

        /// <summary>
        /// 画面フィルターを追加します。
        /// 既存のフィルターの後に適用されます。
        /// </summary>
        /// <param name="targetName">ターゲット名（nullならメイン画面）</param>
        /// <param name="param">フィルターパラメータ</param>
        public void AddScreenFilter(TonFilterParam param)
        {
            AddScreenFilter(null, param);
        }
        public void AddScreenFilter(string targetName, TonFilterParam param)
        {
            if (targetName == null)
            {
                _currentFilters.Add(param);
            }
            else
            {
                if (!_targetFilters.ContainsKey(targetName))
                {
                    _targetFilters[targetName] = new List<TonFilterParam>();
                }
                _targetFilters[targetName].Add(param);
            }
        }

        /// <summary>
        /// 画面フィルターをクリアします。
        /// </summary>
        /// <param name="targetName">ターゲット名（nullならメイン画面）</param>
        public void ClearScreenFilter(string targetName = null)
        {
            if (targetName == null)
            {
                _currentFilters.Clear();
            }
            else
            {
                if (_targetFilters.ContainsKey(targetName))
                {
                    _targetFilters[targetName].Clear();
                }
            }
        }


        /// <summary>
        /// デバッグ用：指定したグループの最終使用時刻を1時間前に設定し、強制的にキャッシュ切れ扱いにします。
        /// </summary>
        public void DebugForceExpireCache(string contentId)
        {
            if (Ton.Game == null) return;
            
            if (contentId == "Default")
            {
                Ton.Log.Warning("[Graphics] Debug: Cannot expire 'Default' group.");
                return;
            }

            if (_contentGroups.ContainsKey(contentId))
            {
                double now = Ton.Game.GetTotalGameTime().TotalSeconds;
                _contentGroups[contentId].LastUsed = now - 3600.0;
                Ton.Log.Info($"[Graphics] Debug: Forced cache expiration for group '{contentId}'.");
            }
            else
            {
                Ton.Log.Warning($"[Graphics] Debug: ContentGroup '{contentId}' not found.");
            }
        }
    }
}
