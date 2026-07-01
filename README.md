# JulyBoot

July 框架 L1 装配层（`com.july.boot`）。提供游戏入口基类、启动管线链式编排，以及热更程序集注册契约。连接 AOT 启动链与 JulyArch 架构初始化。

> **本文档描述框架的真实行为，与 `Runtime/` 代码一一对应。**

## 核心机制

### 启动流程

```
JulyGameEntry.Awake
  → DontDestroyOnLoad
  → new LaunchPipeline()
  → ConfigurePipeline(pipeline)     ← 子类 override，链式 Add Step
  → pipeline.ExecuteAsync(ct)
       → 每步 ILaunchStep.ExecuteAsync
       → 返回 false → 中止管线
  → _isInit = true
```

```
JulyGameEntry.OnDestroy
  → ArchContext.Current?.Shutdown()
  → JulyDI.Clear()
```

### JulyGameEntry — 入口基类

抽象 `MonoBehaviour`，模板方法模式：

| 成员 | 说明 |
|------|------|
| `ConfigurePipeline(LaunchPipeline)` | **abstract**，子类链式添加启动步骤 |
| `IsInitialized` | 管线全部成功后为 `true` |
| `OnDestroy` | 自动 Shutdown + Clear（可 override 扩展） |

```csharp
public class GameEntry : JulyGameEntry
{
    protected override void ConfigurePipeline(LaunchPipeline pipeline)
    {
        pipeline
            .Add(new InitPlatformStep())
            .Add(new LoadHotUpdateStep())
            .Add(new RegisterArchStep())
            .Add(new InitArchStep())
            .Add(new GameLaunchStep());

        pipeline.OnStepBegin = (name, current, total) =>
            Debug.Log($"[{current}/{total}] {name}");
    }
}
```

### LaunchPipeline — 链式启动管线

| API | 说明 |
|-----|------|
| `Add(ILaunchStep step)` | 追加步骤，返回 `this` 支持链式调用 |
| `ExecuteAsync(CancellationToken ct)` | 顺序执行，`false` 中止并返回 `false` |
| `OnStepBegin` | `(stepName, currentIndex, totalCount)` 进度回调 |

每步执行前输出 `[Launch] [i/n] StepName` 日志。取消通过 `ct.ThrowIfCancellationRequested()` 传播。

### ILaunchStep — 启动步骤契约

```csharp
public interface ILaunchStep
{
    string Name { get; }
    UniTask<bool> ExecuteAsync(CancellationToken ct);
}
```

| 返回值 | 行为 |
|--------|------|
| `true` | 继续下一步 |
| `false` | 中止管线，`_isInit` 保持 `false` |

典型 Step 实现：平台 SDK 初始化、HybridCLR 加载、ArchContext 注册与 `InitializeAsync`、热更 Registrar 调用。

### IHotUpdateRegistrar — 热更三阶段契约

热更 DLL 加载后由项目层发现实现类并调用（JulyBoot 仅定义接口契约，发现与调用逻辑由消费方 LaunchStep 实现）：

```
Phase 1: Register()
  → 注册 Store / System 到 ArchContext
  → 时机：DLL 加载后、InitializeAsync 前

Phase 2: PreInitializeAsync(ct)
  → 加载配置表等必须在 InitializeAsync 之前完成的工作
  → 时机：Register 之后、InitializeAsync 之前

Phase 3: OnGameLaunch()
  → 配置 UI 窗口、进入首个业务场景
  → 时机：AOT 基础设施就绪、System.OnUpdate 开始驱动后
```

```csharp
public class HotUpdateRegistrar : IHotUpdateRegistrar
{
    public void Register()
    {
        var ctx = ArchContext.Current;
        ctx.RegisterStore<PlayerStore>();
        ctx.RegisterSystem<UISystem>();
    }

    public async UniTask PreInitializeAsync(CancellationToken ct)
    {
        await LoadConfigTablesAsync(ct);
    }

    public async UniTask OnGameLaunch()
    {
        await GetSystem<SceneSystem>().LoadSceneAsync("Main");
    }
}
```

## 层级关系

```
JulyCommon (L0)          JulyArch (L2)
     │                        │
     └────── JulyBoot (L1) ───┘
                │
           项目 GameEntry
                │
         LaunchPipeline Steps
                │
         IHotUpdateRegistrar
```

## 约定

| 约定 | 说明 |
|------|------|
| 一个场景一个 Entry | `DontDestroyOnLoad`，重复 Awake 需项目层防重 |
| Step 返回 false 即中止 | 不抛异常，由调用方检查 `IsInitialized` |
| Shutdown 顺序固定 | 先 ArchContext（System/Store 清理）再 JulyDI |
| Registrar 自动发现 | 实现 `IHotUpdateRegistrar` 即可，无需手动注册接口 |

## 依赖

- `com.july.common` — JLogger、JulyDI
- `com.july.arch` — ArchContext

程序集：`JulyBoot.Runtime`。
