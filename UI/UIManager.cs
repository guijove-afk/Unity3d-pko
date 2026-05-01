using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject blacksmithPanel;
    [SerializeField] private GameObject bankPanel;
    [SerializeField] private GameObject guildPanel;
    [SerializeField] private GameObject stablePanel;
    [SerializeField] private GameObject teleportPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject mapPanel;

    [Header("HUD")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private GameObject minimapPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.I))
            TogglePanel(inventoryPanel);

        if (Input.GetKeyDown(KeyCode.E))
            TogglePanel(equipmentPanel);

        if (Input.GetKeyDown(KeyCode.K))
            TogglePanel(skillPanel);

        if (Input.GetKeyDown(KeyCode.Q))
            TogglePanel(questPanel);

        if (Input.GetKeyDown(KeyCode.C))
            TogglePanel(characterPanel);

        if (Input.GetKeyDown(KeyCode.M))
            TogglePanel(mapPanel);

        if (Input.GetKeyDown(KeyCode.Return))
            TogglePanel(chatPanel);

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePanel(optionsPanel);
    }

    private void TogglePanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(!panel.activeSelf);
    }

    private void CloseAllPanels()
    {
        inventoryPanel?.SetActive(false);
        equipmentPanel?.SetActive(false);
        skillPanel?.SetActive(false);
        questPanel?.SetActive(false);
        characterPanel?.SetActive(false);
        shopPanel?.SetActive(false);
        dialoguePanel?.SetActive(false);
        blacksmithPanel?.SetActive(false);
        bankPanel?.SetActive(false);
        guildPanel?.SetActive(false);
        stablePanel?.SetActive(false);
        teleportPanel?.SetActive(false);
        optionsPanel?.SetActive(false);
        mapPanel?.SetActive(false);
    }

    public void OpenShop(string npcId, string[] items)
    {
        CloseAllPanels();
        shopPanel?.SetActive(true);
    }

    public void OpenQuestDialogue(string npcId, string[] dialogue, string[] availableQuests, string[] completesQuests)
    {
        CloseAllPanels();
        dialoguePanel?.SetActive(true);
    }

    public void OpenBlacksmith(string npcId)
    {
        CloseAllPanels();
        blacksmithPanel?.SetActive(true);
    }

    public void OpenBank(string npcId)
    {
        CloseAllPanels();
        bankPanel?.SetActive(true);
    }

    public void OpenGuild(string npcId)
    {
        CloseAllPanels();
        guildPanel?.SetActive(true);
    }

    public void OpenSkillTree(string npcId)
    {
        CloseAllPanels();
        skillPanel?.SetActive(true);
    }

    public void OpenStable(string npcId)
    {
        CloseAllPanels();
        stablePanel?.SetActive(true);
    }

    public void OpenTeleport(string npcId)
    {
        CloseAllPanels();
        teleportPanel?.SetActive(true);
    }

    public void OpenDialogue(string npcId, string[] dialogue)
    {
        CloseAllPanels();
        dialoguePanel?.SetActive(true);
    }
}