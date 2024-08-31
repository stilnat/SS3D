using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DummyStuff
{
    public class DummyFire : MonoBehaviour
    {
        [FormerlySerializedAs("bulletPrefab")]
        [SerializeField]
        private GameObject _bulletPrefab;

        [FormerlySerializedAs("spawnPoint")]
        [SerializeField]
        private Transform _spawnPoint;

        [FormerlySerializedAs("fireRate")]
        [SerializeField]
        private float _fireRate = 10f; // Bullets fired per second

        [FormerlySerializedAs("bulletSpeed")]
        [SerializeField]
        private float _bulletSpeed = 10f; // Speed of the bullets

        [SerializeField]
        private bool _readyToFire = true;

        public void Fire()
        {
            if (!_readyToFire)
            {
                return;
            }

            _readyToFire = false;

            StartCoroutine(ReadyToFire());
            GameObject bullet = Instantiate(_bulletPrefab, _spawnPoint.position, _spawnPoint.rotation);

            if (bullet.TryGetComponent(out Rigidbody bulletRigidbody))
            {
                bulletRigidbody.velocity = _spawnPoint.forward * _bulletSpeed;
            }
        }

        private IEnumerator ReadyToFire()
        {
            yield return new WaitForSeconds(1f / _fireRate);
            _readyToFire = true;
        }
    }
}
