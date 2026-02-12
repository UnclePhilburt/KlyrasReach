/*
 * Debug version of Automatic Sliding Door
 * This will tell us exactly what's happening with player detection
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    public class AutomaticSlidingDoorDebug : MonoBehaviour
    {
        [Header("Door Panel References")]
        [SerializeField] private Transform _leftDoorPanel;
        [SerializeField] private Transform _rightDoorPanel;

        [Header("Door Behavior Settings")]
        [SerializeField] private float _slideDistance = 2f;
        [SerializeField] private float _slideSpeed = 2f;
        [SerializeField] private float _detectionRange = 3f;
        [SerializeField] private Vector3 _slideDirection = Vector3.right;

        [Header("Player Detection")]
        [SerializeField] private string _playerTag = "Player";

        private Vector3 _leftDoorClosedPosition;
        private Vector3 _leftDoorOpenPosition;
        private Vector3 _rightDoorClosedPosition;
        private Vector3 _rightDoorOpenPosition;

        private bool _isOpen = false;
        private bool _playerInRange = false;
        private BoxCollider _triggerZone;

        private void Awake()
        {
            if (_leftDoorPanel == null)
            {
                Debug.LogError("[DEBUG DOOR] No door panel assigned!");
                enabled = false;
                return;
            }

            _leftDoorClosedPosition = _leftDoorPanel.localPosition;
            _leftDoorOpenPosition = _leftDoorClosedPosition + (_slideDirection.normalized * _slideDistance);

            if (_rightDoorPanel != null)
            {
                _rightDoorClosedPosition = _rightDoorPanel.localPosition;
                _rightDoorOpenPosition = _rightDoorClosedPosition + (-_slideDirection.normalized * _slideDistance);
            }

            CreateTriggerZone();

            Debug.Log($"[DEBUG DOOR] Setup complete. Closed: {_leftDoorClosedPosition}, Open: {_leftDoorOpenPosition}");
        }

        private void CreateTriggerZone()
        {
            _triggerZone = gameObject.AddComponent<BoxCollider>();
            _triggerZone.isTrigger = true;
            _triggerZone.size = new Vector3(_detectionRange * 2, 3f, _detectionRange * 2);

            Debug.Log($"[DEBUG DOOR] Trigger created. Size: {_triggerZone.size}, Center: {_triggerZone.center}");
        }

        private void Update()
        {
            Vector3 leftTargetPosition = _playerInRange ? _leftDoorOpenPosition : _leftDoorClosedPosition;
            Vector3 rightTargetPosition = _playerInRange ? _rightDoorOpenPosition : _rightDoorClosedPosition;

            _leftDoorPanel.localPosition = Vector3.Lerp(
                _leftDoorPanel.localPosition,
                leftTargetPosition,
                Time.deltaTime * _slideSpeed
            );

            if (_rightDoorPanel != null)
            {
                _rightDoorPanel.localPosition = Vector3.Lerp(
                    _rightDoorPanel.localPosition,
                    rightTargetPosition,
                    Time.deltaTime * _slideSpeed
                );
            }

            UpdateDoorState();
        }

        private void UpdateDoorState()
        {
            float distanceToOpenPosition = Vector3.Distance(_leftDoorPanel.localPosition, _leftDoorOpenPosition);

            if (_playerInRange && !_isOpen && distanceToOpenPosition < 0.1f)
            {
                _isOpen = true;
                Debug.Log("[DEBUG DOOR] Door OPENED");
            }
            else if (!_playerInRange && _isOpen && distanceToOpenPosition > _slideDistance - 0.1f)
            {
                _isOpen = false;
                Debug.Log("[DEBUG DOOR] Door CLOSED");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[DEBUG DOOR] Something entered trigger: {other.gameObject.name}, Tag: {other.tag}");

            if (other.CompareTag(_playerTag))
            {
                _playerInRange = true;
                Debug.Log("[DEBUG DOOR] ✓✓✓ PLAYER DETECTED - Opening door! ✓✓✓");
            }
            else
            {
                Debug.LogWarning($"[DEBUG DOOR] Object '{other.gameObject.name}' entered but tag is '{other.tag}', not '{_playerTag}'");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"[DEBUG DOOR] Something exited trigger: {other.gameObject.name}");

            if (other.CompareTag(_playerTag))
            {
                _playerInRange = false;
                Debug.Log("[DEBUG DOOR] Player left - Closing door");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(transform.position, new Vector3(_detectionRange * 2, 3f, _detectionRange * 2));

            if (_leftDoorPanel != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 arrowStart = _leftDoorPanel.position;
                Vector3 arrowEnd = arrowStart + (_slideDirection.normalized * _slideDistance);
                Gizmos.DrawLine(arrowStart, arrowEnd);
                Gizmos.DrawSphere(arrowEnd, 0.1f);
            }
        }
    }
}
