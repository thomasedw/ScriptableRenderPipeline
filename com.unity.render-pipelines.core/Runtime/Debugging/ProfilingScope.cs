using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    [Flags]
    public enum ProfilingType
    {
        Cpu = 1 << 0,
        Gpu = 1 << 1,
        InlineCpu = 1 << 2,

        CpuGpu = Cpu | Gpu,
        AllCpu = Cpu | InlineCpu,
        All = Cpu | Gpu | InlineCpu,
    }

    public class ProfilingSampler
    {
        public ProfilingSampler(string name, ProfilingType samplerType)
        {
            bool inlineCpuProfiling = samplerType.HasFlag(ProfilingType.InlineCpu);
            bool cpuProfiling = samplerType.HasFlag(ProfilingType.Cpu);
            bool gpuProfiling = samplerType.HasFlag(ProfilingType.Gpu);

            sampler = (cpuProfiling || gpuProfiling) ? CustomSampler.Create(name, samplerType.HasFlag(ProfilingType.Gpu)) : null;
            inlineSampler = inlineCpuProfiling ? CustomSampler.Create(string.Format("INL_{0}", name)) : null;
            m_SamplerType = samplerType;
        }

        public bool IsValid(ProfilingType samplerType) { return (samplerType == m_SamplerType) && (sampler != null || inlineSampler != null); }

        public CustomSampler sampler;
        public CustomSampler inlineSampler;

        ProfilingType m_SamplerType;

        public static ProfilingSampler Get<ProfilingSamplerId>(ProfilingSamplerId samplerId, ProfilingType samplerType = ProfilingType.Cpu, string nameOverride = "") where ProfilingSamplerId : Enum
        {
            ProfilingSampler[] s_Samplers = null;

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
            if (sampler == null || !sampler.IsValid(samplerType))
            {
                s_Samplers[index] = new ProfilingSampler(nameOverride == "" ? samplerId.ToString() : nameOverride, samplerType);
            }
#if UNITY_EDITOR
            if (nameOverride != "" && s_Samplers[index].sampler.name != nameOverride)
            {
                Debug.LogError(string.Format("Tried to use the same sampler id {0} with a different name override {1}. This is not supported", samplerId, nameOverride));
            }
#endif
            return s_Samplers[(int)(object)samplerId];
        }
    }

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

            if (cmd != null && m_Sampler != null)
                cmd.BeginSample(m_Sampler);
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
                if (m_Cmd != null && m_Sampler != null)
                    m_Cmd.EndSample(m_Sampler);
                m_InlineSampler?.End();
            }

            m_Disposed = true;
        }
    }
}
