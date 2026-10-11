using UnityEngine;
using UnityEngine.InputSystem;
using RuleGame.Core.Rules;
using RuleGame.Gameplay.Combat;

namespace RuleGame.Gameplay.Player
{
    /// <summary>
    /// 玩家移动控制。Kinematic 刚体 + <see cref="Rigidbody2D.Slide"/> 自行解算碰撞。
    /// </summary>
    /// <remarks>
    /// <para><b>为什么是 Kinematic</b>（架构决策 D1）：移动完全由代码决定，手感可控。
    /// 代价是引擎不再替我们挡住静态地形 —— <c>MovePosition</c> 自己不处理碰撞 ——
    /// 所以位移必须走 <see cref="Rigidbody2D.Slide"/>：它按 <c>surfaceUp</c> 解析接触面角度、
    /// 在设定斜率内沿面滑行、并多次叠代处理拐角。</para>
    /// <para><b>重力自己积分</b>，并且直接读 <c>Physics2D.gravity</c>：重力规则改的是这个全局值，
    /// 本类无需改一行就会跟着变 —— 「规则作用于世界本身」就是这样落地的。
    /// 重力<b>不能</b>交给 <c>SlideMovement.gravity</c>：那只会得到匀速下落，没有加速度。</para>
    /// <para><c>surfaceUp</c> 取当前重力的反方向。于是「重力反转」之后天花板就是新的地面，
    /// 接地判定与起跳方向会自动跟着翻；「重力失效」时沿用上一次的方向，避免方向退化成零向量。</para>
    /// <para>物理运算写在 <see cref="FixedUpdate"/>（Unity 2D 物理要求）。</para>
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IKnockbackReceiver
    {
        // ==================================================================
        //  序列化字段
        // ==================================================================

        [Header("输入")]
        [Tooltip("拖入 Assets/_Project/Settings/InputSystem_Actions.inputactions")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("移动")]
        [SerializeField] private float _moveSpeed    = 8f;
        [SerializeField] private float _jumpSpeed    = 14f;
        [SerializeField] private float _gravityScale = 1f;

        [Tooltip("无输入时的水平减速度；会被 FrictionScale 缩放（摩擦失效 ≈ 冰面）。")]
        [SerializeField] private float _frictionDecel = 40f;

        [Header("碰撞")]
        [Tooltip("阻挡移动、并用于接地判定的层。地形与墙体都放这里。")]
        [SerializeField] private LayerMask _solidLayer;

        [Tooltip("接地探测距离（米）。")]
        [SerializeField] private float _groundProbeDistance = 0.08f;

        [Tooltip("贴地吸附距离：下坡时把角色按回地面，0 = 关闭。")]
        [SerializeField] private float _surfaceAnchorDistance = 0.05f;

        [Tooltip("可沿其滑行的最大坡面角度。竖直墙是 90°，所以必须小于 90 才会被挡住。")]
        [SerializeField] private float _surfaceSlideAngle = 89f;

        [Header("击退")]
        [Tooltip("外部冲量每秒的衰减量。")]
        [SerializeField] private float _knockbackDecay = 30f;

        [Header("状态")]
        [Tooltip("同一物体上的状态组件；暂停状态从这里读。留空则自动取。")]
        [SerializeField] private EntityStatus _status;

        [Header("攻击")]
        [SerializeField] private CombatSystem _combat;
        [SerializeField] private float     _attackDamage    = 10f;
        [SerializeField] private float     _attackKnockback = 6f;
        [SerializeField] private float     _attackCooldown  = 0.25f;
        [SerializeField] private Vector2   _attackBoxSize   = new Vector2(1.2f, 1f);
        [SerializeField] private float     _attackOffset    = 0.8f;
        [SerializeField] private LayerMask _attackMask;

        [Header("能量")]
        [Tooltip("每命中一个目标恢复的能量（策划案 §四：攻击小怪回能）。未命中不给。")]
        [SerializeField] private float _energyPerHit = 4f;

        // ==================================================================
        //  运行时状态
        // ==================================================================

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[4];

        private Rigidbody2D _rb;
        private Vector2 _velocity;          // 自己维护的速度：重力 / 输入 / 跳跃
        private Vector2 _externalVelocity;  // 击退等外部冲量，独立衰减，不掺进重力与摩擦
        private Vector2 _moveInput;
        private Vector2 _up = Vector2.up;   // 当前「上」方向 = 重力反方向
        private bool    _jumpQueued;
        private bool    _grounded;
        private float   _facing = 1f;
        private float   _nextAttackTime;

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;

        /// <summary>是否站在支撑面上（供踩踏判定与调试用）。</summary>
        public bool IsGrounded => _grounded;

        /// <summary>暂停中不读输入、不推进物理。</summary>
        /// <remarks>暂停状态由 <c>EntityStatus.SetPaused</c> 驱动 —— 暂停持有者（P1 的时间管理器）负责调用。</remarks>
        private bool IsPaused => _status != null && _status.IsPaused;

        // ==================================================================
        //  生命周期
        // ==================================================================

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            // Kinematic + 自己解算（见类注释）。gravityScale 置 0：重力在 FixedUpdate 里自己积分。
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.linearDamping = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;           // 平台角色不翻滚
            _rb.useFullKinematicContacts = true;                              // Kinematic 与静态体 / 推动物之间也要产生接触

            if (_status == null) _status = GetComponent<EntityStatus>();

            UpdateUp();                                                       // 起手就有正确的「上」方向

            if (_inputActions != null)
            {
                _moveAction   = _inputActions.FindAction("Player/Move",   false);
                _jumpAction   = _inputActions.FindAction("Player/Jump",   false);
                _attackAction = _inputActions.FindAction("Player/Attack", false);
            }

            if (_moveAction == null)
                Debug.LogWarning("[PlayerController] 未找到输入动作，请把 InputSystem_Actions 拖到 _inputActions。", this);
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _jumpAction?.Enable();
            _attackAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
            _attackAction?.Disable();
        }

        // ==================================================================
        //  输入（视觉帧）
        // ==================================================================

        private void Update()
        {
            if (IsPaused)
            {
                _moveInput = Vector2.zero;
                _jumpQueued = false;
                return;
            }

            _moveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

            if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
                _jumpQueued = true;

            if (_attackAction != null && _attackAction.WasPressedThisFrame())
                DoAttack();
        }

        // ==================================================================
        //  物理（固定帧）
        // ==================================================================

        private void FixedUpdate()
        {
            if (IsPaused)
            {
                // 暂停期间清空速度：否则重力会在暂停的每一帧继续堆积，
                // 恢复的瞬间一次性位移出去。
                _velocity         = Vector2.zero;
                _externalVelocity = Vector2.zero;
                return;
            }

            float dt = Time.fixedDeltaTime;
            UpdateUp();

            // ① 重力：自己积分（要的是加速度，所以累加，不交给 SlideMovement.gravity）
            _velocity += Physics2D.gravity * _gravityScale * dt;

            // ② 水平：有输入直接给速度，无输入按摩擦力减速
            if (Mathf.Abs(_moveInput.x) > 0.01f)
            {
                _velocity.x = _moveInput.x * _moveSpeed;
                _facing = Mathf.Sign(_moveInput.x);
            }
            else
            {
                float friction = Mathf.Max(0.01f, FrictionScale);   // 0 = 无摩擦
                _velocity.x = Mathf.MoveTowards(_velocity.x, 0f, _frictionDecel * friction * dt);
            }

            // ③ 接地：查询刚体自身的全部碰撞体，不需要额外的地面检测子物体
            _grounded = ProbeGround();

            // ④ 贴地时抵消朝地面累积的速度
            if (_grounded && Vector2.Dot(_velocity, _up) < 0f)
                _velocity -= _up * Vector2.Dot(_velocity, _up);

            // ⑤ 跳跃
            if (_jumpQueued)
            {
                if (_grounded) _velocity = SetAlong(_velocity, _up, _jumpSpeed);
                _jumpQueued = false;
            }

            // ⑥ 击退：外部速度独立衰减
            _externalVelocity = Vector2.MoveTowards(_externalVelocity, Vector2.zero, _knockbackDecay * dt);

            // ⑦ 位移交给 Slide 解算接触（Kinematic 的 MovePosition 自己不处理碰撞）
            var result = _rb.Slide(_velocity + _externalVelocity, dt, BuildSlideMovement());

            // ⑧ 撞墙 / 撞顶：Slide 把用不掉的速度还回来，清掉对应分量，避免贴着墙持续挤压
            if (Mathf.Abs(result.remainingVelocity.x) > 0.001f) _velocity.x = 0f;
            if (Mathf.Abs(result.remainingVelocity.y) > 0.001f) _velocity.y = 0f;
        }

        /// <summary>每次物理帧的 Slide 配置。</summary>
        private Rigidbody2D.SlideMovement BuildSlideMovement()
        {
            // 上升途中不加吸附，否则起跳会被按回地面。
            bool anchored = _grounded && Vector2.Dot(_velocity, _up) <= 0.01f;

            return new Rigidbody2D.SlideMovement
            {
                surfaceUp         = _up,
                surfaceSlideAngle = _surfaceSlideAngle,
                gravitySlipAngle  = 0f,                  // 不沿斜面自动下滑
                gravity           = Vector2.zero,        // 重力已在 ① 自己积分
                maxIterations     = 4,                   // 拐角要几次叠代才滑得干净
                surfaceAnchor     = anchored ? -_up * _surfaceAnchorDistance : Vector2.zero,
                useLayerMask      = true,
                layerMask         = _solidLayer,
                useAttachedTriggers = false,
                useSimulationMove = true,                // 交给模拟步生效：暂停（不 Simulate）时自然静止
            };
        }

        /// <summary>沿当前「上」方向的反向做一次碰撞体扫描，看脚下有没有支撑面。</summary>
        private bool ProbeGround()
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(_solidLayer);

            int count = _rb.Cast(-_up, filter, _groundHits, _groundProbeDistance);
            for (int i = 0; i < count; i++)
            {
                // 只有法线朝着「上」的接触才算支撑面；墙面法线是水平的，会被排除。
                if (Vector2.Dot(_groundHits[i].normal, _up) > 0.5f) return true;
            }
            return false;
        }

        /// <summary>「上」方向 = 重力反方向；重力失效（零向量）时沿用上一次的方向。</summary>
        private void UpdateUp()
        {
            if (Physics2D.gravity.sqrMagnitude > 0.0001f)
                _up = -Physics2D.gravity.normalized;
        }

        /// <summary>把速度沿 <paramref name="axis"/> 的分量设为 <paramref name="value"/>，其余分量不动。</summary>
        private static Vector2 SetAlong(Vector2 velocity, Vector2 axis, float value)
            => velocity - axis * Vector2.Dot(velocity, axis) + axis * value;

        // ==================================================================
        //  击退
        // ==================================================================

        /// <summary>外部冲量入口（击退）。见 <see cref="IKnockbackReceiver"/>。</summary>
        /// <remarks><b>不能</b>直接写 <c>Rigidbody2D.linearVelocity</c>：本类每个物理帧都会整体
        /// 覆盖速度，那样写进去的冲量活不过一帧。</remarks>
        public void AddImpulse(Vector2 impulse) => _externalVelocity += impulse;

        // ==================================================================
        //  攻击
        // ==================================================================

        private void DoAttack()
        {
            if (_combat == null) return;
            if (Time.time < _nextAttackTime) return;      // 攻击冷却：连点不会连打

            _nextAttackTime = Time.time + _attackCooldown;

            Vector2 center = (Vector2)transform.position + new Vector2(_facing * _attackOffset, 0f);
            int hits = _combat.PerformAttack(this, center, _attackBoxSize, _attackMask,
                                             _attackDamage, _attackKnockback);

            // 能量恢复来源之一：命中即回，未命中不给（策划案 §四）。
            if (hits > 0 && _status != null)
                _status.RestoreEnergy(_energyPerHit * hits);
        }

        /// <summary>攻击判定体：调参时直接看得到范围与朝向。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.2f, 0.8f);
            Vector2 center = (Vector2)transform.position + new Vector2(_facing * _attackOffset, 0f);
            Gizmos.DrawWireCube(center, _attackBoxSize);
        }

        // ==================================================================
        //  属性读取
        // ==================================================================

        /// <summary>当前摩擦缩放（1.0 = 正常）。规则系统未就绪时退化为 1。</summary>
        private static float FrictionScale
        {
            get
            {
                var attrs = RuleSystem.Instance?.Attributes;
                return attrs?.Values.FrictionScale ?? 1f;
            }
        }
    }
}
