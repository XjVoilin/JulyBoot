using Cysharp.Threading.Tasks;

namespace JulyBoot
{
    public interface ILaunchStep
    {
        string Name { get; }
        UniTask<bool> ExecuteAsync(LaunchContext ctx);
    }
}
