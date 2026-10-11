using UnityEngine;

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
        /// ⚠️ 暂停**不能**用 <c>Time.timeScale = 0</c>（策划案 §3.1），所以 <c>Time.deltaTime</c>
        /// 在暂停期间照常流逝 —— 只能靠这个显式闸门冻结。由暂停的持有者（P1 的时间管理器）调用。
        /// </remarks>
        public bool IsPaused { get; private set; }

        /// <summary>设置暂停状态。与 <c>EntityStatus.SetPaused</c> 同一套约定。</summary>
        public void SetPaused(bool paused) => IsPaused = paused;

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError("[RuleSystemDriver] 没有指定 RuleCatalog，规则系统无法初始化。", this);
                return;
            }
            RuleSystem.Initialize(_catalog);
        }

        private void Update()
        {
            if (IsPaused) return;      // 暂停：规则时限不流逝
            RuleSystem.Instance?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            // 让下次进 Play / 切场景重新初始化
            RuleSystem.Reset();
        }
    }
}
