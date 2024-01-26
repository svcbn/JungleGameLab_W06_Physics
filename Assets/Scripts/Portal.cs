using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Portal : MonoBehaviour
{
    // 포탈끼리는 서로 다른 포탈을 알고있음
    [field: SerializeField]
    public Portal OtherPortal { get; private set; }

    // 포탈 색 변경용
    [SerializeField]
    private Renderer outlineRenderer;

    // 포탈 색,에디터에서 변경가능
    [field: SerializeField]
    public Color PortalColour { get; private set; }

    // 레이어마스크, 에디터에서 여러개 고를수있음
    [SerializeField]
    private LayerMask placementMask;

    // 포탈 자신의 트랜스폼
    [SerializeField]
    private Transform portalTransform;

    // 포탈에 통과되려면 PortalableObject 를 들고있어야함, 이 리스트에 추가됨.
    private List<PortalableObject> portalObjects = new List<PortalableObject>();
   
    // 포탈 부착상태 확인
    public bool IsPlaced { get; private set; } = false;

    // 포탈 부착상태일때의 벽 콜라이더 담아놓기 위한 전역변수
    private Collider wallCollider;

    // 포탈 표면 포탈 카메라에서 바꿔줌
    public Renderer Renderer { get; private set; }

    // 포탈 접촉 처리
    private new BoxCollider collider;

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
        Renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        outlineRenderer.material.SetColor("_OutlineColour", PortalColour);
        
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // 포탈 설치되면 카메라 작동
        Renderer.enabled = OtherPortal.IsPlaced;

        for (int i = 0; i < portalObjects.Count; ++i)
        {
            // 포탈 통과 가능 모든 오브젝트들의 상대좌표 추적
            Vector3 objPos = transform.InverseTransformPoint(portalObjects[i].transform.position);

            // 포탈과 같아지면 워프
            if (objPos.z > 0.0f)
            {
                portalObjects[i].Warp();
            }
        }

        // 포탈 초기화
        if (Input.GetKeyDown(KeyCode.R))
        {
            RemovePortal();
        }
    }

    // 포탈에 콜라이더 검출되었을때
    private void OnTriggerEnter(Collider other)
    {
        // 포탈가능물체 검출 시도
        var obj = other.GetComponent<PortalableObject>();
        
        // 포탈가능물체인지 확인
        if (obj != null)
        {
            // 검사 리스트에 추가
            portalObjects.Add(obj);

            // 포탈 물체에서 벽콜라이더 꺼주고 포탈진입 처리 해주고
            obj.SetIsInPortal(this, OtherPortal, wallCollider);
        }
    }

    // 포탈에서 콜라이더 검출 끝났을때
    private void OnTriggerExit(Collider other)
    {
        // 포탈가능물체 검출 시도
        var obj = other.GetComponent<PortalableObject>();

        // 리스트에서 동일 물체 검사
        if(portalObjects.Contains(obj))
        {
            // 리스트에서 제거
            portalObjects.Remove(obj);

            // 벽콜라이더 켜주고 포탈 탈출 처리
            obj.ExitPortal(wallCollider);
        }
    }

    // 포탈 설치될때
    public bool PlacePortal(Collider wallCollider, Vector3 pos, Quaternion rot)
    {
        // 레이캐스트 맞은 지점의 위치
        portalTransform.position = pos;

        // 항상 포탈의 위와 앞으로 설정
        portalTransform.rotation = rot;

        // 하지만 살짝 앞으로 빼서 겹치는건 방지. 겹치면 렌더링 깨짐
        portalTransform.position -= portalTransform.forward * 0.001f;

        // 포탈이 다른 무언가랑 겹치지 않게
        FixOverhangs();

        // 포탈이 벽 경계와 겹치지 않게
        FixIntersects();

        // 벽들과 밀착 확인
        if (CheckOverlap())
        {
            // 벽콜라이더 설정
            this.wallCollider = wallCollider;

            // 위치, 방향 설정
            transform.position = portalTransform.position;
            transform.rotation = portalTransform.rotation;

            // 포탈 active 해주고 설치bool 갱신
            gameObject.SetActive(true);
            IsPlaced = true;
            return true;
        }

        // 위에서 안걸렸으면 false
        return false;
    }

    // 포탈이 다른 무엇인가랑 겹쳐서 생성되지 않도록 보정
    private void FixOverhangs()
    {
        // 우좌상하 검사
        var testPoints = new List<Vector3>
        {
            new Vector3(-1.1f,  0.0f, 0.1f),
            new Vector3( 1.1f,  0.0f, 0.1f),
            new Vector3( 0.0f, -2.1f, 0.1f),
            new Vector3( 0.0f,  2.1f, 0.1f)
        };
        
        var testDirs = new List<Vector3>
        {
             Vector3.right,
            -Vector3.right,
             Vector3.up,
            -Vector3.up
        };

        // 4방향에 대해 검사
        for(int i = 0; i < 4; ++i)
        {
            RaycastHit hit;

            // 각 방향별로 레이캐스트 할 위치랑 방향 설정
            Vector3 raycastPos = portalTransform.TransformPoint(testPoints[i]);
            Vector3 raycastDir = portalTransform.TransformDirection(testDirs[i]);

            // 시작방향으로부터 0.05 이내 레이어마스크 할당한 것이 있다면 안해도됨
            // (벽 모서리거나 할 일이 없으니까)
            if(Physics.CheckSphere(raycastPos, 0.05f, placementMask))
            {
                break;
            }
            // 세로 최대길이인 2.1f만큼 쏴보고
            else if(Physics.Raycast(raycastPos, raycastDir, out hit, 2.1f, placementMask))
            {
                // 맞은 위치랑 쏜지점 벡터 계산해서 오프셋 설정
                var offset = hit.point - raycastPos;

                // 오프셋만큼 밀어주기
                // 사실 세로기준 2.1f 라서 가로는 1.1f 로 해줄법한테 그냥 퉁친걸로 보임
                portalTransform.Translate(offset, Space.World);
            }
        }
    }

    // 포탈이 벽 경계를 넘지 않도록 보정
    private void FixIntersects()
    {
        // 우좌상하 검사
        var testDirs = new List<Vector3>
        {
             Vector3.right,
            -Vector3.right,
             Vector3.up,
            -Vector3.up
        };

        var testDists = new List<float> { 1.1f, 1.1f, 2.1f, 2.1f };

        // 4방향에 대해서 검사
        for (int i = 0; i < 4; ++i)
        {
            RaycastHit hit;
            Vector3 raycastPos = portalTransform.TransformPoint(0.0f, 0.0f, -0.1f);
            Vector3 raycastDir = portalTransform.TransformDirection(testDirs[i]);

            // 가로 1.1f만큼, 세로 2.1f 만큼 검사
            if (Physics.Raycast(raycastPos, raycastDir, out hit, testDists[i], placementMask))
            {
                // 검출 지점이 있으면 오프셋 설정
                var offset = (hit.point - raycastPos);

                // 설정한 오프셋과 레이 길이로 레이캐스트 반대방향으로 변경위치 설정
                var newOffset = -raycastDir * (testDists[i] - offset.magnitude);

                // 오프셋 이동
                portalTransform.Translate(newOffset, Space.World);
            }
        }
    }

    // 포탈이 한번 설치되고 나면 다른 설치에 방해받지 않도록
    private bool CheckOverlap()
    {
        // 체크할 범위
        var checkExtents = new Vector3(0.9f, 1.9f, 0.05f);

        // 체크할 방향들. 앞,뒤 + 모서리 4군데 기준
        var checkPositions = new Vector3[]
        {
            portalTransform.position + portalTransform.TransformVector(new Vector3( 0.0f,  0.0f, -0.1f)),

            portalTransform.position + portalTransform.TransformVector(new Vector3(-1.0f, -2.0f, -0.1f)),
            portalTransform.position + portalTransform.TransformVector(new Vector3(-1.0f,  2.0f, -0.1f)),
            portalTransform.position + portalTransform.TransformVector(new Vector3( 1.0f, -2.0f, -0.1f)),
            portalTransform.position + portalTransform.TransformVector(new Vector3( 1.0f,  2.0f, -0.1f)),

            portalTransform.TransformVector(new Vector3(0.0f, 0.0f, 0.2f))
        };

        // 벽이나 다른 포탈 겹치는지 육면체 범위로 판정
        var intersections = Physics.OverlapBox(checkPositions[0], checkExtents, portalTransform.rotation, placementMask);

        // 겹치는게 여러개 있다면
        if(intersections.Length > 1)
        {
            // false 반환
            return false;
        }
        // 겹치는게 한개만 있으면
        else if(intersections.Length == 1) 
        {
            // 그게 이미 설치된 이 포탈이 아니라면
            if (intersections[0] != this.collider)
            {
                // false 반환
                return false;
            }
        }

        // 포탈 모서리가 벽에 밀착되었나 확인
        bool isOverlapping = true;

        // 벽하고 밀착되었나 확인
        for(int i = 1; i < checkPositions.Length - 1; ++i)
        {
            isOverlapping &= Physics.Linecast(checkPositions[i], 
                checkPositions[i] + checkPositions[checkPositions.Length - 1], placementMask);
        }

        return isOverlapping;
    }

    // 설치된 포탈 제거
    public void RemovePortal()
    {
        gameObject.SetActive(false);
        IsPlaced = false;
    }
}
