using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Combat.Interactions
{
    public class Gun : NetworkBehaviour
    {
        [SerializeField]
        private Transform _rifleButt;

        [SerializeField]
        private GameObject _bulletPrefab;

        [SerializeField]
        private Transform _spawnPoint;

        [SerializeField]
        private float _fireRate = 10f; // Bullets fired per second

        [SerializeField]
        private float _bulletSpeed = 10f; // Speed of the bullets

        [SerializeField]
        private bool _readyToFire = true;

        public Transform RifleButt => _rifleButt;

        // Shit code, just to get the guns going a bit, to change
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
