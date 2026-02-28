using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Mononotonka
{
    /// <summary>
    /// サウンド管理クラスです。
    /// BGM（ストリーミング再生）とSE（効果音）の再生を管理します。
    /// </summary>
    public class TonSound
    {
        private IServiceProvider _serviceProvider;
        private string _rootDirectory;
        
        // リソース管理クラス
        private class ResourceInfo<T>
        {
            public T Data;
            public string ContentId; // 所属するContentGroup ID
            public long Size; // バイト数（推定）
            public List<SoundEffectInstance> Instances = new List<SoundEffectInstance>(); // SE用インスタンス管理
            public float BaseVolume = 1.0f;
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
        private long _totalSoundMemory = 0;
        private long _maxSoundMemory = 0;

        /// <summary>
        /// 現在のサウンドメモリ使用量（バイト）を取得します。
        /// </summary>
        public long TotalSoundMemory => _totalSoundMemory;

        /// <summary>
        /// 最大サウンドメモリ使用量（バイト）を取得します。
        /// </summary>
        public long MaxSoundMemory => _maxSoundMemory;

        private long EstimateSeSize(SoundEffect se)
        {
            if (se == null) return 0;
            // 推定: 44.1kHz, 16bit, Stereo = 176400 bytes/sec
            return (long)(se.Duration.TotalSeconds * 44100 * 2 * 2);
        }

        // リソース管理
        private Dictionary<string, ResourceInfo<SoundEffect>> _seResources = new Dictionary<string, ResourceInfo<SoundEffect>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, ResourceInfo<Song>> _bgmResources = new Dictionary<string, ResourceInfo<Song>>(StringComparer.OrdinalIgnoreCase);
        
        // フォールバック用SE（遅延生成、共有）
        private SoundEffect _fallbackSe;

        
        // コンテンツグループ管理
        private Dictionary<string, ContentGroup> _contentGroups = new Dictionary<string, ContentGroup>(StringComparer.OrdinalIgnoreCase);
        
        // BGM 状態
        private float _masterVolume = 1.0f;
        private float _bgmVolumeScale = 1.0f;
        private float _seVolumeScale = 1.0f;
        private string _currentBgmName = null;
        private bool _isBgmPlaying = false;
        private bool _isSeMutedByUser = false;
        private bool _isBgmMutedByUser = false;
        private bool _isMutedByInactiveWindow = false;
        private bool _muteWhenInactive = false;
        
        // Resume用
        private string _pausedBgmName = null;
        private TimeSpan _pausedBgmPosition = TimeSpan.Zero;

        // フェード処理用
        private float _bgmVolume = 1.0f;
        private float _targetBgmVolume = 1.0f;
        private float _fadeSpeed = 0f; // 1秒あたりの変化量
        private bool _isFading = false;
        private bool _stopAfterFade = false;

        private double _cacheTimeout = 300.0; // 5分（未使用リソースの破棄基準）
        private double _lastCleanupTime = 0;
        private double _lastBgmUpdateTime = 0;

        /// <summary>
        /// 初期化処理です。
        /// </summary>
        /// <param name="serviceProvider">ServiceProvider</param>
        /// <param name="rootDirectory">RootDirectory</param>
        public void Initialize(IServiceProvider serviceProvider, string rootDirectory = "Content")
        {
            _serviceProvider = serviceProvider;
            _rootDirectory = rootDirectory;
            
            // "Default" グループの作成（ContentManagerは共有ではなく新規作成とするか、引数で渡されたものを使うかだが、ここでは新規作成）
            // Initialize時にContentManagerが渡されなくなったため、内部で作成する
            GetOrCreateContentGroup("Default");
        }
        
        // ヘルパー: グループ取得・作成
        private ContentGroup GetOrCreateContentGroup(string contentId)
        {
            if (!_contentGroups.ContainsKey(contentId))
            {
                var group = new ContentGroup(_serviceProvider, _rootDirectory);
                group.LastUsed = 0; // Usage updated on play/load
                _contentGroups[contentId] = group;
                Ton.Log.Info($"[Sound] Created ContentGroup: {contentId}");
            }
            return _contentGroups[contentId];
        }

        /// <summary>
        /// フォールバック用のSE（ブザー音）を取得・生成します。
        /// </summary>
        private SoundEffect GetFallbackSE()
        {
            if (_fallbackSe != null) return _fallbackSe;

            // 矩形波の生成 (44.1kHz, Mono)
            int sampleRate = 44100;
            double duration = 0.5; // 秒
            int samples = (int)(sampleRate * duration);
            byte[] buffer = new byte[samples * 2]; // 16bit

            double frequency = 220.0; // Hz (Low pitch)
            
            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / sampleRate;
                // 矩形波: 周期の前半は+1、後半は-1
                short amplitude = (short)(Math.Sign(Math.Sin(2.0 * Math.PI * frequency * t)) * 10000);
                
                buffer[i * 2] = (byte)(amplitude & 0xFF);
                buffer[i * 2 + 1] = (byte)((amplitude >> 8) & 0xFF);
            }

            _fallbackSe = new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
            return _fallbackSe;
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
        /// 現在再生対象BGMのリソース取得を試みます。
        /// </summary>
        private bool TryGetCurrentBgmResource(out ResourceInfo<Song> resource)
        {
            resource = null;
            if (string.IsNullOrEmpty(_currentBgmName))
            {
                return false;
            }

            return _bgmResources.TryGetValue(_currentBgmName, out resource);
        }

        /// <summary>
        /// フェード関連フラグをリセットします。
        /// </summary>
        private void ResetFadeState()
        {
            _isFading = false;
            _stopAfterFade = false;
            _fadeSpeed = 0f;
        }

        /// <summary>
        /// 現在のSEミュート有効状態を取得します。
        /// </summary>
        /// <returns>ミュート有効時はtrue</returns>
        private bool IsEffectiveSeMuted()
        {
            return _isSeMutedByUser || _isMutedByInactiveWindow;
        }

        /// <summary>
        /// 現在のBGMミュート有効状態を取得します。
        /// </summary>
        /// <returns>ミュート有効時はtrue</returns>
        private bool IsEffectiveBgmMuted()
        {
            return _isBgmMutedByUser || _isMutedByInactiveWindow;
        }

        /// <summary>
        /// 再生中BGMへ現在のミュート状態を反映します。
        /// </summary>
        private void ApplyBgmMuteState()
        {
            if (!_isBgmPlaying)
            {
                return;
            }

            ApplyCurrentBgmVolume();
        }

        /// <summary>
        /// すべてのSEインスタンスを停止して破棄します。
        /// </summary>
        private void StopAllSeInstancesForMute()
        {
            foreach (var resource in _seResources.Values)
            {
                if (resource.Instances == null)
                {
                    continue;
                }

                for (int i = resource.Instances.Count - 1; i >= 0; i--)
                {
                    var instance = resource.Instances[i];
                    if (instance == null)
                    {
                        resource.Instances.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        if (!instance.IsDisposed && instance.State != SoundState.Stopped)
                        {
                            instance.Stop();
                        }
                    }
                    catch
                    {
                        // 破棄処理優先のため、停止失敗は握りつぶして続行します。
                    }

                    if (!instance.IsDisposed)
                    {
                        instance.Dispose();
                    }
                    resource.Instances.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// BGM停止完了時の状態更新を一箇所で行います。
        /// </summary>
        /// <param name="stopPlayer">MediaPlayer.Stopを実行するか</param>
        private void CompleteBgmStop(bool stopPlayer = true)
        {
            if (stopPlayer)
            {
                try
                {
                    MediaPlayer.Stop();
                }
                catch (Exception ex)
                {
                    Ton.Log.Warning($"StopBGM media stop failed: {ex.Message}");
                }
            }

            _isBgmPlaying = false;
            _currentBgmName = null;
            _bgmVolume = 0f;
            _targetBgmVolume = 0f;
            ResetFadeState();
        }

        /// <summary>
        /// 現在再生中BGMに対して音量を反映します。
        /// </summary>
        /// <returns>反映できた場合true</returns>
        private bool ApplyCurrentBgmVolume()
        {
            if (!TryGetCurrentBgmResource(out var resource))
            {
                return false;
            }

            float effectiveVolume = IsEffectiveBgmMuted()
                ? 0.0f
                : _bgmVolume * _masterVolume * _bgmVolumeScale * resource.BaseVolume;
            MediaPlayer.Volume = MathHelper.Clamp(effectiveVolume, 0.0f, 1.0f);
            return true;
        }

        /// <summary>
        /// 未使用リソースの破棄時間を設定します。
        /// デフォルトは600秒(10分)です。
        /// </summary>
        /// <param name="seconds">秒数</param>
        public void SetCacheTimeout(double seconds)
        {
            _cacheTimeout = seconds;
        }

        /// <summary>
        /// 効果音(SE)の手動ミュート状態を設定します。
        /// </summary>
        /// <param name="muted">trueでミュート、falseで解除</param>
        public void SetSEMuted(bool muted)
        {
            if (_isSeMutedByUser == muted)
            {
                return;
            }

            bool wasMuted = IsEffectiveSeMuted();
            _isSeMutedByUser = muted;
            bool isMuted = IsEffectiveSeMuted();

            if (!wasMuted && isMuted)
            {
                StopAllSeInstancesForMute();
            }
        }

        /// <summary>
        /// 効果音(SE)の手動ミュート状態を取得します。
        /// </summary>
        /// <returns>手動ミュート中ならtrue</returns>
        public bool IsSEMuted()
        {
            return _isSeMutedByUser;
        }

        /// <summary>
        /// BGMの手動ミュート状態を設定します。
        /// </summary>
        /// <param name="muted">trueでミュート、falseで解除</param>
        public void SetBGMMuted(bool muted)
        {
            if (_isBgmMutedByUser == muted)
            {
                return;
            }

            bool wasMuted = IsEffectiveBgmMuted();
            _isBgmMutedByUser = muted;
            bool isMuted = IsEffectiveBgmMuted();

            if (wasMuted != isMuted)
            {
                ApplyBgmMuteState();
            }
        }

        /// <summary>
        /// BGMの手動ミュート状態を取得します。
        /// </summary>
        /// <returns>手動ミュート中ならtrue</returns>
        public bool IsBGMMuted()
        {
            return _isBgmMutedByUser;
        }

        /// <summary>
        /// ウィンドウ非アクティブ時に自動ミュートするかを設定します。
        /// </summary>
        /// <param name="enabled">trueで有効、falseで無効</param>
        public void SetMuteWhenInactive(bool enabled)
        {
            bool wasBgmMuted = IsEffectiveBgmMuted();
            bool wasSeMuted = IsEffectiveSeMuted();

            _muteWhenInactive = enabled;
            if (!_muteWhenInactive)
            {
                _isMutedByInactiveWindow = false;
            }
            else
            {
                var state = Ton.Game != null ? Ton.Game.GetWindowActivityState() : TonWindowActivityState.Active;
                _isMutedByInactiveWindow = (state == TonWindowActivityState.Inactive || state == TonWindowActivityState.JustDeactivated);
            }

            bool isBgmMuted = IsEffectiveBgmMuted();
            bool isSeMuted = IsEffectiveSeMuted();

            if (!wasSeMuted && isSeMuted)
            {
                StopAllSeInstancesForMute();
            }

            if (wasBgmMuted != isBgmMuted)
            {
                ApplyBgmMuteState();
            }
        }

        /// <summary>
        /// ウィンドウ非アクティブ時自動ミュート設定を取得します。
        /// </summary>
        /// <returns>有効ならtrue</returns>
        public bool GetMuteWhenInactive()
        {
            return _muteWhenInactive;
        }

        /// <summary>
        /// ウィンドウ状態を反映して自動ミュートを更新します。
        /// </summary>
        /// <param name="state">ウィンドウ状態</param>
        public void ApplyWindowActivityState(TonWindowActivityState state)
        {
            bool wasBgmMuted = IsEffectiveBgmMuted();
            bool wasSeMuted = IsEffectiveSeMuted();

            if (_muteWhenInactive)
            {
                _isMutedByInactiveWindow = (state == TonWindowActivityState.Inactive || state == TonWindowActivityState.JustDeactivated);
            }
            else
            {
                _isMutedByInactiveWindow = false;
            }

            bool isBgmMuted = IsEffectiveBgmMuted();
            bool isSeMuted = IsEffectiveSeMuted();

            if (!wasSeMuted && isSeMuted)
            {
                StopAllSeInstancesForMute();
            }

            if (wasBgmMuted != isBgmMuted)
            {
                ApplyBgmMuteState();
            }
        }

        /// <summary>
        /// 更新処理。BGMのフェード処理やリソースのクリーンアップを行います。
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            double totalSeconds = gameTime.TotalGameTime.TotalSeconds;

            // BGM再生中は定期的にリソース使用時間を更新（1秒ごと）
            if (_isBgmPlaying)
            {
                if (TryGetCurrentBgmResource(out var playingResource))
                {
                    if (totalSeconds - _lastBgmUpdateTime >= 1.0)
                    {
                        _lastBgmUpdateTime = totalSeconds;
                        UpdateLastUsed(playingResource.ContentId, totalSeconds);
                    }
                }
                else
                {
                    // 参照先が消失している場合は安全側で停止状態へ遷移する
                    CompleteBgmStop();
                }
            }

            // フェード処理
            if (_isFading)
            {
                if (!TryGetCurrentBgmResource(out _))
                {
                    // アンロード直後などで参照先が無い場合
                    CompleteBgmStop();
                }
                else
                {
                    if (_bgmVolume < _targetBgmVolume)
                    {
                        _bgmVolume += _fadeSpeed * elapsed;
                        if (_bgmVolume >= _targetBgmVolume)
                        {
                            _bgmVolume = _targetBgmVolume;
                            ResetFadeState();
                        }
                    }
                    else if (_bgmVolume > _targetBgmVolume)
                    {
                        _bgmVolume -= _fadeSpeed * elapsed;
                        if (_bgmVolume <= _targetBgmVolume)
                        {
                            _bgmVolume = _targetBgmVolume;
                            bool stopAfterFade = _stopAfterFade;
                            ResetFadeState();
                            if (stopAfterFade)
                            {
                                CompleteBgmStop();
                            }
                        }
                    }
                    else
                    {
                        bool stopAfterFade = _stopAfterFade;
                        ResetFadeState();
                        if (stopAfterFade)
                        {
                            CompleteBgmStop();
                        }
                    }

                    if (_isBgmPlaying)
                    {
                        ApplyCurrentBgmVolume();
                    }
                }
            }

            // リソース破棄チェック (10秒ごとに実行)
            if (totalSeconds - _lastCleanupTime >= 10.0)
            {
                _lastCleanupTime = totalSeconds;

                // SEインスタンスの定期クリーンアップ（停止したものをリストから外す）
                foreach (var kvp in _seResources)
                {
                    if (kvp.Value.Instances != null)
                    {
                        // 停止済みインスタンスを明示的に破棄してから管理リストから除去する
                        for (int i = kvp.Value.Instances.Count - 1; i >= 0; i--)
                        {
                            var instance = kvp.Value.Instances[i];
                            if (instance == null || instance.IsDisposed || instance.State == SoundState.Stopped)
                            {
                                if (instance != null && !instance.IsDisposed)
                                {
                                    instance.Dispose();
                                }
                                kvp.Value.Instances.RemoveAt(i);
                            }
                        }
                    }
                }

                // グループごとのクリーンアップ
                List<string> removeGroups = new List<string>();
                foreach (var kvp in _contentGroups)
                {
                    if (kvp.Key == "Default") continue; // Defaultは除外
                    
                    if (totalSeconds - kvp.Value.LastUsed > _cacheTimeout)
                    {
                        removeGroups.Add(kvp.Key);
                    }
                }
                
                foreach (string gid in removeGroups)
                {
                    Unload(gid);
                    Ton.Log.Info($"[MEM] Auto-released ContentGroup: {gid}");
                }
            }
        }
        
        /// <summary>
        /// 指定したContentIDのグループをアンロードします。"Default"グループはアンロードできません。
        /// </summary>
        /// <param name="contentId">グループID</param>
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
                
                // グループに属するリソースの解放処理
                // SEのインスタンス停止
                foreach (var resName in group.ResourceNames)
                {
                    if (_seResources.ContainsKey(resName))
                    {
                        var res = _seResources[resName];
                        if (res.Instances != null)
                        {
                            foreach(var i in res.Instances) { i.Stop(); i.Dispose(); }
                        }
                        _totalSoundMemory -= res.Size;
                        _seResources.Remove(resName);
                    }
                    else if (_bgmResources.ContainsKey(resName))
                    {
                        // 再生中なら止める
                        if (_currentBgmName == resName) StopBGM(0);
                        _bgmResources.Remove(resName);
                    }
                }

                // ContentManagerのアンロードと破棄
                group.Manager.Unload();
                group.Manager.Dispose();
                
                _contentGroups.Remove(contentId);
                Ton.Log.Info($"[MEM] Unloaded ContentGroup: {contentId}");
            }
        }

        /// <summary>
        /// すべてのサウンドリソースを停止・解放します。
        /// </summary>
        public void UnloadAll()
        {
            StopAll();

            // 全グループ解放（Default含む）... としたいが、Terminate以外ではDefaultは残すべきか？
            // UnloadAllは「全て」なのでDefaultも含めてリセットし、再作成する挙動とする
            
            List<string> groupKeys = new List<string>(_contentGroups.Keys);
            foreach (var key in groupKeys)
            {
                // UnloadメソッドはDefaultを弾くので、直接処理するか、Unloadメソッドの引数で強制フラグを持たせる等の対応が必要
                // ここでは直接処理を行う
                var group = _contentGroups[key];
                
                // ... SE Instance Stop ...
                foreach (var resName in group.ResourceNames) {
                     if (_seResources.ContainsKey(resName)) {
                         var res = _seResources[resName];
                         if (res.Instances != null) foreach(var i in res.Instances) { i.Stop(); i.Dispose(); }
                     }
                }
                
                group.Manager.Unload();
                group.Manager.Dispose();
            }

            _contentGroups.Clear();
            GetOrCreateContentGroup("Default"); // Defaultだけ再作成

            _seResources.Clear();
            _bgmResources.Clear();
            _totalSoundMemory = 0;
            _maxSoundMemory = 0;
            
            Ton.Log.Info("[MEM] Unloaded All Sounds.");
            
            // フォールバックも破棄
            if (_fallbackSe != null && !_fallbackSe.IsDisposed)
            {
                _fallbackSe.Dispose();
                _fallbackSe = null;
            }
        }

        /// <summary>
        /// 効果音(SE)を読み込みます。
        /// </summary>
        public void LoadSound(string path, string name, string contentId = "Default", float baseVolume = 1.0f)
        {
             baseVolume = MathHelper.Clamp(baseVolume, 0.0f, 1.0f);

             // 既にロード済みならグループ更新等はせずリターン（簡易実装）
             if (_seResources.ContainsKey(name)) return;
             
             var group = GetOrCreateContentGroup(contentId);
             
              try {
                  var se = group.Manager.Load<SoundEffect>(path);
                  long size = EstimateSeSize(se);
                  
                  var info = new ResourceInfo<SoundEffect>
                  {
                      Data = se,
                      ContentId = contentId,
                      Size = size,
                      BaseVolume = baseVolume
                  };
                  _seResources[name] = info;
                  group.ResourceNames.Add(name);
                  
                  _totalSoundMemory += size;
                  if (_totalSoundMemory > _maxSoundMemory) _maxSoundMemory = _totalSoundMemory;
                  Ton.Log.Info($"[MEM] Loaded SE: {path} (Group: {contentId})");
                  
                  // ロード時もUsage更新
                  if (Ton.Game != null) UpdateLastUsed(contentId, Ton.Game.GetTotalGameTime().TotalSeconds);

              } catch (Exception ex) { Ton.Log.Error($"SE Load Failed: {path} {ex.Message}"); }
        }

        /// <summary>
        /// BGMを読み込みます。
        /// </summary>
        public void LoadBGM(string path, string name, string contentId = "Default", float baseVolume = 1.0f)
        {
             baseVolume = MathHelper.Clamp(baseVolume, 0.0f, 1.0f);

             if (_bgmResources.ContainsKey(name)) return;

             var group = GetOrCreateContentGroup(contentId);

             try {
                 var song = group.Manager.Load<Song>(path);
                 var info = new ResourceInfo<Song>
                 {
                     Data = song,
                     ContentId = contentId,
                     Size = 0,
                     BaseVolume = baseVolume
                 };
                 _bgmResources[name] = info;
                 group.ResourceNames.Add(name);

                 Ton.Log.Info($"[MEM] Loaded BGM: {path} (Group: {contentId})");

                 if (Ton.Game != null) UpdateLastUsed(contentId, Ton.Game.GetTotalGameTime().TotalSeconds);

             } catch (Exception ex) { Ton.Log.Error($"BGM Load Failed: {path} {ex.Message}"); }
        }

        /// <summary>
        /// BGMをループ再生します。
        /// <param name="bgmName">登録名</param>
        /// <param name="bResume">trueなら中断箇所からの再生を試みます(保存された名前と一致する場合のみ)</param>
        /// <param name="fadeSeconds">フェードインにかける秒数(0で即時再生)</param>
        /// <param name="volume">音量(0.0-1.0)</param>
        /// </summary>
        public void PlayBGM(string bgmName, float fadeSeconds = 0.0f, float volume = 1.0f)
        {
            volume = MathHelper.Clamp(volume, 0.0f, 1.0f);

            // リソースが無ければフォールバックSEを鳴らす（エラー通知）
            if (!_bgmResources.TryGetValue(bgmName, out var targetResource))
            {
                 Ton.Log.Warning($"PlayBGM: Resource '{bgmName}' not found. Playing fallback.");
                 if (!IsEffectiveSeMuted())
                 {
                     try 
                     {
                         var fbWithVol = GetFallbackSE().CreateInstance();
                         fbWithVol.Volume = MathHelper.Clamp(volume * _masterVolume * _seVolumeScale, 0f, 1f);
                         fbWithVol.Play();
                     }
                     catch (Exception ex)
                     {
                         Ton.Log.Warning($"PlayBGM fallback failed: {ex.Message}");
                     }
                 }
                 return;
            }

            if (_currentBgmName == bgmName && _isBgmPlaying)
            {
                // すでに再生中の場合、音量変更のみ反映
                _targetBgmVolume = volume;
                if (fadeSeconds > 0)
                {
                    _isFading = true;
                    _stopAfterFade = false;
                    _fadeSpeed = Math.Abs(_targetBgmVolume - _bgmVolume) / Math.Max(fadeSeconds, 0.0001f);
                }
                else
                {
                    _bgmVolume = _targetBgmVolume;
                    ResetFadeState();
                    ApplyCurrentBgmVolume();
                }
                return;
            }

            // 新しいBGMの再生（既存のフェード状態はリセット）
            ResetFadeState();

            Song song = targetResource.Data;
            MediaPlayer.IsRepeating = true;
            
            _currentBgmName = bgmName;
            _targetBgmVolume = volume;
            
            // Usage更新
            if (Ton.Game != null)
            {
                UpdateLastUsed(targetResource.ContentId, Ton.Game.GetTotalGameTime().TotalSeconds);
            }

            // Resume判定
            TimeSpan startPos = TimeSpan.Zero;
            bool doResume = false;
            if (_pausedBgmName == bgmName)
            {
                startPos = _pausedBgmPosition;
                doResume = true;
                _pausedBgmName = null; // Resumeしたらリセット
            }
            
            if (fadeSeconds > 0)
            {
                _bgmVolume = 0f;
                _isFading = true;
                _fadeSpeed = _targetBgmVolume / Math.Max(fadeSeconds, 0.0001f);
                
                // Play
                try {
                    if (doResume) MediaPlayer.Play(song, startPos);
                    else MediaPlayer.Play(song);
                } catch {
                     MediaPlayer.Play(song); // Fallback
                }
                
                MediaPlayer.Volume = 0f;
                _isBgmPlaying = true;
            }
            else
            {
                _bgmVolume = _targetBgmVolume;
                ResetFadeState();

                // Play
                try {
                    if (doResume) MediaPlayer.Play(song, startPos);
                    else MediaPlayer.Play(song);
                } catch {
                     MediaPlayer.Play(song); // Fallback
                }

                _isBgmPlaying = true;
                ApplyCurrentBgmVolume();
            }
        }

        /// <summary>
        /// BGMを停止します。
        /// </summary>
        /// <param name="fadeSeconds">フェードアウトにかける秒数(0で即時停止)</param>
        /// <param name="bPause">trueなら現在再生中のBGM情報を一時保存します(Pause扱い)</param>
        public void StopBGM(bool bPause = false)
        {
            StopBGM(0.0f, bPause);
        }
        public void StopBGM(float fadeSeconds)
        {
            StopBGM(fadeSeconds, false);
        }
        public void StopBGM(float fadeSeconds = 0.0f, bool bPause = false)
        {
            if (!_isBgmPlaying)
            {
                ResetFadeState();
                return;
            }

            if (bPause && !string.IsNullOrEmpty(_currentBgmName))
            {
                _pausedBgmName = _currentBgmName;
                try
                {
                    _pausedBgmPosition = MediaPlayer.PlayPosition;
                }
                catch
                {
                    _pausedBgmPosition = TimeSpan.Zero;
                }
            }

            if (fadeSeconds > 0 && _bgmVolume > 0.0f && TryGetCurrentBgmResource(out _))
            {
                _targetBgmVolume = 0f;
                _isFading = true;
                _fadeSpeed = _bgmVolume / Math.Max(fadeSeconds, 0.0001f);
                _stopAfterFade = true;
            }
            else
            {
                CompleteBgmStop();
            }
        }

        /// <summary>
        /// 効果音(SE)を再生します。
        /// </summary>
        /// <param name="seName">登録名</param>
        /// <param name="volume">音量(0.0-1.0)</param>

        public void PlaySE(string seName, float volume = 1.0f)
        {
            if (IsEffectiveSeMuted())
            {
                return;
            }

            if (!_seResources.ContainsKey(seName))
            {
                 // リソース消失（または未ロード）
                 Ton.Log.Warning($"PlaySE: Resource '{seName}' not found. Playing fallback.");
                 try
                 {
                     var fbWithVol = GetFallbackSE().CreateInstance();
                     fbWithVol.Volume = MathHelper.Clamp(volume * _masterVolume * _seVolumeScale, 0f, 1f);
                     fbWithVol.Play();
                 }
                 catch (Exception ex)
                 {
                     Ton.Log.Warning($"PlaySE fallback failed: {ex.Message}");
                 }
                 return;
            }else
            {
                var res = _seResources[seName];
                var instance = res.Data.CreateInstance();
                instance.Volume = MathHelper.Clamp(volume * res.BaseVolume * _masterVolume * _seVolumeScale, 0.0f, 1.0f);
                instance.Play();

                // 管理リストに追加
                if (res.Instances == null) res.Instances = new List<SoundEffectInstance>();
                res.Instances.Add(instance);

                UpdateLastUsed(res.ContentId, Ton.Game.GetTotalGameTime().TotalSeconds);
            }
        }

        /// <summary>
        /// すべての音を停止します（主にBGM）。
        /// </summary>
        public void StopAll()
        {
            StopBGM(0);
            // Effectの停止はインスタンス管理が必要なため、ここではBGMのみ停止します。
        }

        /// <summary>
        /// マスター音量を設定します。
        /// </summary>
        /// <param name="volume">音量(0.0-1.0)</param>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = MathHelper.Clamp(volume, 0.0f, 1.0f);
            ApplyBgmMuteState();
        }

        /// <summary>
        /// BGM全体の音量係数を設定します。
        /// </summary>
        /// <param name="volume">音量係数(0.0-1.0)</param>
        public void SetBGMVolume(float volume)
        {
            _bgmVolumeScale = MathHelper.Clamp(volume, 0.0f, 1.0f);
            ApplyBgmMuteState();
        }

        /// <summary>
        /// BGM全体の音量係数を取得します。
        /// </summary>
        /// <returns>BGM音量係数</returns>
        public float GetBGMVolume()
        {
            return _bgmVolumeScale;
        }

        /// <summary>
        /// SE全体の音量係数を設定します。
        /// </summary>
        /// <param name="volume">音量係数(0.0-1.0)</param>
        public void SetSEVolume(float volume)
        {
            _seVolumeScale = MathHelper.Clamp(volume, 0.0f, 1.0f);
        }

        /// <summary>
        /// SE全体の音量係数を取得します。
        /// </summary>
        /// <returns>SE音量係数</returns>
        public float GetSEVolume()
        {
            return _seVolumeScale;
        }

        /// <summary>
        /// マスター音量を取得します。
        /// </summary>
        public float GetMasterVolume()
        {
            return _masterVolume;
        }

        /// <summary>
        /// 現在BGMを再生中かどうかを取得します。
        /// </summary>
        /// <returns>再生中ならtrue、それ以外はfalse</returns>
        public bool IsBGMPlaying()
        {
            return _isBgmPlaying;
        }

        /// <summary>
        /// 現在再生中のBGMの再生位置を取得します。
        /// </summary>
        /// <returns>再生位置(TimeSpan)。再生していない場合はZero。</returns>
        public TimeSpan GetBGMPosition()
        {
            if (!_isBgmPlaying) return TimeSpan.Zero;
            try
            {
                return MediaPlayer.PlayPosition;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// 現在再生中のBGMの長さを取得します。
        /// </summary>
        /// <returns>長さ(TimeSpan)。再生していない場合はZero。</returns>
        public TimeSpan GetBGMLength()
        {
            if (!_isBgmPlaying || !TryGetCurrentBgmResource(out var resource))
            {
                return TimeSpan.Zero;
            }
            return resource.Data.Duration;
        }

        /// <summary>
        /// 終了処理。
        /// </summary>
        public void Terminate()
        {
            StopAll();
            if (_fallbackSe != null && !_fallbackSe.IsDisposed)
            {
                _fallbackSe.Dispose();
                _fallbackSe = null;
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
                Ton.Log.Warning("[Sound] Debug: Cannot expire 'Default' group.");
                return;
            }

            if (_contentGroups.ContainsKey(contentId))
            {
                double now = Ton.Game.GetTotalGameTime().TotalSeconds;
                _contentGroups[contentId].LastUsed = now - 3600.0;
                Ton.Log.Info($"[Sound] Debug: Forced cache expiration for group '{contentId}'.");
            }
            else
            {
                Ton.Log.Warning($"[Sound] Debug: ContentGroup '{contentId}' not found.");
            }
        }
    }
}
