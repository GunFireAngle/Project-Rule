using UnityEngine;

namespace RuleGame.Development.P3
{
    /// <summary>
    /// 白盒调试用跟随摄像机：阻尼跟随 + 朝移动方向前瞻。
    /// </summary>
    /// <remarks>
    /// <para>⚠️ 这是 <b>P3 开发与手感测试用</b>的临时摄像机，不是正式方案 ——
    /// 正式的场景摄像机归 P4（场景与叙事）。放在 <c>Development/P3/</c> 是为了与生产代码隔离。</para>
    /// <para>为什么必须写在 <see cref="LateUpdate"/>：玩家是在 <c>FixedUpdate</c> 里移动的，
    /// 在 <c>Update</c> 里跟随会读到「上一帧的位置」，画面会抖。</para>
    /// <para>阻尼用 <see cref="Mathf.SmoothDamp"/>（基于速度的平滑），而不是
    /// <c>Lerp(a, b, k * dt)</c> —— 后者的实际阻尼强度随帧率变化，快机器和慢机器手感不一致。</para>
    /// </remarks>
    public class P3FollowCamera : MonoBehaviour
    {
        // ==================================================================
        //  序列化字段
        // ==================================================================

        [Header("目标")]
        [Tooltip("跟随谁。留空则自动找场景里带 PlayerController 的物体。")]
        [SerializeField] private Transform _target;

        [Tooltip("相对目标的取景偏移（一般把角色放画面偏下一点）。")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 0.5f);

        [Header("阻尼")]
        [Tooltip("横向追上目标所需时间（秒）。越小越跟手。")]
        [SerializeField] private float _smoothTimeX = 0.12f;

        [Tooltip("纵向追上目标所需时间。比横向慢 —— 跳跃时画面不会跟着上下猛甩。")]
        [SerializeField] private float _smoothTimeY = 0.28f;

        [Header("前瞻")]
        [Tooltip("朝移动方向多留出的距离：让你更早看见前进方向上的东西。0 = 关闭。")]
        [SerializeField] private float _lookAhead = 1.2f;

        [Tooltip("前瞻翻转的平滑时间，避免左右横跳时画面来回抽。")]
        [SerializeField] private float _lookAheadSmoothTime = 0.25f;

        // ==================================================================
        //  运行时状态
        // ==================================================================

        private Vector2 _velocity;
        private float   _leadX;
        private float   _leadVelocity;
        private Vector2 _lastTargetPosition;
        private float   _keepZ;
        private bool    _initialized;

        // ==================================================================
        //  生命周期
        // ==================================================================

        private void Awake()
        {
            _keepZ = transform.position.z;      // 只动 XY，Z 保持原样（2D 摄像机不会被推远）

            if (_target == null)
            {
                var player = FindAnyObjectByType<RuleGame.Gameplay.Player.PlayerController>();
                if (player != null) _target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 targetPosition = _target.position;

            // 用「目标这一帧移动了多少」推断朝向 —— 不依赖任何具体组件，敌人也能当目标
            if (_initialized)
            {
                float dx = targetPosition.x - _lastTargetPosition.x;
                if (Mathf.Abs(dx) > 0.0001f)
                {
                    float desiredLead = Mathf.Sign(dx) * _lookAhead;
                    _leadX = Mathf.SmoothDamp(_leadX, desiredLead, ref _leadVelocity, _lookAheadSmoothTime, Mathf.Infinity, dt);
                }
            }
            _lastTargetPosition = targetPosition;

            Vector2 desired = targetPosition + _offset + new Vector2(_leadX, 0f);

            if (!_initialized)
            {
                // 开场直接归位，否则摄像机会从初始位置一路飘过去
                _initialized = true;
                transform.position = new Vector3(desired.x, desired.y, _keepZ);
                return;
            }

            Vector2 current = transform.position;
            current.x = Mathf.SmoothDamp(current.x, desired.x, ref _velocity.x, _smoothTimeX, Mathf.Infinity, dt);
            current.y = Mathf.SmoothDamp(current.y, desired.y, ref _velocity.y, _smoothTimeY, Mathf.Infinity, dt);
            transform.position = new Vector3(current.x, current.y, _keepZ);
        }
    }
}
