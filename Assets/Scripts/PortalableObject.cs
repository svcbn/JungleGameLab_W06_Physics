using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PortalableObject : MonoBehaviour
{
    
    private GameObject cloneObject;

    private int inPortalCount = 0;
    
    private Portal inPortal;
    private Portal outPortal;

    protected new Rigidbody rigidbody;
    protected new Collider collider;

    // y 축 기준 180도 회전용 쿼터니언
    private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

    protected virtual void Awake()
    {
        // 클론오브젝트는 이 포탈가능오브젝트의 요소를 거의 다 들고있음
        cloneObject = new GameObject();
        cloneObject.SetActive(false);
        var meshFilter = cloneObject.AddComponent<MeshFilter>();
        var meshRenderer = cloneObject.AddComponent<MeshRenderer>();

        meshFilter.mesh =this.GetComponent<MeshFilter>().mesh;
        meshRenderer.materials = this.GetComponent<MeshRenderer>().materials;
        cloneObject.transform.localScale = transform.localScale;

        rigidbody = this.GetComponent<Rigidbody>();
        collider = this.GetComponent<Collider>();
    }

    private void LateUpdate()
    {
        // 포탈 진입도, 탈출도 안하면 안씀
        if(inPortal == null || outPortal == null)
        {
            return;
        }

        // 포탈 두개가 다 설치되어있고 클론오브젝트가 켜지면
        // 근데 지금 켜주는데가 없음
        if(cloneObject.activeSelf && inPortal.IsPlaced && outPortal.IsPlaced)
        {
            // 포탈의 트랜스폼 정보 받아와서
            var inTransform = inPortal.transform;
            var outTransform = outPortal.transform;

            // 포탈 기준으로 현재 위치에 대해 뒤집힌 위치로 이동시켜줌
            Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
            relativePos = halfTurn * relativePos;
            cloneObject.transform.position = outTransform.TransformPoint(relativePos);

            // 회전각도 마찬가지
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * transform.rotation;
            relativeRot = halfTurn * relativeRot;
            cloneObject.transform.rotation = outTransform.rotation * relativeRot;
        }
        // 아니면 그냥 눈에 안보이게 멀리 치워버림
        else
        {
            cloneObject.transform.position = new Vector3(-1000.0f, 1000.0f, -1000.0f);
        }
    }

    // 포탈 가능 물체가 포탈 콜라이더에 검출되었을 때
    public void SetIsInPortal(Portal inPortal, Portal outPortal, Collider wallCollider)
    {
        // 들어가는 포탈과 나가는 포탈 설정
        this.inPortal = inPortal;
        this.outPortal = outPortal;

        // 벽과 충돌 잠시 무시해줌
        Physics.IgnoreCollision(collider, wallCollider);

        // 복제 물체 꺼줌
        cloneObject.SetActive(false);

        // 포탈 접촉 물체 체크용. 근데 왜 bool아님??? 굳이 int로 한 이유가 있나
        ++inPortalCount;
    }

    // 포탈 가능 물체가 포탈 콜라이더에서 검출 종료되었을 때
    public void ExitPortal(Collider wallCollider)
    {
        // 벽과 충돌 다시 돌려줌
        Physics.IgnoreCollision(collider, wallCollider, false);

        // 포탈 물체 개수 감소
        --inPortalCount;

        // 포탈 통과중인 물체 없으면
        if (inPortalCount == 0)
        {
            // 복제 물체 꺼줌
            cloneObject.SetActive(false);
        }
    }

    // 워프는 이쪽으로
    public virtual void Warp()
    {
        var inTransform = inPortal.transform;
        var outTransform = outPortal.transform;
         
        // 현재 물체의 위치 포탈 기준으로 월드좌표 변환 후 뒤집어서 바꿔줌
        Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
        relativePos = halfTurn * relativePos;
        transform.position = outTransform.TransformPoint(relativePos);

        // 회전각 또한 마찬가지
        Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * transform.rotation;
        relativeRot = halfTurn * relativeRot;
        transform.rotation = outTransform.rotation * relativeRot;

        // 진입 속도 그대로 돌려주기 
        Vector3 relativeVel = inTransform.InverseTransformDirection(rigidbody.velocity);
        relativeVel = halfTurn * relativeVel;
        rigidbody.velocity = outTransform.TransformDirection(relativeVel);

        // 재진입을 대비해 인-아웃 바꿔주기
        var tmp = inPortal;
        inPortal = outPortal;
        outPortal = tmp;
    }
}
