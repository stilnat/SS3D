using Coimbra;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class DummyBullet : MonoBehaviour
    {
        // Start is called before the first frame update
        protected void Start()
        {
            StartCoroutine(DestroyItself());
        }

        private IEnumerator DestroyItself()
        {
            yield return new WaitForSeconds(5f);
            gameObject.Dispose(true);
        }
    }
}
