using System.Collections;
using DG.Tweening;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CustomLocomotionTeleporter : MonoBehaviour
{
    [SerializeField] private InputActionProperty _teleportMoveAction;

    [SerializeField] private LineRenderer _line;
    [SerializeField] private Gradient _canTeleportColor;
    [SerializeField] private Gradient _noTeleportColor;
    [SerializeField] private float _step;
    [SerializeField] private LayerMask _layerMask;

    [SerializeField] private Transform _teleportBody;
    [SerializeField] private Transform _teleportPrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField, Range(0.01f, 100f)] private float _maxHeight = 1f;

    [SerializeField] private AudioClip _teleportAudioClip;
    private AudioSource _effectsAudioSource;

    [Header("Fade and Time")]
    [SerializeField] private Image _fadeImage;
    [SerializeField] private float _fadeInTime;
    [SerializeField] private float _fadeOutTime;
    [SerializeField] private float _waitAfterTeleportTime;

    private bool _isTeleporting;
    private bool _isTeleportActionPressed;
    
    private bool _validTeleport;
    private Vector3 _validTeleportLocation;

    private Camera _playerCamera;
    private XROrigin _xrOrigin;

    private GameObject _teleportModel;

    private void Start()
    {
        _playerCamera = Camera.main;
        _xrOrigin = FindObjectOfType<XROrigin>();

        if (_teleportPrefab != null)
        {
            _teleportModel = Instantiate(_teleportPrefab.gameObject);
        }
    }

    private void Update()
    {
        bool teleportMovePressed = _teleportMoveAction.action.IsPressed();

        _line.colorGradient = _isTeleporting ? _noTeleportColor : _canTeleportColor;

        if (teleportMovePressed)
        {
            if (!_isTeleportActionPressed)
            {
                ShootTeleport();
            }
            else
            {
                SetTeleportVisible(false);
            }
        }
        else
        {
            if (_isTeleportActionPressed)
            {
                _isTeleportActionPressed = false;
            }
            else if (!_isTeleporting && !_isTeleportActionPressed && _validTeleport)
            {
                _validTeleport = false;
                _isTeleportActionPressed = true;

                StartCoroutine(Teleport(_validTeleportLocation, _waitAfterTeleportTime, _fadeInTime, _fadeOutTime));
            }

            SetTeleportVisible(false);
        }
    }

    private void ShootTeleport()
    {
        Ray ray = new Ray(_firePoint.position, _firePoint.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && 1 << hit.collider.gameObject.layer == _layerMask)
        {
            SetTeleportVisible(true);
            
            Vector3 direction = hit.point - _firePoint.position;
            Vector3 groundDirection = new Vector3(direction.x, 0, direction.z);
            Vector3 targetPosition = new Vector3(groundDirection.magnitude, direction.y, 0);

            float height = targetPosition.y + targetPosition.magnitude / 2f;
            height = Mathf.Clamp(height, 0.01f, _maxHeight);

            float v0;
            float time;
            float angle;
            CalculateProjectilePath(targetPosition, height, out v0, out angle, out time);

            if (_line != null)
                DrawPath(groundDirection.normalized, v0, angle, time, _step);
            if (_teleportModel != null)
                _teleportModel.transform.position = hit.point;

            _validTeleport = !_isTeleporting && !_isTeleportActionPressed;
            _validTeleportLocation = hit.point;

            if (!_isTeleporting && !_isTeleportActionPressed)
            {
                _validTeleport = true;
                _validTeleportLocation = hit.point;
            }
        }
        else
        {
            SetTeleportVisible(false);
            _validTeleport = false;
        }
    }

    private IEnumerator Teleport(Vector3 position, float delay, float fadeInTime, float fadeOutTime)
    {
        _isTeleporting = true;

        if (_teleportAudioClip != null)
        {
            if (_effectsAudioSource == null)
            {
                _effectsAudioSource = gameObject.AddComponent<AudioSource>();
                _effectsAudioSource.loop = false;
                _effectsAudioSource.playOnAwake = false;
            }
            _effectsAudioSource.PlayOneShot(_teleportAudioClip);
        }

        var rot = Matrix4x4.Rotate(_playerCamera.transform.rotation);
        var delta = rot.MultiplyPoint3x4(_playerCamera.transform.InverseTransformPoint(_xrOrigin.transform.position));

        if (_fadeImage != null)
        {
            var tween = _fadeImage.DOFade(1f, _fadeInTime)
                .OnComplete(() =>
                {
                    _teleportBody.position = position + delta + Vector3.up * _xrOrigin.CameraInOriginSpaceHeight;
                });

            yield return tween.WaitForCompletion();
            tween = _fadeImage.DOFade(0f, _fadeOutTime);
            yield return tween.WaitForCompletion();
        }
        else
        {
            _teleportBody.position = position + delta + Vector3.up * _xrOrigin.CameraInOriginSpaceHeight;
        }

        yield return new WaitForSeconds(delay);

        _isTeleporting = false;
    }

    private void CalculateProjectilePath(Vector3 targetPosition, float height, out float v0, out float angle, out float time)
    {
        float xt = targetPosition.x;
        float yt = targetPosition.y;
        float gravity = -Physics.gravity.y;

        float a = (-0.5f * gravity);
        float b = Mathf.Sqrt(2 * gravity * height);
        float c = -yt;

        float tPlus = QuadraticEquation(a, b, c, 1);
        float tMinus = QuadraticEquation(a, b, c, -1);
        
        time = tPlus > tMinus ? tPlus : tMinus;
        angle = Mathf.Atan(b * time / xt);
        v0 = b / Mathf.Sin(angle);
    }

    private float QuadraticEquation(float a, float b, float c, float sign)
    {
        return (-b + sign * Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
    }

    private void DrawPath(Vector3 direction, float v0, float angle, float time, float step)
    {
        step = Mathf.Max(0.01f, step);
        _line.positionCount = (int) (time / step) + 2;

        int count = 0;
        Vector2 displacement;

        for (float i = 0; i < time; i += step)
        {
            displacement = ProjectileDisplacement(v0, i, angle);
            
            _line.SetPosition(count, _firePoint.position + direction * displacement.x + Vector3.up * displacement.y);
            count++;
        }

        displacement = ProjectileDisplacement(v0, time, angle);
        _line.SetPosition(count, _firePoint.position + direction * displacement.x + Vector3.up * displacement.y);
    }
    private Vector2 ProjectileDisplacement(float v0, float t, float angle)
    {
        float x = v0 * t * Mathf.Cos(angle);
        float y = v0 * t * Mathf.Sin(angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(t, 2);
        return new Vector3(x, y);
    }

    private void SetTeleportVisible(bool visible)
    {
        _line.gameObject.SetActive(visible);
        _teleportModel.SetActive(visible);
    }
}
