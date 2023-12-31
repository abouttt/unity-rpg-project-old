using UnityEngine;
using Cinemachine;

public class PlayerCameraController : MonoBehaviour
{
    public static readonly string SaveKey = "SaveCamera";

    public Transform LockOnTarget
    {
        get => _targetCamera.LookAt;
        set
        {
            _targetCamera.LookAt = value;
            IsLockOn = value != null;
            Player.Animator.SetFloat(_animIDLockOn, IsLockOn ? 1f : 0f);
            _stateDrivenCameraAnimator.SetBool(_animIDLockOn, IsLockOn);
            _lockOnTargetImageFollowTarget.SetTarget(_targetCamera.LookAt);
        }
    }

    public bool IsLockOn { get; private set; } = false;

    [Header("[Rotate]")]
    [SerializeField]
    private GameObject _cinemachineCameraTarget;
    [SerializeField]
    private float _sensitivity;
    [SerializeField]
    private float _topClamp;
    [SerializeField]
    private float _bottomClamp;

    [Space(10)]
    [Header("[Lock On]")]
    [SerializeField]
    private float _radius;
    [SerializeField, Range(0, 360)]
    private float _angle;
    [SerializeField]
    private LayerMask _targetMask;
    [SerializeField]
    private LayerMask _obstructionMask;
    [SerializeField]
    private Animator _stateDrivenCameraAnimator;
    [SerializeField]
    private CinemachineVirtualCamera _followCamera;
    [SerializeField]
    private CinemachineVirtualCamera _targetCamera;
    private UI_FollowTarget _lockOnTargetImageFollowTarget;

    private readonly Collider[] _lockTargets = new Collider[10];
    private readonly int _animIDLockOn = Animator.StringToHash("LockOn");

    private float _cinemachineTargetPitch;  // X
    private float _cinemachineTargetYaw;    // Y

    private void Awake()
    {
        LoadSaveData();
    }

    private void Start()
    {
        _lockOnTargetImageFollowTarget = Managers.UI.Get<UI_AutoCanvas>().LockOnTargetImage.GetComponent<UI_FollowTarget>();
    }

    private void Update()
    {
        if (Managers.Input.LockOn)
        {
            if (IsLockOn)
            {
                LockOnTarget = null;
            }
            else
            {
                FindTarget();
            }
        }
    }

    private void LateUpdate()
    {
        if (IsLockOn)
        {
            TrackingTarget();
        }

        CameraRotation();
    }

    public string GetSaveData()
    {
        CameraSaveData saveData = new()
        {
            Pitch = _cinemachineTargetPitch,
            Yaw = _cinemachineTargetYaw,
        };

        return JsonUtility.ToJson(saveData);
    }

    private void CameraRotation()
    {
        if (IsLockOn)
        {
            _cinemachineCameraTarget.transform.rotation = _followCamera.transform.rotation;
            var eulerAngles = _cinemachineCameraTarget.transform.eulerAngles;
            _cinemachineTargetPitch = eulerAngles.x;
            _cinemachineTargetYaw = eulerAngles.y;
        }
        else
        {
            if (Managers.Input.Look.sqrMagnitude >= 0.01f)
            {
                _cinemachineTargetYaw += Managers.Input.Look.x * _sensitivity;
                _cinemachineTargetPitch += Managers.Input.Look.y * _sensitivity;
            }

            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, _bottomClamp, _topClamp);
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f);
        }
    }

    private void FindTarget()
    {
        float shortestAngle = Mathf.Infinity;
        Transform finalTarget = null;

        int targetCnt = Physics.OverlapSphereNonAlloc(_cinemachineCameraTarget.transform.position, _radius, _lockTargets, _targetMask);
        for (int i = 0; i < targetCnt; i++)
        {
            var directionToTarget = (_lockTargets[i].transform.position - _cinemachineCameraTarget.transform.position).normalized;
            var currentAngle = Vector3.Angle(_cinemachineCameraTarget.transform.forward, directionToTarget);
            if (currentAngle < _angle * 0.5f)
            {
                float distanceToTarget = Vector3.Distance(_cinemachineCameraTarget.transform.position, _lockTargets[i].transform.position);
                if (currentAngle < shortestAngle)
                {
                    if (!Physics.Raycast(_cinemachineCameraTarget.transform.position, directionToTarget, distanceToTarget, _obstructionMask))
                    {
                        finalTarget = _lockTargets[i].transform;
                        shortestAngle = currentAngle;
                    }
                }
            }
        }

        LockOnTarget = finalTarget;
    }

    private void TrackingTarget()
    {
        if (!LockOnTarget.gameObject.activeSelf ||
            Vector3.Distance(_cinemachineCameraTarget.transform.position, LockOnTarget.transform.position) > _radius)
        {
            LockOnTarget = null;
        }
    }

    private float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle > 180)
        {
            return lfAngle - 360f;
        }
        else if (lfAngle < -180)
        {
            return lfAngle + 360f;
        }

        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void LoadSaveData()
    {
        if (Managers.Data.Load<string>(SaveKey, out var json) && !Managers.Game.IsPortalSpawnPosition)
        {
            var saveData = JsonUtility.FromJson<CameraSaveData>(json);
            _cinemachineTargetPitch = saveData.Pitch;
            _cinemachineTargetYaw = saveData.Yaw;
        }
        else
        {
            _cinemachineTargetYaw = _cinemachineCameraTarget.transform.rotation.eulerAngles.y;
        }
    }
}
