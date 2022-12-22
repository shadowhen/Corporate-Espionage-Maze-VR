using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class CustomLocomotionSystem : MonoBehaviour
{
    [InspectorName("XR Origin")] public XROrigin XrOrigin;

    [SerializeField] private float _delayTime;

    [Header("Audio")]
    public AudioClip TeleportAudioClip;
    private AudioSource _teleportAudioSource;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
    }

    private void Start()
    {
        if (XrOrigin == null)
            XrOrigin = FindObjectOfType<XROrigin>();

    }

    public void Teleport(Vector3 worldLocation)
    {
        if (_isBusy)
            return;
        StartCoroutine(Teleport_Coroutine(worldLocation));
    }

    private IEnumerator Teleport_Coroutine(Vector3 worldLocation)
    {
        if (XrOrigin == null)
            yield return null;

        _isBusy = true;

        PlaySoundEffect();
        XrOrigin.transform.position = CalculateTeleportPosition(worldLocation);

        yield return new WaitForSeconds(_delayTime);

        _isBusy = false;
    }

    private void PlaySoundEffect()
    {
        if (TeleportAudioClip == null)
            return;

        if (_teleportAudioSource == null)
        {
            _teleportAudioSource = gameObject.AddComponent<AudioSource>();
            _teleportAudioSource.playOnAwake = false;
            _teleportAudioSource.loop = false;
        }
        _teleportAudioSource.PlayOneShot(TeleportAudioClip);
    }

    private Vector3 CalculateTeleportPosition(Vector3 worldLocation)
    {
        var camera = XrOrigin.Camera;

        var rot = Matrix4x4.Rotate(camera.transform.rotation);
        var delta = rot.MultiplyPoint3x4(camera.transform.InverseTransformPoint(XrOrigin.transform.position));

        return worldLocation + delta + Vector3.up * XrOrigin.CameraInOriginSpaceHeight;
    }
}
