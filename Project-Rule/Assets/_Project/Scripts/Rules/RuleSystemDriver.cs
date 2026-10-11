using UnityEngine;
using RuleGame.Infrastructure.Common;

namespace RuleGame.Core.Rules
{
    /// <summary>
    /// 规则系统的启动器与心跳。
    /// </summary>
    /// <remarks>
    /// 挂在常驻场景对象上（或做成跨场景的 Prefab）。
    /// 负责两件事：<b>初始化单例</b>、<b>每帧 Tick</b>。
    /// </remarks>
    [DefaultExecutionOrder(-100)]   // 比其他脚本先 Awake
    public class RuleSystemDriver : MonoBehaviour
    {
        [Tooltip("规则目录资产，放在 Assets/_Project/Data/Rules/")]
        [SerializeField] private RuleCatalog _catalog;

        private void Awake()
        {
            // 对象注册表先起来：实体在 OnEnable 里会来找它。
            WorldRegistry.Initialize();

            if (_catalog == null)
            {
                Debug.LogError("[RuleSystemDriver] 没有指定 RuleCatalog，规则系统无法初始化。", this);
                return;
            }
            RuleSystem.Initialize(_catalog);
        }

        private void Update()
        {
            // ⚠️ TODO(P1)：暂停用的是 Physics2D.simulationMode = Script（策划案 §3.1
            // 禁止 Time.timeScale = 0），但 Time.deltaTime 在暂停期间照常走，
            // 规则时长会在暂停时被扣掉。等 ITimeManager 落地后改用它的 delta。
            RuleSystem.Instance?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            // 让下次进 Play / 切场景重新初始化
            RuleSystem.Reset();
            WorldRegistry.Reset();
        }
    }
}
