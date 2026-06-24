using System;
using Cysharp.Threading.Tasks;
using JulyCommon;
using UnityEngine;

namespace JulyBoot
{
    public abstract class JulyGameEntry : MonoBehaviour
    {
        [Header("框架配置文件")]
        [SerializeField]
        protected FrameworkConfig frameworkConfig;

        private bool _isInit;

        protected bool IsInitialized => _isInit;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            RunPipeline().Forget();
        }

        private async UniTask RunPipeline()
        {
            try
            {
                var ctx = new LaunchContext(frameworkConfig, destroyCancellationToken);

                var pipeline = new LaunchPipeline();
                ConfigurePipeline(pipeline);

                if (await pipeline.ExecuteAsync(ctx))
                {
                    _isInit = true;
                    JLogger.Log("[Launch] Complete");
                }
            }
            catch (OperationCanceledException)
            {
                JLogger.Log("[Launch] Cancelled");
            }
            catch (Exception ex)
            {
                JLogger.LogException(ex);
            }
        }

        protected abstract void ConfigurePipeline(LaunchPipeline pipeline);

        protected virtual void OnDestroy()
        {
            try
            {
                JulyDI.Clear();
                JLogger.Log("[Launch] Framework shutdown");
            }
            catch (Exception ex)
            {
                JLogger.LogException(ex);
            }
        }
    }
}
