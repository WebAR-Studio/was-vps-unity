using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class SpriteAnimationTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Камера, на которую смотрит спрайт")]
    public Camera targetCamera;

    [Header("View Settings")]
    [Tooltip("Максимальный угол между направлением камеры и вектором к спрайту")]
    public float viewAngleThreshold = 15f;

    [Header("Animation Settings")]
    [Tooltip("Триггер из Idle → Playing")]
    public string startTrigger = "Start";
    [Tooltip("Триггер из Playing → Finish")]
    public string finishTrigger = "Finish";
    [Tooltip("Триггер из Playing → Idle (Return)")]
    public string returnTrigger = "Return";
    [Tooltip("Имя состояния основной анимации")]
    public string playingStateName = "Playing";
    [Tooltip("Имя состояния Idle")]
    public string idleStateName = "Idle";
    [Tooltip("Имя состояния Finish")]
    public string finishStateName = "Finish";

    public event System.Action<SpriteAnimationTrigger> OnFinished;
    public bool IsFinished => finishTriggered;

    private Animator animator;
    private bool isPlaying;
    private bool finishTriggered;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        // 1) Billboard
        Vector3 dir = transform.position - targetCamera.transform.position;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // 2) Угол между forward камеры и направлением на спрайт
        float angle = Vector3.Angle(targetCamera.transform.forward, dir.normalized);

        // 3) Логика состояний
        if (!isPlaying && !finishTriggered)
        {
            // Камера заходит в конус — стартуем Playing
            if (angle <= viewAngleThreshold)
            {
                animator.SetTrigger(startTrigger);
                isPlaying = true;
            }
        }
        else if (isPlaying && !finishTriggered)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);

            // Если Playing закончилось — Finish
            if (info.IsName(playingStateName) && info.normalizedTime >= 1f)
            {
                animator.SetTrigger(finishTrigger);
                finishTriggered = true;
                OnFinished?.Invoke(this);
            }
            // Если во время Playing камера ушла из-за угла — Return → Idle
            else if (angle > viewAngleThreshold)
            {
                animator.ResetTrigger(startTrigger);      // отменяем старт
                animator.SetTrigger(returnTrigger);      // запускаем return-переход
                isPlaying = false;
            }
        }
        // После finishTriggered остаёмся в Finish и не возвращаемся в Idle
    }
}
