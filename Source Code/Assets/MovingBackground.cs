using UnityEngine;
using System.Collections;

public class MovingBackground : MonoBehaviour {

    private const float Ymax = 6.5f;
    private const float Ymin = -3.5f;
    private float scrollSpeed;
    private Vector2 savedOffset;

    void Start()
    {
        scrollSpeed = ((Ymax - Ymin) / 25);
        savedOffset = gameObject.GetComponent<Renderer>().sharedMaterial.GetTextureOffset("_MainTex");
    }

    void Update()
    {
        float y = Mathf.Repeat(Time.time * scrollSpeed, 1);
        Vector2 offset = new Vector2(savedOffset.x, y);
        gameObject.GetComponent<Renderer>().sharedMaterial.SetTextureOffset("_MainTex", offset);
    }

    void OnDisable()
    {
        gameObject.GetComponent<Renderer>().sharedMaterial.SetTextureOffset("_MainTex", savedOffset);
    }
}
