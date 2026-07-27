using Spine.Unity;
using System.Collections;
using UnityEngine;


public class FxRooted : BaseFx
{
    public SkeletonAnimation[] skeletonAnimations;
    public string nameAnim = "Roots";

    public override void Active(Vector3 position, float scale = 1f)
    {
        base.Active(position, scale);
        skeletonAnimations[0].AnimationState.SetAnimation(0, nameAnim, false);
        skeletonAnimations[1].AnimationState.SetAnimation(0, nameAnim, false);
    }
}
