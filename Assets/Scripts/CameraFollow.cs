using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    public Transform target;        // Takip edilecek blok (genellikle en üstteki)
    public float smoothSpeed = 2f;  // Kamera geçiþ hýzý
    public Vector3 offset;          // Kamera ile blok arasýndaki mesafe

    private float lastTargetY;

    void Start()
    {
        if (target != null)
            lastTargetY = target.position.y;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Sadece yukarý doðru hareketi takip et
        if (target.position.y > lastTargetY)
            lastTargetY = target.position.y;

        Vector3 desiredPosition = new Vector3(transform.position.x, lastTargetY + offset.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
