using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Piece : MonoBehaviour
{
    [SerializeField] private new Renderer renderer; 
    
    public void SetRendererActive(bool _value)
    {
        renderer.enabled = _value;
    }
}
