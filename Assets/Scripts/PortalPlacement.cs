using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CameraMove))]
public class PortalPlacement : MonoBehaviour
{
    [SerializeField]
    private PortalPair portals;

    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private Crosshair crosshair;

    private CameraMove cameraMove;

    private void Awake()
    {
        cameraMove = GetComponent<CameraMove>();
    }

    private void Update()
    {
        // 좌클릭
        if(Input.GetButtonDown("Fire1"))
        {
            FirePortal(0, transform.position, transform.forward, 250.0f);
        }
        // 우클릭
        else if (Input.GetButtonDown("Fire2"))
        {
            FirePortal(1, transform.position, transform.forward, 250.0f);
        }
    }

    // 포탈 발사
    private void FirePortal(int portalID, Vector3 pos, Vector3 dir, float distance)
    {
        RaycastHit hit;
        Physics.Raycast(pos, dir, out hit, distance, layerMask);

        if(hit.collider != null)
        {
            // 다른 포탈에 또 포탈 설치 시도 발사할 때, 포탈 너머에 발사해줌
            if (hit.collider.tag == "Portal")
            {
                var inPortal = hit.collider.GetComponent<Portal>();
                
                if(inPortal == null)
                {
                    return;
                }

                var outPortal = inPortal.OtherPortal;

                // 인포탈쪽에서 발사한 레이캐스트 아웃포탈쪽으로 빼줌
                Vector3 relativePos = inPortal.transform.InverseTransformPoint(hit.point + dir);
                relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
                pos = outPortal.transform.TransformPoint(relativePos);

                Vector3 relativeDir = inPortal.transform.InverseTransformDirection(dir);
                relativeDir = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeDir;
                dir = outPortal.transform.TransformDirection(relativeDir);

                distance -= Vector3.Distance(pos, hit.point);

                // 새로 갱신받은 위치로 다시쏴줌
                FirePortal(portalID, pos, dir, distance);

                return;
            }

            // 아웃포탈쪽에서 지면과 평행하게 회전 맞춰줌
            var cameraRotation = cameraMove.TargetRotation;
            var portalRight = cameraRotation * Vector3.right;
            
            // 방향 구분해서 +, - 구분
            if(Mathf.Abs(portalRight.x) >= Mathf.Abs(portalRight.z))
            {
                portalRight = (portalRight.x >= 0) ? Vector3.right : -Vector3.right;
            }
            else
            {
                portalRight = (portalRight.z >= 0) ? Vector3.forward : -Vector3.forward;
            }

            // 포탈면의 법선벡터가 포탈의 앞
            var portalForward = -hit.normal;
            // 오른쪽과 앞쪽 외적으로 위쪽 벡터 계산
            var portalUp = -Vector3.Cross(portalRight, portalForward);

            // 정면으로 돌려주기
            var portalRotation = Quaternion.LookRotation(portalForward, portalUp);
            
            // 포탈 부착 시도
            bool wasPlaced = portals.Portals[portalID].PlacePortal(hit.collider, hit.point, portalRotation);

            // ui 반영
            if(wasPlaced)
            {
                crosshair.SetPortalPlaced(portalID, true);
            }
        }
    }
}
