using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

public class CustomTeleporter : MonoBehaviour
{
    public CustomLocomotionSystem LocomotionSystem;

    public InputActionProperty TeleportAction;
    private bool _teleportActionPressed;

    public InputActionProperty ToggleTeleportAction;
    public bool Toggleable = false;
    private bool _togglePressed;
    private bool _showTeleport;

    public LayerMask TeleportMask;
    public float Step = 0.01f;
    public float MaxHeight = 0.5f;

    public LineRenderer TeleportLine;
    public Gradient TeleportAllowedGradient;
    public Gradient TeleportWaitGradient;

    public Transform FirePoint;

    private bool _validTeleport;
    private Vector3 _validTeleportLocation;

    [SerializeField] private Transform _teleportBody;
    public Transform TeleportBody
    {
        get => _teleportBody;
        set
        {
            _teleportBody = value;
            if (_teleportBody != null)
            {
                _teleportBodyMaterial = _teleportBody.GetComponentInChildren<MeshRenderer>().material;
            }
            else
            {
                _teleportBodyMaterial = null;
            }
        }
    }
    private Material _teleportBodyMaterial;

    private void Start()
    {
        if (_teleportBody != null)
        {
            _teleportBodyMaterial = _teleportBody.GetComponentInChildren<MeshRenderer>().material;
        }
    }

    private void Update()
    {
        if (Toggleable)
        {
            HandleInputToggleable();
        }
        else
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        if (TeleportAction.action.IsPressed())
        {
            Ray ray = new Ray(FirePoint.position, FirePoint.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && 1 << hit.collider.gameObject.layer == TeleportMask)
            {

                _validTeleport = true;
                _validTeleportLocation = hit.point;

                DrawPathLine(hit);
                UpdateTeleportBody(hit.point);
                UpdateTeleportColor();
                TeleportLine.colorGradient = !LocomotionSystem.IsBusy ? TeleportAllowedGradient : TeleportWaitGradient;
                SetTeleportVisible(true);
            }
            else
            {
                _validTeleport = false;
                SetTeleportVisible(false);
            }
        }
        else
        {
            if (!LocomotionSystem.IsBusy && _validTeleport && !TeleportAction.action.IsPressed())
            {
                LocomotionSystem.Teleport(_validTeleportLocation);
            }

            _validTeleport = false;
            SetTeleportVisible(false);
        }
    }

    private void HandleInputToggleable()
    {
        bool showTeleport = _showTeleport;
        
        HandleToggleTeleport();
        if (!TeleportAction.action.IsPressed() && _teleportActionPressed)
        {
            _teleportActionPressed = false;
        }

        if (_showTeleport)
        {
            Ray ray = new Ray(FirePoint.position, FirePoint.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && 1 << hit.collider.gameObject.layer == TeleportMask)
            {
                DrawPathLine(hit);
                UpdateTeleportBody(hit.point);
                UpdateTeleportColor();
                TeleportLine.colorGradient = !LocomotionSystem.IsBusy ? TeleportAllowedGradient : TeleportWaitGradient;

                if (TeleportAction.action.IsPressed() && !LocomotionSystem.IsBusy && !_teleportActionPressed)
                {
                    LocomotionSystem.Teleport(hit.point);
                    _teleportActionPressed = true;
                }
            }
            else
            {
                showTeleport = false;
            }
        }
        SetTeleportVisible(showTeleport);
    }

    private void HandleToggleTeleport()
    {
        if (_togglePressed && !ToggleTeleportAction.action.IsPressed())
        {
            _togglePressed = false;
        }
        else if (!_togglePressed && ToggleTeleportAction.action.IsPressed())
        {
            _togglePressed = true;
            _showTeleport = !_showTeleport;
        }
    }

    private void DrawPathLine(RaycastHit hit)
    {
        Vector3 direction = hit.point - FirePoint.position;
        Vector3 groundDirection = new Vector3(direction.x, 0, direction.z);
        Vector3 targetPosition = new Vector3(groundDirection.magnitude, direction.y, 0);

        float height = targetPosition.y + targetPosition.magnitude / 2f;
        height = Mathf.Clamp(height, 0.01f, MaxHeight);

        float v0;
        float time;
        float angle;
        GMath.CalculateProjectilePath(targetPosition, height, out v0, out angle, out time);

        DrawPath(groundDirection.normalized, v0, angle, time, Step);
    }

    private void DrawPath(Vector3 direction, float v0, float angle, float time, float step)
    {
        step = Mathf.Max(0.01f, step);
        TeleportLine.positionCount = (int)(time / step) + 2;

        int count = 0;
        Vector2 displacement;

        for (float i = 0; i < time; i += step)
        {
            displacement = GMath.ProjectileDisplacement(v0, i, angle);

            TeleportLine.SetPosition(count, FirePoint.position + direction * displacement.x + Vector3.up * displacement.y);
            count++;

            if (count > TeleportLine.positionCount)
            {
                break;
            }
        }

        displacement = GMath.ProjectileDisplacement(v0, time, angle);
        TeleportLine.SetPosition(count, FirePoint.position + direction * displacement.x + Vector3.up * displacement.y);
    }

    private void SetTeleportVisible(bool visible)
    {
        TeleportLine.gameObject.SetActive(visible);
        if (TeleportBody != null)
            TeleportBody.gameObject.SetActive(visible);
    }

    private void UpdateTeleportBody(Vector3 position)
    {
        if (TeleportBody != null)
            TeleportBody.position = position;
    }

    private void UpdateTeleportColor()
    {
        TeleportLine.colorGradient = !LocomotionSystem.IsBusy ? TeleportAllowedGradient : TeleportWaitGradient;
        if (_teleportBodyMaterial != null)
        {
            _teleportBodyMaterial.color = TeleportLine.startColor;
        }
    }
}
