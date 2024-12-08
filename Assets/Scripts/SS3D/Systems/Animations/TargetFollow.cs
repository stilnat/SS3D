using UnityEngine;

public class TargetFollow : MonoBehaviour
{

    public Transform Followed { get; private set; }

    private bool _reproduceRotation;

    private float _timeToReachRotation;

    private float _timing;

    public void Follow(Transform followed, bool reproduceRotation, float timeToReachRotation = 0)
    {
        Followed = followed;
        _reproduceRotation = reproduceRotation;
        _timeToReachRotation = timeToReachRotation;
        _timing = 0;
    }

    private void Update()
    {
        if (Followed is null)
        {
            return;
        }

        transform.position = Followed.position;

        if (!_reproduceRotation) { return; }

        transform.rotation = _timing >= _timeToReachRotation ? Followed.rotation : Quaternion.Slerp(transform.rotation, Followed.rotation, _timing/_timeToReachRotation);
        _timing += Time.deltaTime;
    }
}
