using System.Threading;

namespace JulyBoot
{
    public class LaunchContext
    {
        public FrameworkConfig Config { get; }
        public CancellationToken Token { get; }

        internal LaunchContext(FrameworkConfig config, CancellationToken token)
        {
            Config = config;
            Token = token;
        }
    }
}
