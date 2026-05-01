using Mirror;
using UnityEngine;

public class AutoStart : MonoBehaviour
{
    void Start()
    {
        Debug.Log("STARTANDO HOST...");
        NetworkManager.singleton.StartHost();
    }
}