/*
 * Direct Position Sync - Bypasses Opsive's slow interpolation
 *
 * PURPOSE:
 * Directly applies networked position instead of slowly interpolating
 * This fixes choppy movement and wrong positions on remote players
 */

using UnityEngine;
using Photon.Pun;

namespace KlyrasReach.Multiplayer
{
    [RequireComponent(typeof(PhotonView))]
    public class PunDirectPositionSync : MonoBehaviour, IPunObservable
    {
        private PhotonView _photonView;
        private Rigidbody _rigidbody;

        // Lag compensation variables
        private Vector3 _networkPosition;
        private Quaternion _networkRotation;
        private Vector3 _networkVelocity;
        private Vector3 _positionAtLastPacket;
        private Quaternion _rotationAtLastPacket;

        private double _currentPacketTime;
        private double _lastPacketTime;
        private double _currentTime;

        [Header("Settings")]
        [SerializeField] private bool _enableDebugLogging = true;
        private float _lastLogTime;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
            _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody == null)
            {
                Debug.LogWarning("[PunDirectPositionSync] No Rigidbody found - velocity prediction disabled");
            }

            _networkPosition = transform.position;
            _rotationAtLastPacket = transform.rotation;
            _positionAtLastPacket = transform.position;
            _networkRotation = transform.rotation;
            _networkVelocity = Vector3.zero;
        }

        private void Start()
        {
            if (!_photonView.IsMine)
            {
                Debug.Log($"[PunDirectPositionSync] Enabled for REMOTE player: {gameObject.name}");

                // Make rigidbody kinematic for remote players
                // This prevents physics from interfering with network position updates
                if (_rigidbody != null)
                {
                    _rigidbody.isKinematic = true;
                    Debug.Log($"[PunDirectPositionSync] Set Rigidbody to kinematic for remote player");
                }
            }
            else
            {
                Debug.Log($"[PunDirectPositionSync] LOCAL player - script disabled");
                enabled = false;
            }
        }

        private void Update()
        {
            if (_photonView.IsMine) return;

            // Lag compensation interpolation
            double timeToReachGoal = _currentPacketTime - _lastPacketTime;
            _currentTime += Time.deltaTime;

            if (timeToReachGoal > 0)
            {
                float t = (float)(_currentTime / timeToReachGoal);
                transform.position = Vector3.Lerp(_positionAtLastPacket, _networkPosition, t);
                transform.rotation = Quaternion.Lerp(_rotationAtLastPacket, _networkRotation, t);
            }

            // Debug logging
            if (_enableDebugLogging && Time.time - _lastLogTime > 3f)
            {
                float distance = Vector3.Distance(transform.position, _networkPosition);
                Debug.Log($"[PunDirectPositionSync] Distance to target: {distance:F2}m, Lerp t: {(_currentTime / timeToReachGoal):F2}, Network velocity: {_networkVelocity.magnitude:F2} m/s");
                _lastLogTime = Time.time;
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // LOCAL player - send position, rotation, and velocity
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);

                if (_rigidbody != null)
                {
                    stream.SendNext(_rigidbody.linearVelocity);
                }
                else
                {
                    stream.SendNext(Vector3.zero);
                }
            }
            else
            {
                // REMOTE player - receive network position with lag compensation
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkRotation = (Quaternion)stream.ReceiveNext();
                _networkVelocity = (Vector3)stream.ReceiveNext();

                // Apply velocity prediction for lag compensation (official PUN method)
                float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                _networkPosition += _networkVelocity * lag;

                if (_enableDebugLogging)
                {
                    Debug.Log($"[PunDirectPositionSync] Packet received - Lag: {lag:F3}s, Velocity: {_networkVelocity.magnitude:F2}, Position adjusted by: {(_networkVelocity * lag).magnitude:F2}m");
                }

                // Update packet timing for smooth interpolation
                _positionAtLastPacket = transform.position;
                _rotationAtLastPacket = transform.rotation;

                _lastPacketTime = _currentPacketTime;
                _currentPacketTime = info.SentServerTime;
                _currentTime = 0.0;
            }
        }
    }
}
