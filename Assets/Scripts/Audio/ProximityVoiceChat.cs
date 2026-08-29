using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class ProximityVoiceChat : NetworkBehaviour
{
    [Header("Microphone Input Settings")]
    [Tooltip("Microphone device to record from. If left empty, the system default device is used.")]
    [SerializeField] private string selectedDevice = "";

    [Tooltip("Sample rate for voice capture (16000 Hz offers clear voice at minimal network bandwidth).")]
    [SerializeField] private int sampleRate = 16000;

    [Tooltip("Voice Activity Detection (VAD) volume threshold. Audio below this level will not transmit.")]
    [SerializeField, Range(0.001f, 0.2f)] private float voiceThreshold = 0.015f;

    [Tooltip("Input gain multiplier applied to microphone volume.")]
    [SerializeField, Range(0.5f, 5f)] private float micGain = 1.5f;

    [Tooltip("If true, microphone only transmits while holding the Push-to-Talk key.")]
    [SerializeField] private bool pushToTalk = false;

    [Tooltip("Key used for Push-to-Talk.")]
    [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;

    [Header("Proximity 3D Audio Playback")]
    [Tooltip("AudioSource used to play back remote player voice in 3D space. Created automatically if null.")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Tooltip("Volume for remote player voice playback.")]
    [SerializeField, Range(0f, 1f)] private float playbackVolume = 1.0f;

    [Tooltip("Distance at which remote player voice is at maximum volume.")]
    [SerializeField] private float minDistance = 1.5f;

    [Tooltip("Maximum distance at which remote player voice can be heard.")]
    [SerializeField] private float maxDistance = 25f;

    [Header("Enemy Perception & Hearing Alert")]
    [Tooltip("If true, speaking will broadcast noise events to nearby AI / enemies.")]
    [SerializeField] private bool alertEnemiesOnSpeak = true;

    [Tooltip("Maximum noise radius (meters) when shouting / speaking at peak volume.")]
    [SerializeField] private float maxAlertRadius = 25f;

    [Tooltip("Minimum noise radius (meters) when whispering / speaking at threshold volume.")]
    [SerializeField] private float minAlertRadius = 3f;

    [Tooltip("Layer mask used to detect enemies when speaking.")]
    [SerializeField] private LayerMask enemyLayer = ~0;

    [Header("Debug")]
    [Tooltip("Print a debug log in the Unity console whenever speaking is detected / starts.")]
    [SerializeField] private bool logWhenSpeaking = true;

    // Public State Properties
    public float CurrentLoudness { get; private set; } = 0f;
    public bool IsSpeaking { get; private set; } = false;
    public bool IsPushToTalkEnabled => pushToTalk;
    public string SelectedDevice => selectedDevice;

    // Events
    public static event Action<Vector3, float, ulong> OnPlayerSpoke;
    public event Action<bool> OnSpeakingStateChanged;

    // Internal Recording State (Local Owner)
    private AudioClip recordingClip;
    private int lastSamplePosition = 0;
    private float[] micSampleBuffer;
    private byte[] transmissionBuffer;
    private const float PacketInterval = 0.05f; // 50ms chunks (20 packets/sec)
    private int samplesPerPacket;
    private float packetTimer = 0f;
    private int speechHangoverFrames = 0;
    private const int MaxHangoverFrames = 4; // Keep transmitting for ~200ms after silence to avoid choppy cuts

    // Internal Playback State (Remote Clients)
    private AudioClip streamingClip;
    private const int PlaybackBufferSeconds = 2;
    private float[] playbackRingBuffer;
    private int playbackWritePosition = 0;
    private int totalPlaybackSamples;
    private bool isPlaybackInitialized = false;

    // AI Perception Buffer
    private readonly Collider[] enemyHitBuffer = new Collider[16];

    private void Awake()
    {
        samplesPerPacket = Mathf.CeilToInt(sampleRate * PacketInterval);
        micSampleBuffer = new float[samplesPerPacket];
        transmissionBuffer = new byte[samplesPerPacket];

        totalPlaybackSamples = sampleRate * PlaybackBufferSeconds;
        playbackRingBuffer = new float[totalPlaybackSamples];

        InitializeAudioSource();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            StartMicrophoneCapture();
        }
        else
        {
            InitializeRemotePlayback();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            StopMicrophoneCapture();
        }
        else
        {
            StopRemotePlayback();
        }

        base.OnNetworkDespawn();
    }

    private void InitializeAudioSource()
    {
        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure 3D Spatial Audio properties
        voiceAudioSource.spatialBlend = 1.0f; // 100% 3D spatialization
        voiceAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        voiceAudioSource.minDistance = minDistance;
        voiceAudioSource.maxDistance = maxDistance;
        voiceAudioSource.dopplerLevel = 0.0f; // Prevent pitch distortion when moving
        voiceAudioSource.spread = 60f;
        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.loop = true;
    }

    private void InitializeRemotePlayback()
    {
        if (isPlaybackInitialized) return;

        streamingClip = AudioClip.Create($"VoiceStream_{OwnerClientId}", totalPlaybackSamples, 1, sampleRate, false);
        Array.Clear(playbackRingBuffer, 0, playbackRingBuffer.Length);
        streamingClip.SetData(playbackRingBuffer, 0);

        if (voiceAudioSource != null)
        {
            voiceAudioSource.clip = streamingClip;
            voiceAudioSource.volume = playbackVolume;
            voiceAudioSource.Play();
        }

        playbackWritePosition = 0;
        isPlaybackInitialized = true;
    }

    private void StopRemotePlayback()
    {
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = null;
        }

        if (streamingClip != null)
        {
            Destroy(streamingClip);
            streamingClip = null;
        }

        isPlaybackInitialized = false;
    }

    #region Microphone Capture (Owner)

    public void StartMicrophoneCapture()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[ProximityVoiceChat] No microphone devices found on this system.");
            return;
        }

        // Pick default device if specified device is not available
        string deviceToUse = null;
        if (!string.IsNullOrEmpty(selectedDevice))
        {
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                if (Microphone.devices[i] == selectedDevice)
                {
                    deviceToUse = selectedDevice;
                    break;
                }
            }
        }

        if (deviceToUse == null)
        {
            deviceToUse = Microphone.devices[0];
            selectedDevice = deviceToUse;
        }

        // 1-second circular capture buffer
        recordingClip = Microphone.Start(deviceToUse, true, 1, sampleRate);
        lastSamplePosition = 0;
        packetTimer = 0f;

        Debug.Log($"[ProximityVoiceChat] Microphone started on device '{deviceToUse}' at {sampleRate} Hz.");
    }

    public void StopMicrophoneCapture()
    {
        if (!string.IsNullOrEmpty(selectedDevice) && Microphone.IsRecording(selectedDevice))
        {
            Microphone.End(selectedDevice);
        }

        if (recordingClip != null)
        {
            Destroy(recordingClip);
            recordingClip = null;
        }

        SetSpeakingState(false);
        CurrentLoudness = 0f;
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateLocalVoiceCapture();
    }

    private void UpdateLocalVoiceCapture()
    {
        if (recordingClip == null || string.IsNullOrEmpty(selectedDevice) || !Microphone.IsRecording(selectedDevice))
        {
            return;
        }

        packetTimer += Time.deltaTime;
        if (packetTimer < PacketInterval) return;
        packetTimer = 0f;

        int currentMicPos = Microphone.GetPosition(selectedDevice);
        if (currentMicPos < 0 || currentMicPos == lastSamplePosition) return;

        int clipLength = recordingClip.samples;
        int samplesAvailable = (currentMicPos - lastSamplePosition + clipLength) % clipLength;

        if (samplesAvailable < samplesPerPacket) return;

        // Read recorded PCM samples into buffer
        recordingClip.GetData(micSampleBuffer, lastSamplePosition);
        lastSamplePosition = (lastSamplePosition + samplesPerPacket) % clipLength;

        // Calculate Root Mean Square (RMS) volume and peak loudness
        float sum = 0f;
        for (int i = 0; i < samplesPerPacket; i++)
        {
            float s = micSampleBuffer[i] * micGain;
            micSampleBuffer[i] = Mathf.Clamp(s, -1.0f, 1.0f);
            sum += s * s;
        }

        float rms = Mathf.Sqrt(sum / samplesPerPacket);
        CurrentLoudness = Mathf.Clamp01(rms * 4f);

        // Check Voice Activity Detection or Push-to-Talk
        bool pttActive = !pushToTalk || Input.GetKey(pushToTalkKey);
        bool aboveThreshold = rms >= voiceThreshold;

        if (aboveThreshold && pttActive)
        {
            speechHangoverFrames = MaxHangoverFrames;
        }
        else if (speechHangoverFrames > 0)
        {
            speechHangoverFrames--;
        }

        bool shouldTransmit = speechHangoverFrames > 0 && pttActive;
        SetSpeakingState(shouldTransmit);

        if (shouldTransmit)
        {
            // Quantize float (-1f..1f) to 8-bit byte (0..255) for efficient low-latency transmission
            for (int i = 0; i < samplesPerPacket; i++)
            {
                float normalized = (micSampleBuffer[i] * 0.5f) + 0.5f;
                transmissionBuffer[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(normalized * 255f), 0, 255);
            }

            // Transmit packet across network
            SendVoicePacketServerRpc(transmissionBuffer, samplesPerPacket);

            // Alert nearby AI if enabled
            if (alertEnemiesOnSpeak && CurrentLoudness > 0.05f)
            {
                AlertNearbyEnemies(transform.position, CurrentLoudness);
            }

            // Broadcast static event for external listeners
            OnPlayerSpoke?.Invoke(transform.position, CurrentLoudness, OwnerClientId);
        }
    }

    private void SetSpeakingState(bool speaking)
    {
        if (IsSpeaking != speaking)
        {
            IsSpeaking = speaking;
            OnSpeakingStateChanged?.Invoke(speaking);

            if (logWhenSpeaking && speaking)
            {
                Debug.Log($"<color=#00FF66>[ProximityVoiceChat]</color> Speaking detected! Loudness: {CurrentLoudness:F2} | Owner: {OwnerClientId} | PTT: {(!pushToTalk || Input.GetKey(pushToTalkKey))}");
            }
        }
    }

    #endregion

    #region Network Voice Transport (ServerRpc / ClientRpc)

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    private void SendVoicePacketServerRpc(byte[] voiceData, int sampleCount, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // Relay voice packet to all other clients
        BroadcastVoicePacketClientRpc(voiceData, sampleCount, OwnerClientId);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void BroadcastVoicePacketClientRpc(byte[] voiceData, int sampleCount, ulong senderClientId)
    {
        // Local owner doesn't need to play back their own voice
        if (IsOwner) return;

        if (!isPlaybackInitialized)
        {
            InitializeRemotePlayback();
        }

        if (streamingClip == null || voiceData == null || sampleCount <= 0) return;

        // Decode 8-bit quantized bytes back to float audio samples
        float[] decodedSamples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float normalized = voiceData[i] / 255f;
            decodedSamples[i] = (normalized - 0.5f) * 2.0f;
        }

        // Stream into continuous ring buffer
        streamingClip.SetData(decodedSamples, playbackWritePosition);
        playbackWritePosition = (playbackWritePosition + sampleCount) % totalPlaybackSamples;
    }

    #endregion

    #region Enemy Perception / Hearing Broadcast

    private void AlertNearbyEnemies(Vector3 noisePosition, float loudness)
    {
        float alertRadius = Mathf.Lerp(minAlertRadius, maxAlertRadius, loudness);

        int hitCount = Physics.OverlapSphereNonAlloc(noisePosition, alertRadius, enemyHitBuffer, enemyLayer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = enemyHitBuffer[i];
            if (col == null) continue;

            AI enemyAI = col.GetComponentInParent<AI>();
            if (enemyAI != null)
            {
                // Trigger AI reaction or pass noise source position
                enemyAI.SendMessage("OnHeardNoise", noisePosition, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    #endregion

    #region Public Configuration Methods

    public void SetPushToTalk(bool enabled, KeyCode key = KeyCode.V)
    {
        pushToTalk = enabled;
        pushToTalkKey = key;
    }

    public void SetVolume(float volume)
    {
        playbackVolume = Mathf.Clamp01(volume);
        if (voiceAudioSource != null)
        {
            voiceAudioSource.volume = playbackVolume;
        }
    }

    public void SetDevice(string deviceName)
    {
        if (selectedDevice == deviceName) return;

        selectedDevice = deviceName;
        if (IsOwner && Microphone.IsRecording(selectedDevice))
        {
            StopMicrophoneCapture();
            StartMicrophoneCapture();
        }
    }

    public void SetVoiceThreshold(float threshold)
    {
        voiceThreshold = Mathf.Clamp(threshold, 0.001f, 0.5f);
    }

    public void SetMicGain(float gain)
    {
        micGain = Mathf.Clamp(gain, 0.1f, 10f);
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize 3D Audio Proximity Hearing Radius in Unity Scene View
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, minDistance);

        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        if (Application.isPlaying && IsSpeaking)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
            float currentRadius = Mathf.Lerp(minAlertRadius, maxAlertRadius, CurrentLoudness);
            Gizmos.DrawWireSphere(transform.position, currentRadius);
        }
    }
#endif
}
