using System;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace TermProject.Game
{
    [DisallowMultipleComponent]
    public sealed class PerformanceLogger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        [Header("Logging")]
        [SerializeField] private bool autoStartLogging = true;
        [SerializeField] private bool onlyLogWhilePlaying = true;
        [SerializeField, Min(0.1f)] private float sampleInterval = 1f;
        [SerializeField] private KeyCode toggleLoggingKey = KeyCode.F9;

        private StreamWriter csvWriter;
        private string csvPath;
        private string summaryPath;
        private bool logging;
        private float sampleTimer;
        private int framesInSample;
        private float frameMsSum;
        private float frameMsMin = float.MaxValue;
        private float frameMsMax;
        private int totalSamples;
        private float fpsSum;
        private float lowestFps = float.MaxValue;
        private float highestFrameMs;
        private float highestMainThreadMs;
        private float highestGcAllocKb;
        private long highestAllocatedMemory;
        private int highestActiveBots;
        private int highestWave;
        private bool capturedMainThreadTime;
        private bool capturedGcAlloc;
        private ProfilerRecorder mainThreadTimeRecorder;
        private ProfilerRecorder gcAllocatedInFrameRecorder;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            }
        }

        private void OnEnable()
        {
            StartProfilerRecorders();
        }

        private void Start()
        {
            if (autoStartLogging)
            {
                StartLogging();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleLoggingKey))
            {
                if (logging)
                {
                    StopLogging();
                }
                else
                {
                    StartLogging();
                }
            }

            if (!logging || onlyLogWhilePlaying && !IsGamePlaying())
            {
                return;
            }

            float frameMs = Time.unscaledDeltaTime * 1000f;
            frameMsSum += frameMs;
            frameMsMin = Mathf.Min(frameMsMin, frameMs);
            frameMsMax = Mathf.Max(frameMsMax, frameMs);
            framesInSample++;
            sampleTimer += Time.unscaledDeltaTime;

            if (sampleTimer >= sampleInterval)
            {
                WriteSample();
            }
        }

        private void OnDisable()
        {
            StopLogging();
            DisposeProfilerRecorders();
        }

        private void OnApplicationQuit()
        {
            StopLogging();
        }

        public void StartLogging()
        {
            if (logging)
            {
                return;
            }

            Directory.CreateDirectory(Application.persistentDataPath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            csvPath = Path.Combine(Application.persistentDataPath, $"TermProject_Performance_{timestamp}.csv");
            summaryPath = Path.Combine(Application.persistentDataPath, $"TermProject_Performance_{timestamp}_summary.txt");
            csvWriter = new StreamWriter(csvPath);
            csvWriter.WriteLine("time_seconds,wave,active_bots,fps,avg_frame_ms,min_frame_ms,max_frame_ms,main_thread_ms,gc_alloc_kb,allocated_mb,reserved_mb,mono_mb");

            logging = true;
            ResetStats();
            Debug.Log($"Performance logging started: {csvPath}");
        }

        public void StopLogging()
        {
            if (!logging)
            {
                return;
            }

            if (framesInSample > 0)
            {
                WriteSample();
            }

            WriteSummary();
            csvWriter?.Flush();
            csvWriter?.Dispose();
            csvWriter = null;
            logging = false;
            Debug.Log($"Performance logging saved:\nCSV: {csvPath}\nSummary: {summaryPath}");
        }

        private void WriteSample()
        {
            if (csvWriter == null || framesInSample <= 0)
            {
                ResetSample();
                return;
            }

            float avgFrameMs = frameMsSum / framesInSample;
            float fps = avgFrameMs > 0f ? 1000f / avgFrameMs : 0f;
            int wave = gameManager != null ? gameManager.CurrentWave : 0;
            int activeBots = gameManager != null ? gameManager.ActiveBots : 0;
            long allocated = Profiler.GetTotalAllocatedMemoryLong();
            long reserved = Profiler.GetTotalReservedMemoryLong();
            long mono = Profiler.GetMonoUsedSizeLong();
            float mainThreadMs = GetRecorderMilliseconds(mainThreadTimeRecorder);
            float gcAllocKb = GetRecorderKilobytes(gcAllocatedInFrameRecorder);

            csvWriter.WriteLine(string.Join(
                ",",
                Format(Time.unscaledTime),
                wave.ToString(CultureInfo.InvariantCulture),
                activeBots.ToString(CultureInfo.InvariantCulture),
                Format(fps),
                Format(avgFrameMs),
                Format(frameMsMin),
                Format(frameMsMax),
                FormatOptional(mainThreadMs),
                FormatOptional(gcAllocKb),
                Format(ToMegabytes(allocated)),
                Format(ToMegabytes(reserved)),
                Format(ToMegabytes(mono))));
            csvWriter.Flush();

            totalSamples++;
            fpsSum += fps;
            lowestFps = Mathf.Min(lowestFps, fps);
            highestFrameMs = Mathf.Max(highestFrameMs, frameMsMax);
            TrackHighestOptional(mainThreadMs, ref highestMainThreadMs, ref capturedMainThreadTime);
            TrackHighestOptional(gcAllocKb, ref highestGcAllocKb, ref capturedGcAlloc);
            highestAllocatedMemory = Math.Max(highestAllocatedMemory, allocated);
            highestActiveBots = Mathf.Max(highestActiveBots, activeBots);
            highestWave = Mathf.Max(highestWave, wave);

            ResetSample();
        }

        private void WriteSummary()
        {
            float averageFps = totalSamples > 0 ? fpsSum / totalSamples : 0f;
            float finalLowestFps = totalSamples > 0 ? lowestFps : 0f;

            using (StreamWriter summaryWriter = new StreamWriter(summaryPath))
            {
                summaryWriter.WriteLine("FPS Arena Deathmatch Performance Summary");
                summaryWriter.WriteLine($"Average FPS: {Format(averageFps)}");
                summaryWriter.WriteLine($"Lowest sampled FPS: {Format(finalLowestFps)}");
                summaryWriter.WriteLine($"Highest sampled frame time ms: {Format(highestFrameMs)}");
                summaryWriter.WriteLine($"Highest sampled main thread CPU ms: {FormatOptional(highestMainThreadMs, capturedMainThreadTime)}");
                summaryWriter.WriteLine($"Highest sampled GC alloc KB/frame: {FormatOptional(highestGcAllocKb, capturedGcAlloc)}");
                summaryWriter.WriteLine($"Highest wave reached: {highestWave}");
                summaryWriter.WriteLine($"Highest active bot count: {highestActiveBots}");
                summaryWriter.WriteLine($"Highest allocated memory MB: {Format(ToMegabytes(highestAllocatedMemory))}");
                summaryWriter.WriteLine($"CSV path: {csvPath}");
            }
        }

        private bool IsGamePlaying()
        {
            return gameManager == null || gameManager.CurrentState == GameManager.GameState.Playing;
        }

        private void ResetStats()
        {
            sampleTimer = 0f;
            totalSamples = 0;
            fpsSum = 0f;
            lowestFps = float.MaxValue;
            highestFrameMs = 0f;
            highestMainThreadMs = 0f;
            highestGcAllocKb = 0f;
            highestAllocatedMemory = 0;
            highestActiveBots = 0;
            highestWave = 0;
            capturedMainThreadTime = false;
            capturedGcAlloc = false;
            ResetSample();
        }

        private void ResetSample()
        {
            sampleTimer = 0f;
            framesInSample = 0;
            frameMsSum = 0f;
            frameMsMin = float.MaxValue;
            frameMsMax = 0f;
        }

        private void StartProfilerRecorders()
        {
#if ENABLE_PROFILER
            TryStartRecorder(ref mainThreadTimeRecorder, ProfilerCategory.Internal, "Main Thread");
            TryStartRecorder(ref gcAllocatedInFrameRecorder, ProfilerCategory.Memory, "GC Allocated In Frame");
#endif
        }

        private void DisposeProfilerRecorders()
        {
            if (mainThreadTimeRecorder.Valid)
            {
                mainThreadTimeRecorder.Dispose();
            }

            if (gcAllocatedInFrameRecorder.Valid)
            {
                gcAllocatedInFrameRecorder.Dispose();
            }
        }

        private static void TryStartRecorder(ref ProfilerRecorder recorder, ProfilerCategory category, string statName)
        {
            if (recorder.Valid)
            {
                return;
            }

            try
            {
                recorder = ProfilerRecorder.StartNew(category, statName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PerformanceLogger could not start profiler recorder '{statName}': {exception.Message}");
            }
        }

        private static float GetRecorderMilliseconds(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue * 0.000001f : float.NaN;
        }

        private static float GetRecorderKilobytes(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue / 1024f : float.NaN;
        }

        private static void TrackHighestOptional(float value, ref float highestValue, ref bool capturedValue)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return;
            }

            capturedValue = true;
            highestValue = Mathf.Max(highestValue, value);
        }

        private static float ToMegabytes(long bytes)
        {
            return bytes / (1024f * 1024f);
        }

        private static string FormatOptional(float value, bool capturedValue = true)
        {
            return capturedValue && !float.IsNaN(value) && !float.IsInfinity(value) ? Format(value) : "n/a";
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
