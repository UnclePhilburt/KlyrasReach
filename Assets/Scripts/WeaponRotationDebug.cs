using UnityEngine;
using Opsive.UltimateCharacterController.Items;
using System.Reflection;

/// <summary>
/// Debug helper to adjust weapon rotation in real-time during Play mode
/// Works with Opsive's perspective item system
/// </summary>
public class WeaponRotationDebug : MonoBehaviour
{
    [Header("Real-Time Rotation Adjustment")]
    [Tooltip("Rotation to apply to the weapon")]
    public Vector3 rotation = Vector3.zero;

    [Tooltip("Position offset")]
    public Vector3 position = Vector3.zero;

    private Vector3 _lastRotation;
    private Vector3 _lastPosition;
    private object _perspectiveItem;
    private FieldInfo _objectField;

    private void Start()
    {
        // Find FirstPersonPerspectiveItem or ThirdPersonPerspectiveItem
        var firstPerson = GetComponent(System.Type.GetType("Opsive.UltimateCharacterController.FirstPersonController.Items.FirstPersonPerspectiveItem, Opsive.UltimateCharacterController"));
        var thirdPerson = GetComponent(System.Type.GetType("Opsive.UltimateCharacterController.ThirdPersonController.Items.ThirdPersonPerspectiveItem, Opsive.UltimateCharacterController"));

        _perspectiveItem = firstPerson != null ? firstPerson : thirdPerson;

        if (_perspectiveItem != null)
        {
            _objectField = _perspectiveItem.GetType().GetField("m_Object", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Log("[WeaponRotationDebug] Found perspective item, ready for real-time adjustment!");
        }
        else
        {
            Debug.LogWarning("[WeaponRotationDebug] No perspective item found!");
        }

        _lastRotation = rotation;
        _lastPosition = position;
    }

    private void LateUpdate()
    {
        // Run in LateUpdate to apply after Opsive's positioning
        if (rotation != _lastRotation || position != _lastPosition)
        {
            ApplyTransform();
            _lastRotation = rotation;
            _lastPosition = position;

            Debug.Log($"[WeaponRotationDebug] Applied - Rotation: {rotation}, Position: {position}");
        }
    }

    private void ApplyTransform()
    {
        if (_perspectiveItem != null && _objectField != null)
        {
            var objectValue = _objectField.GetValue(_perspectiveItem);
            if (objectValue != null)
            {
                var posField = objectValue.GetType().GetField("LocalPosition");
                var rotField = objectValue.GetType().GetField("LocalRotation");

                if (rotField != null)
                {
                    rotField.SetValue(objectValue, rotation);
                }
                if (posField != null)
                {
                    posField.SetValue(objectValue, position);
                }

                _objectField.SetValue(_perspectiveItem, objectValue);
            }
        }
    }
}
