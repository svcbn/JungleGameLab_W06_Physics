using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : PortalableObject
{
    private CameraMove cameraMove;

    protected override void Awake()
    {
        base.Awake();

        cameraMove = GetComponentInChildren<CameraMove>();
    }

    public override void Warp()
    {
        base.Warp();

        // 카메라 상하 초기화
        cameraMove.ResetTargetRotation();
    }
}
