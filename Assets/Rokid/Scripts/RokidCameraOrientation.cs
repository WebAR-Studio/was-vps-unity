using UnityEngine;

public class RokidCameraOrientation : MonoBehaviour
{
    private const float TIMER_TIME = 0.5f;
    private readonly float _accelerometerRotationalAngleFactorXZ = -90f;

    [SerializeField] protected Transform _cameraMain;

    protected UnityEngine.Gyroscope _gyro;

    protected bool _isGyroSupported = false;
    protected bool _isRotationWithGyro = false;

    protected bool _isAccelerometerSupported = false;
    protected bool _isRotationWithAccelerometer = false;

    [SerializeField] private bool _useGyro = false;
    [SerializeField] private bool _useAccelerometer = true;

    [SerializeField] private bool _initializeOnStart = false;
    [SerializeField] private bool _isGyroDisabledOnDestroy = false;

    private float _timerValue;

    [Header("Accelerometer Settings")]
    [Tooltip("1f => no vibrations")]
    [Range(1f, 50f)]
    [SerializeField]
    private float _accelerometerSensitivityXZ = 5f;
    [Tooltip("if > 1f => use it for smooth motion")]
    [Range(0f, 5f)]
    [SerializeField]
    private float _accelerometerSmoothLimitXZ = 0.5f;
    [Range(0f, 2f)]
    [SerializeField] private float _accelerometerSensitivityY = 0.11f;
    [SerializeField] private float _accelerometerRotationalSpeedFactorY = 350f;

    private Vector3 _accelerometerCurrentRotationXZ;
    private Quaternion _accelerometerResultRotationXZ;
    private Vector3 _accelerometerDirNormalized;

    protected virtual void Start()
    {
        if (_initializeOnStart)
        {
#if !UNITY_EDITOR
            Init();
#endif
        }
    }

    public void Init()
    {
        _timerValue = TIMER_TIME;
        if (_useGyro)
        {
            _isGyroSupported = SystemInfo.supportsGyroscope;
        }
        if (_useAccelerometer)
        {
            _isAccelerometerSupported = SystemInfo.supportsAccelerometer;
        }

        if (_isGyroSupported)
        {
            _cameraMain.parent.transform.rotation = Quaternion.Euler(90f, 180f, 0f);

            _gyro = Input.gyro;
            _gyro.enabled = true;
            _isRotationWithGyro = true;
        }
        else
        {
            if (_isAccelerometerSupported)
            {
                _isRotationWithAccelerometer = true;
            }
        }
    }

#if !UNITY_EDITOR
    private void FixedUpdate() 
	{
		UpdateNotInEditor();
	}
#endif

    protected virtual void UpdateNotInEditor()
    {
        if (_isGyroSupported && _isRotationWithGyro)
        {
            if (_timerValue > 0)
            {
                GyroByValue();
                _timerValue -= Time.deltaTime;
            }
            else
            {
                GyroBySpeedAndValue();
            }
        }
        else if (_isAccelerometerSupported && _isRotationWithAccelerometer)
        {
            RotateYWithAccelerometer();
            RotateXZWithAccelerometer();
        }
    }

    private void GyroByValue()
    {
        Quaternion targetRotation = GyroToUnity(_gyro.attitude);
        _cameraMain.localRotation = Quaternion.Slerp(
            _cameraMain.localRotation,
            targetRotation,
            Time.deltaTime * 5f);
    }

    private void GyroBySpeedAndValue()
    {
        float y = _cameraMain.eulerAngles.y;
        _cameraMain.localRotation = GyroToUnity(_gyro.attitude);
        _cameraMain.eulerAngles = new Vector3(
            _cameraMain.eulerAngles.x,
            y + GyroToUnity(_gyro.rotationRate * 2f).y,
            _cameraMain.eulerAngles.z);
    }

    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    private static Vector3 GyroToUnity(Vector3 q)
    {
        return new Vector3(-q.x, -q.y, q.z);
    }

    protected void RotateYWithAccelerometer()
    {
        _accelerometerDirNormalized = Input.acceleration.normalized;

        if (_accelerometerDirNormalized.x >= _accelerometerSensitivityY
            || _accelerometerDirNormalized.x <= -_accelerometerSensitivityY)
        {
            _cameraMain.Rotate(
                0f,
                Input.acceleration.x
                    * _accelerometerRotationalSpeedFactorY * Time.deltaTime,
                0f);
        }
    }

    protected void RotateXZWithAccelerometer()
    {
        _accelerometerCurrentRotationXZ.y = _cameraMain.localEulerAngles.y;

        _accelerometerCurrentRotationXZ.x =
            Input.acceleration.z * _accelerometerRotationalAngleFactorXZ;

        _accelerometerCurrentRotationXZ.z =
            Input.acceleration.x * _accelerometerRotationalAngleFactorXZ;

        _accelerometerResultRotationXZ = Quaternion.Slerp(
            _cameraMain.localRotation,
            Quaternion.Euler(_accelerometerCurrentRotationXZ),
            _accelerometerSensitivityXZ * Time.deltaTime);

        if (Quaternion.Angle(_cameraMain.rotation, _accelerometerResultRotationXZ)
            > _accelerometerSmoothLimitXZ)
        {
            _cameraMain.localRotation = _accelerometerResultRotationXZ;
        }
        else
        {
            _cameraMain.localRotation = Quaternion.Slerp(
                _cameraMain.localRotation,
                Quaternion.Euler(_accelerometerCurrentRotationXZ),
                Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        if (_isGyroDisabledOnDestroy)
        {
            if (_gyro != null)
            {
                _gyro.enabled = false;
            }
        }
    }
}
