using Unity.Collections;
using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 프로토타입용 자체 음성 전송 구현체.
    /// Microphone PCM을 음성 코덱으로 압축한 뒤 NGO named message로 전달한다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MicrophoneVoiceTransport : NetworkBehaviour, IVoiceTransport
    {
        private const string VOICE_FRAME_MESSAGE = "Caretaker.Radio.VoiceFrame";
        private const int DEFAULT_SAMPLE_RATE = 48000;
        private const int DEFAULT_FRAME_SAMPLES = 960;
        private const int DEFAULT_CLIP_SECONDS = 1;
        private const int PLAYBACK_BUFFER_SECONDS = 4;
        private const int DEFAULT_MIN_BUFFERED_FRAMES = 4;
        private const int MAX_OPUS_PACKET_BYTES = 1275;

        [SerializeField] private RadioNetworkBridge _radioNetworkBridge;
        [SerializeField] private VoiceCodecMode _codecMode = VoiceCodecMode.Opus;
        [SerializeField] private bool _preferHighQualityOpusMode = true;
        [SerializeField] private int _sampleRate = DEFAULT_SAMPLE_RATE;
        [SerializeField] private int _frameSamples = DEFAULT_FRAME_SAMPLES;
        [SerializeField, Range(0.1f, 2f)] private float _captureGain = 0.7f;
        [SerializeField, Range(0.1f, 2f)] private float _playbackGain = 0.5f;
        [SerializeField, Min(0)] private int _minimumBufferedFrames = DEFAULT_MIN_BUFFERED_FRAMES;
        [SerializeField] private bool _autoStartSilentPlayback = true;
        [SerializeField] private bool _showDebugOverlay = false;
        [SerializeField] private Vector2 _debugPanelPosition = new(16f, 220f);

        private AudioClip _microphoneClip;
        private AudioSource _audioSource;
        private byte[] _encodedFrame;
        private float[] _captureFrame;
        private float[] _decodeFrame;
        private float[] _playbackBuffer;
        private ConcentusOpusVoiceCodec _opusCodec;
        private int _microphoneReadPosition;
        private int _playbackReadPosition;
        private int _playbackWritePosition;
        private int _queuedPlaybackSamples;
        private int _outputSampleRate;
        private float _playbackResampleAccumulator;
        private bool _isTransmitting;
        private bool _isMessageHandlerRegistered;
        private string _microphoneDeviceName = "None";
        private int _lastMicrophonePosition;
        private int _capturedFrames;
        private int _sentFrames;
        private int _receivedFrames;
        private int _relayedFrames;
        private int _droppedFrames;
        private int _underruns;
        private bool _isPlaybackPrimed;
        private readonly object _playbackLock = new();

        /// <summary>
        /// 현재 로컬 마이크 송신 중인지 반환한다.
        /// </summary>
        public bool IsTransmitting => _isTransmitting;

        /// <summary>
        /// 재생 대기 중인 샘플 수를 반환한다.
        /// </summary>
        public int QueuedPlaybackSamples => _queuedPlaybackSamples;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            ApplyRuntimeAudioSettings();
            _captureFrame = new float[_frameSamples];
            _decodeFrame = new float[_frameSamples];
            _encodedFrame = new byte[Mathf.Max(_frameSamples, MAX_OPUS_PACKET_BYTES)];
            _playbackBuffer = new float[_sampleRate * PLAYBACK_BUFFER_SECONDS];
            _outputSampleRate = AudioSettings.outputSampleRate;
            _opusCodec = new ConcentusOpusVoiceCodec(_sampleRate, _frameSamples);

            if (_radioNetworkBridge == null)
            {
                _radioNetworkBridge = GetComponent<RadioNetworkBridge>();
            }

            ConfigureAudioSource();
        }

        private void OnValidate()
        {
            if (_sampleRate <= 0)
            {
                _sampleRate = DEFAULT_SAMPLE_RATE;
            }

            if (_frameSamples <= 0)
            {
                _frameSamples = Mathf.Max(1, _sampleRate / 50);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager != null && NetworkManager.CustomMessagingManager != null)
            {
                NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                    VOICE_FRAME_MESSAGE,
                    HandleVoiceFrameMessage);
                _isMessageHandlerRegistered = true;
            }
        }

        public override void OnNetworkDespawn()
        {
            StopTransmit();

            if (NetworkManager != null && NetworkManager.CustomMessagingManager != null)
            {
                NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(VOICE_FRAME_MESSAGE);
                _isMessageHandlerRegistered = false;
            }
        }

        private void Update()
        {
            if (!_isTransmitting || _microphoneClip == null)
            {
                return;
            }

            PumpMicrophoneFrames();
        }

        /// <inheritdoc />
        public void StartTransmit()
        {
            if (_isTransmitting)
            {
                return;
            }

            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[Radio] No microphone device is available.", this);
                return;
            }

            _microphoneDeviceName = Microphone.devices[0];
            _microphoneClip = Microphone.Start(_microphoneDeviceName, true, DEFAULT_CLIP_SECONDS, _sampleRate);
            _microphoneReadPosition = 0;
            _lastMicrophonePosition = 0;
            _isTransmitting = true;
            Debug.Log($"[Radio] Microphone transmit started. Device={_microphoneDeviceName}", this);
        }

        /// <inheritdoc />
        public void StopTransmit()
        {
            if (!_isTransmitting)
            {
                return;
            }

            _isTransmitting = false;

            if (Microphone.IsRecording(_microphoneDeviceName))
            {
                Microphone.End(_microphoneDeviceName);
            }

            _microphoneClip = null;
            _microphoneReadPosition = 0;
            _lastMicrophonePosition = 0;
            Debug.Log("[Radio] Microphone transmit stopped.", this);
        }

        private void PumpMicrophoneFrames()
        {
            int writePosition = Microphone.GetPosition(_microphoneDeviceName);
            _lastMicrophonePosition = writePosition;
            if (writePosition < 0)
            {
                return;
            }

            int clipSamples = _microphoneClip.samples;
            int availableSamples = GetAvailableSamples(writePosition, clipSamples);

            while (availableSamples >= _frameSamples)
            {
                if (_microphoneReadPosition + _frameSamples > clipSamples)
                {
                    _microphoneReadPosition = 0;
                    availableSamples = GetAvailableSamples(writePosition, clipSamples);
                    if (availableSamples < _frameSamples)
                    {
                        break;
                    }
                }

                _microphoneClip.GetData(_captureFrame, _microphoneReadPosition);
                ApplyCaptureGain(_captureFrame, _frameSamples);
                int encodedLength = EncodeFrame(_captureFrame, _frameSamples, _encodedFrame);
                SendVoiceFrame(_encodedFrame, _frameSamples, encodedLength);
                _capturedFrames++;

                _microphoneReadPosition = (_microphoneReadPosition + _frameSamples) % clipSamples;
                availableSamples -= _frameSamples;
            }
        }

        private int GetAvailableSamples(int writePosition, int clipSamples)
        {
            if (writePosition >= _microphoneReadPosition)
            {
                return writePosition - _microphoneReadPosition;
            }

            return clipSamples - _microphoneReadPosition + writePosition;
        }

        private void SendVoiceFrame(byte[] encodedFrame, int sampleCount, int encodedLength)
        {
            if (!CanUseNetwork() || _radioNetworkBridge == null)
            {
                return;
            }

            ulong localClientId = NetworkManager.LocalClientId;
            if (_radioNetworkBridge.CurrentTalkerId != localClientId)
            {
                _droppedFrames++;
                return;
            }

            if (IsServer)
            {
                RelayVoiceFrame(localClientId, encodedFrame, sampleCount, encodedLength);
                return;
            }

            SendVoiceFrameToServer(localClientId, encodedFrame, sampleCount, encodedLength);
        }

        private void SendVoiceFrameToServer(ulong originalTalkerId, byte[] encodedFrame, int sampleCount, int encodedLength)
        {
            using FastBufferWriter writer = BuildVoiceFrameWriter(originalTalkerId, encodedFrame, sampleCount, encodedLength);
            NetworkManager.CustomMessagingManager.SendNamedMessage(
                VOICE_FRAME_MESSAGE,
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.UnreliableSequenced);
            _sentFrames++;
        }

        private void RelayVoiceFrame(ulong originalTalkerId, byte[] encodedFrame, int sampleCount, int encodedLength)
        {
            if (!CanUseNetwork())
            {
                return;
            }

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId == originalTalkerId || clientId == NetworkManager.LocalClientId)
                {
                    continue;
                }

                using FastBufferWriter writer = BuildVoiceFrameWriter(originalTalkerId, encodedFrame, sampleCount, encodedLength);
                NetworkManager.CustomMessagingManager.SendNamedMessage(
                    VOICE_FRAME_MESSAGE,
                    clientId,
                    writer,
                    NetworkDelivery.UnreliableSequenced);
                _relayedFrames++;
            }
        }

        private FastBufferWriter BuildVoiceFrameWriter(ulong originalTalkerId, byte[] encodedFrame, int sampleCount, int encodedLength)
        {
            int payloadSize = sizeof(ulong) + sizeof(int) * 4 + encodedLength;
            FastBufferWriter writer = new(payloadSize, Allocator.Temp);
            writer.WriteValueSafe(originalTalkerId);
            writer.WriteValueSafe(_sampleRate);
            writer.WriteValueSafe((int)_codecMode);
            writer.WriteValueSafe(sampleCount);
            writer.WriteValueSafe(encodedLength);
            writer.WriteBytesSafe(encodedFrame, encodedLength);
            return writer;
        }

        private void HandleVoiceFrameMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong originalTalkerId);
            reader.ReadValueSafe(out int sampleRate);
            reader.ReadValueSafe(out int codecModeValue);
            reader.ReadValueSafe(out int sampleCount);
            reader.ReadValueSafe(out int encodedLength);

            if (encodedLength > _encodedFrame.Length || sampleCount > _decodeFrame.Length)
            {
                _droppedFrames++;
                Debug.LogWarning("[Radio] Dropped oversized voice frame.", this);
                return;
            }

            reader.ReadBytesSafe(ref _encodedFrame, encodedLength);

            if (IsServer && senderClientId != NetworkManager.ServerClientId)
            {
                if (_radioNetworkBridge == null || _radioNetworkBridge.CurrentTalkerId != senderClientId)
                {
                    _droppedFrames++;
                    return;
                }

                if (sampleRate != _sampleRate)
                {
                    _droppedFrames++;
                    Debug.LogWarning("[Radio] Dropped voice frame with mismatched sample rate.", this);
                    return;
                }

                RelayVoiceFrame(senderClientId, _encodedFrame, sampleCount, encodedLength);
                if (senderClientId != NetworkManager.LocalClientId)
                {
                    int decodedSamples = DecodeFrame((VoiceCodecMode)codecModeValue, _encodedFrame, encodedLength, _decodeFrame);
                    EnqueuePlayback(_decodeFrame, decodedSamples);
                    _receivedFrames++;
                }

                return;
            }

            if (CanUseNetwork() && originalTalkerId == NetworkManager.LocalClientId)
            {
                return;
            }

            if (sampleRate != _sampleRate)
            {
                _droppedFrames++;
                Debug.LogWarning("[Radio] Dropped voice frame with mismatched sample rate.", this);
                return;
            }

            int decodedSampleCount = DecodeFrame((VoiceCodecMode)codecModeValue, _encodedFrame, encodedLength, _decodeFrame);
            EnqueuePlayback(_decodeFrame, decodedSampleCount);
            _receivedFrames++;
        }

        private void EnqueuePlayback(float[] samples, int sampleCount)
        {
            lock (_playbackLock)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    if (_queuedPlaybackSamples == _playbackBuffer.Length)
                    {
                        _playbackReadPosition = (_playbackReadPosition + 1) % _playbackBuffer.Length;
                        _queuedPlaybackSamples--;
                    }

                    _playbackBuffer[_playbackWritePosition] = samples[i];
                    _playbackWritePosition = (_playbackWritePosition + 1) % _playbackBuffer.Length;
                    _queuedPlaybackSamples++;
                }

                if (!_isPlaybackPrimed && _queuedPlaybackSamples >= GetMinimumBufferedSamples())
                {
                    _isPlaybackPrimed = true;
                }
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            lock (_playbackLock)
            {
                for (int i = 0; i < data.Length; i += channels)
                {
                    float sample = 0f;
                    if (_isPlaybackPrimed && _queuedPlaybackSamples > 0)
                    {
                        sample = SoftLimit(ReadPlaybackSample() * _playbackGain);
                    }
                    else if (_isPlaybackPrimed)
                    {
                        _isPlaybackPrimed = false;
                        _playbackResampleAccumulator = 0f;
                        _underruns++;
                    }

                    for (int channel = 0; channel < channels; channel++)
                    {
                        data[i + channel] = sample;
                    }
                }
            }
        }

        private void ConfigureAudioSource()
        {
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0f;

            if (!_autoStartSilentPlayback)
            {
                return;
            }

            AudioClip silentClip = AudioClip.Create("RadioPlaybackCarrier", _sampleRate, 1, _sampleRate, false);
            _audioSource.clip = silentClip;
            _audioSource.Play();
        }

        private void ApplyRuntimeAudioSettings()
        {
            if (_codecMode != VoiceCodecMode.Opus || !_preferHighQualityOpusMode)
            {
                return;
            }

            _sampleRate = DEFAULT_SAMPLE_RATE;
            _frameSamples = DEFAULT_FRAME_SAMPLES;
        }

        private bool CanUseNetwork()
        {
            return NetworkManager != null
                && NetworkManager.IsListening
                && NetworkManager.CustomMessagingManager != null;
        }

        private void OnGUI()
        {
            if (!_showDebugOverlay)
            {
                return;
            }

            Rect rect = new(_debugPanelPosition.x, _debugPanelPosition.y, 360f, 210f);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Radio Voice Debug");
            GUILayout.Label($"Spawned: {IsSpawned}, Handler: {_isMessageHandlerRegistered}");
            GUILayout.Label($"Mode: {GetNetworkModeText()}, LocalId: {GetLocalClientIdText()}");
            GUILayout.Label($"Talker: {GetTalkerText()}");
            GUILayout.Label($"Mic: {_microphoneDeviceName}, Pos: {_lastMicrophonePosition}");
            GUILayout.Label($"AudioSource Playing: {(_audioSource != null && _audioSource.isPlaying)}");
            GUILayout.Label($"Tx: {_isTransmitting}, Captured: {_capturedFrames}, Sent: {_sentFrames}");
            GUILayout.Label($"Relayed: {_relayedFrames}, Received: {_receivedFrames}, Dropped: {_droppedFrames}");
            GUILayout.Label($"Queued Samples: {_queuedPlaybackSamples}, Primed: {_isPlaybackPrimed}");
            GUILayout.Label($"Underruns: {_underruns}, Gain: {_captureGain:0.00}/{_playbackGain:0.00}");
            GUILayout.Label($"Codec: {_codecMode}, Input/Output Hz: {_sampleRate}/{_outputSampleRate}");
            GUILayout.EndArea();
        }

        private string GetNetworkModeText()
        {
            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                return "Offline";
            }

            if (NetworkManager.IsHost)
            {
                return "Host";
            }

            return NetworkManager.IsClient ? "Client" : "Unknown";
        }

        private string GetLocalClientIdText()
        {
            return NetworkManager != null && NetworkManager.IsListening
                ? NetworkManager.LocalClientId.ToString()
                : "-";
        }

        private string GetTalkerText()
        {
            if (_radioNetworkBridge == null)
            {
                return "No bridge";
            }

            return _radioNetworkBridge.CurrentTalkerId == RadioNetworkBridge.NO_TALKER_ID
                ? "Idle"
                : _radioNetworkBridge.CurrentTalkerId.ToString();
        }

        private void ApplyCaptureGain(float[] samples, int sampleCount)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = SoftLimit(samples[i] * _captureGain);
            }
        }

        private int EncodeFrame(float[] samples, int sampleCount, byte[] encodedFrame)
        {
            if (_codecMode == VoiceCodecMode.Opus)
            {
                return _opusCodec.Encode(samples, encodedFrame);
            }

            MuLawVoiceCodec.Encode(samples, sampleCount, encodedFrame);
            return sampleCount;
        }

        private int DecodeFrame(VoiceCodecMode codecMode, byte[] encodedFrame, int encodedLength, float[] decodedFrame)
        {
            if (codecMode == VoiceCodecMode.Opus)
            {
                return _opusCodec.Decode(encodedFrame, encodedLength, decodedFrame);
            }

            MuLawVoiceCodec.Decode(encodedFrame, Mathf.Min(encodedLength, decodedFrame.Length), decodedFrame);
            return Mathf.Min(encodedLength, decodedFrame.Length);
        }

        private int GetMinimumBufferedSamples()
        {
            return Mathf.Max(0, _minimumBufferedFrames) * _frameSamples;
        }

        private float ReadPlaybackSample()
        {
            float sourceSamplesPerOutputSample = _sampleRate / (float)Mathf.Max(1, _outputSampleRate);
            float sample = GetInterpolatedPlaybackSample(_playbackResampleAccumulator);
            _playbackResampleAccumulator += sourceSamplesPerOutputSample;

            while (_playbackResampleAccumulator >= 1f && _queuedPlaybackSamples > 0)
            {
                _playbackReadPosition = (_playbackReadPosition + 1) % _playbackBuffer.Length;
                _queuedPlaybackSamples--;
                _playbackResampleAccumulator -= 1f;
            }

            return sample;
        }

        private float GetInterpolatedPlaybackSample(float fraction)
        {
            float currentSample = _playbackBuffer[_playbackReadPosition];
            if (_queuedPlaybackSamples <= 1)
            {
                return currentSample;
            }

            int nextReadPosition = (_playbackReadPosition + 1) % _playbackBuffer.Length;
            return Mathf.Lerp(currentSample, _playbackBuffer[nextReadPosition], fraction);
        }

        private static float SoftLimit(float sample)
        {
            return sample / (1f + Mathf.Abs(sample));
        }
    }
}
