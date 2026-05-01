using UnityEngine;
using Mirror;

public class CameraFollow : NetworkBehaviour
{
    [Header("Seguimento")]
    public Vector3 offset = new Vector3(0, 12, -8); 
    
    [Header("Configurações de Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 5f;   // Altura mínima (perto)
    public float maxZoom = 20f;  // Altura máxima (longe)
    
    private Transform camTransform;
    private Quaternion fixedRotation;
    private float currentZoom;

    void Start()
    {
        if (!isLocalPlayer) return;
        
        // Inicializa o zoom com a altura (Y) atual do offset
        currentZoom = offset.y;
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        // 1. Busca a câmera se ainda não tiver a referência
        if (camTransform == null)
        {
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
                camTransform.SetParent(null); 
                fixedRotation = Quaternion.Euler(55, 0, 0);
                camTransform.rotation = fixedRotation;
            }
            return;
        }

        // 2. Lógica de Zoom (Scroll do Mouse)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            // Altera o valor do zoom baseado no scroll
            currentZoom -= scrollInput * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            
            // Atualiza o offset Y e Z proporcionalmente para manter o ângulo
            offset.y = currentZoom;
            offset.z = -currentZoom * 0.7f; // Ajusta a distância proporcional à altura
        }

        // 3. Posicionamento Rígido (para evitar o tremor/quique)
        camTransform.position = transform.position + offset;
        
        // 4. Trava a rotação
        camTransform.rotation = fixedRotation;
    }
}