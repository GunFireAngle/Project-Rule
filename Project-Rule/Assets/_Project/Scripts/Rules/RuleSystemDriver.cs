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

        /// <summary>暂停中。规则倒计时与行为型规则都不前进。</summary>
        /// <remarks>
        /// <para>⚠️ 暂停**不能**用 <c>Time.timeScale = 0</c>（策划案 §3.1），所以 <c>Time.deltaTime</c>
        /// 在暂停期间照常流逝 —— 只能靠这个显式闸门冻结。由暂停的持有者（P1 的时间管理器）调用。</para>
        /// <para>🔻 <b>过渡期闸门（决策 2026-10-12）</b>：暂停的最终形态是让本组件改读
        /// <c>ITimeManager.DeltaTime</c>（暂停时返回 0），届时这个布尔量应当**删掉**，
        /// 让「暂停」只存在一份真相。P1 的 <c>ITimeManager</c> 落地前先留着它。</para>
        /// </remarks>
        public bool IsPaused { get; private set; }

        /// <summary>设置暂停状态。与 <c>EntityStatus.SetPaused</c> 同一套约定。</summary>
        public void SetPaused(bool paused) => IsPaused = paused;

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
            // 暂停由两层组成，缺一不可：
            //   ① 物理层：Physics2D.simulationMode = Script（策划案 §3.1 禁止 Time.timeScale = 0）
            //   ② 时钟层：下面这个闸门，挡住 Time.deltaTime 在暂停期间照常推进导致的规则时限流失
            // TODO(P1)：ITimeManager 落地后改读它的 delta，并删掉 IsPaused（见属性注释）。
            if (IsPaused) return;
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
