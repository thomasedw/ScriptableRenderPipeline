//#define ENABLE_GPU_PROFILER

using System;
using UnityEngine.Profiling;


namespace UnityEngine.Rendering
{
    /// <summary>
    /// Wrapper around CPU and GPU profiling samplers.
    /// Use this along ProfilingScope to profile a piece of code.
    /// </summary>
    public class ProfilingSampler
    {
        public ProfilingSampler(string name)
        {
#if ENABLE_GPU_PROFILER
            sampler = CustomSampler.Create(name, true); // Event markers, command buffer CPU profiling and GPU profiling
#else
            sampler = CustomSampler.Create(name); // Event markers, command buffer CPU profiling and GPU profiling
#endif
            inlineSampler = CustomSampler.Create($"Inl_{name}"); // Profiles code "immediately"

            m_Recorder = sampler.GetRecorder();
            m_Recorder.enabled = false;
            m_InlineRecorder = inlineSampler.GetRecorder();
            m_InlineRecorder.enabled = false;
        }

        public bool IsValid() { return (sampler != null && inlineSampler != null); }

        internal CustomSampler sampler { get; private set; }
        internal CustomSampler inlineSampler { get; private set; }

        Recorder m_Recorder;
        Recorder m_InlineRecorder;

        public bool enableRecording
        {
            set
            {
                m_Recorder.enabled = value; ;
                m_InlineRecorder.enabled = value; ;
            }
        }

        public string name => sampler.name;
#if ENABLE_GPU_PROFILER
        public float gpuElapsedTime => m_Recorder.enabled ? m_Recorder.gpuElapsedNanoseconds / 1000000.0f : 0.0f;
        public int gpuSampleCount => m_Recorder.enabled ? m_Recorder.gpuSampleBlockCount : 0;
#else
        public float gpuElapsedTime => 0.0f;
        public int gpuSampleCount => 0;
#endif
        public float cpuElapsedTime => m_Recorder.enabled ? m_Recorder.elapsedNanoseconds / 1000000.0f : 0.0f;
        public int cpuSampleCount => m_Recorder.enabled ? m_Recorder.sampleBlockCount : 0;
        public float inlineCpuElapsedTime => m_InlineRecorder.enabled ? m_InlineRecorder.elapsedNanoseconds / 1000000.0f : 0.0f;
        public int inlineCpuSampleCount => m_InlineRecorder.enabled ? m_InlineRecorder.sampleBlockCount : 0;
    }

    /// <summary>
    /// Helper class to manage profiling samplers referenced by an Id derived from an enumeration
    /// </summary>
    /// <typeparam name="ProfilingSamplerId"></typeparam>
    public class ProfileSamplerList<ProfilingSamplerId> where ProfilingSamplerId : Enum
    {
        static ProfilingSampler[] s_Samplers = null;

        static public ProfilingSampler Get(ProfilingSamplerId samplerId)
        {
            if (s_Samplers == null)
            {
                int max = 0;
                foreach (var value in Enum.GetValues(typeof(ProfilingSamplerId)))
                {
                    max = Math.Max(max, (int)value);
                }
                s_Samplers = new ProfilingSampler[max + 1];
            }

            int index = (int)(object)samplerId;
            var sampler = s_Samplers[index];
            if (sampler == null || !sampler.IsValid())
            {
                s_Samplers[index] = new ProfilingSampler(samplerId.ToString());
            }

            return s_Samplers[index];
        }
    }

    /// <summary>
    /// Scoped Profiling makers
    /// </summary>
    public struct ProfilingScope : IDisposable
    {
        CommandBuffer   m_Cmd;
        bool            m_Disposed;
        CustomSampler   m_Sampler;
        CustomSampler   m_InlineSampler;

        public ProfilingScope(CommandBuffer cmd, ProfilingSampler sampler)
        {
            m_Cmd = cmd;
            m_Disposed = false;
            m_Sampler = sampler.sampler;
            m_InlineSampler = sampler.inlineSampler;

#if ENABLE_GPU_PROFILER
            if (cmd != null)
                cmd.BeginSample(m_Sampler);
#else
            if (cmd != null)
                cmd.BeginSample(m_Sampler.name);
#endif
            m_InlineSampler?.Begin();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        // Protected implementation of Dispose pattern.
        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // As this is a struct, it could have been initialized using an empty constructor so we
            // need to make sure `cmd` isn't null to avoid a crash. Switching to a class would fix
            // this but will generate garbage on every frame (and this struct is used quite a lot).
            if (disposing)
            {
#if ENABLE_GPU_PROFILER
                if (m_Cmd != null)
                    m_Cmd.EndSample(m_Sampler);
#else
                if (m_Cmd != null)
                    m_Cmd.EndSample(m_Sampler.name);
#endif
                m_InlineSampler?.End();
            }

            m_Disposed = true;
        }
    }

    [System.Obsolete("Please use ProfilingScope")]
    public struct ProfilingSample : IDisposable
    {
        readonly CommandBuffer m_Cmd;
        readonly string m_Name;

        bool m_Disposed;
        CustomSampler m_Sampler;

        public ProfilingSample(CommandBuffer cmd, string name, CustomSampler sampler = null)
        {
            m_Cmd = cmd;
            m_Name = name;
            m_Disposed = false;
            if (cmd != null && name != "")
                cmd.BeginSample(name);
            m_Sampler = sampler;
            m_Sampler?.Begin();
        }

        // Shortcut to string.Format() using only one argument (reduces Gen0 GC pressure)
        public ProfilingSample(CommandBuffer cmd, string format, object arg) : this(cmd, string.Format(format, arg))
        {
        }

        // Shortcut to string.Format() with variable amount of arguments - for performance critical
        // code you should pre-build & cache the marker name instead of using this
        public ProfilingSample(CommandBuffer cmd, string format, params object[] args) : this(cmd, string.Format(format, args))
        {
        }

        public void Dispose()
        {
            Dispose(true);
        }

        // Protected implementation of Dispose pattern.
        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // As this is a struct, it could have been initialized using an empty constructor so we
            // need to make sure `cmd` isn't null to avoid a crash. Switching to a class would fix
            // this but will generate garbage on every frame (and this struct is used quite a lot).
            if (disposing)
            {
                if (m_Cmd != null && m_Name != "")
                    m_Cmd.EndSample(m_Name);
                m_Sampler?.End();
            }

            m_Disposed = true;
        }
    }
}
