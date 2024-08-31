using UnityEngine;

namespace DummyStuff
{
    public class CircleSinusoidalMotion : MonoBehaviour
    {
        [SerializeField]
        private float _radius = 5f; // Radius of the circular motion

        [SerializeField]
        private float _speed = 1f; // Speed of the motion

        [SerializeField]
        private float _minHeight = 1f; // Minimum height of the sinusoidal motion

        [SerializeField]
        private float _maxHeight = 3f; // Maximum height of the sinusoidal motion

        [SerializeField]
        private float _waveFrequency = 4f; // Frequency of the sinusoidal motion

        [SerializeField]
        private float _angle;

        protected void Update()
        {
            // Increment angle based on speed
            _angle += _speed * Time.deltaTime;

            // Calculate position using sine and cosine functions
            float x = Mathf.Cos(_angle) * _radius;
            float y = Mathf.Lerp(_minHeight, _maxHeight, (Mathf.Sin(_angle * _waveFrequency) * 0.5f) + 0.5f); // Dynamically adjust height
            float z = Mathf.Sin(_angle) * _radius;

            // Update the position of the GameObject
            transform.position = new Vector3(x, y, z);
        }
    }
}
