using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    public class ProfilingSampler
    {
        public ProfilingSampler(string name)
        {
            sampler = CustomSampler.Create(name, true);
            inlineSampler = CustomSampler.Create(string.Format("INL_{0}", name));
        }

        public bool IsValid() { return (sampler != null && inlineSampler != null); }

        public CustomSampler sampler;
        public CustomSampler inlineSampler;

        public static ProfilingSampler Get<ProfilingSamplerId>(ProfilingSamplerId samplerId, string nameOverride = "") where ProfilingSamplerId : Enum
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
            if (sampler == null || !sampler.IsValid())
            {
                s_Samplers[index] = new ProfilingSampler(nameOverride == "" ? samplerId.ToString() : nameOverride);
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

    class ProfileSamplers<EnumType>
    {

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

            if (cmd != null)
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
                if (m_Cmd != null)
                    m_Cmd.EndSample(m_Sampler);
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
