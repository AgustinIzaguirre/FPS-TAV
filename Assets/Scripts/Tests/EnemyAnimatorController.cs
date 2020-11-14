using UnityEngine;

public class EnemyAnimatorController
{
    private Animator enemyAnimator;
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsShooting = Animator.StringToHash("isShooting");
    private static readonly int IsDead = Animator.StringToHash("isDead");

    public EnemyAnimatorController(Animator enemyAnimator)
    {
        this.enemyAnimator = enemyAnimator;
    }
    public void Shoot()
    {
        enemyAnimator.SetBool(IsShooting, true);
        enemyAnimator.SetBool(IsMoving, false);
        enemyAnimator.SetBool(IsDead, false);
    }

    public void Idle()
    {
        enemyAnimator.SetBool(IsMoving, false);
        enemyAnimator.SetBool(IsShooting, false);
        enemyAnimator.SetBool(IsDead, false);
    }

    public void Kill()
    {
        enemyAnimator.SetBool(IsDead, true);
        enemyAnimator.SetBool(IsMoving, false);
        enemyAnimator.SetBool(IsShooting, false);
    }

    public void Move()
    {
        enemyAnimator.SetBool(IsMoving, true);
        enemyAnimator.SetBool(IsShooting, false);
        enemyAnimator.SetBool(IsDead, false);
    }

    public void ApplyAnimation(AnimationStates animationState)
    {
        if (animationState == AnimationStates.MOVE)
        {
            Move();
        }
        else if (animationState == AnimationStates.SHOOT)
        {
            Shoot();
        }
        else
        {
            Idle();
        }
    }
}
