using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6f;

    Rigidbody _rb;
    GameManager _gm;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _gm = FindObjectOfType<GameManager>();

        if (_rb != null)
        {
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void FixedUpdate()
    {
        if (_rb == null) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0f, v).normalized;

        Vector3 targetPos = _rb.position + dir * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(targetPos);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!other.gameObject.name.StartsWith("Coin_")) return;

        if (_gm == null) _gm = FindObjectOfType<GameManager>();
        if (_gm != null)
            _gm.CollectCoin(other.gameObject);
    }
}